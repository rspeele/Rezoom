module Rezoom.Execution
open System
open System.Collections
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.ContextInsensitive
open Rezoom
open Rezoom.Caching

type private Step(instance : ExecutionInstance, context : ServiceContext, cache : Cache) =
    static let completedTask = Task.Delay(0)
    static let defaultGroup _ = ResizeArray()
    static let retrievalDeferred () = RetrievalDeferred
    static let getValue (this : Dictionary<'k, 'v>) (key : 'k) (generate : 'k -> 'v) =
        let succ, v = this.TryGetValue(key)
        if succ then v else
        let generated = generate key
        this.Add(key, generated)
        generated
    let ungrouped = ResizeArray()
    let grouped = Dictionary()
    let deduped = Dictionary()
    let anyCached = ref false
    let pending = ResizeArray()
    let run (errand : Errand) =
        if !anyCached then retrievalDeferred else
        try
            let mutable result = Unchecked.defaultof<_>
            instance.Log.OnPreparingErrand(errand)
            let prepared = instance.RunErrand(errand, context)
            instance.Log.OnPreparedErrand(errand)
            let retrieve token =
                cache.Invalidate(errand.CacheInfo)
                task {
                    try
                        let! obj = prepared token
                        result <- RetrievalSuccess obj
                        cache.Store(errand.CacheInfo, errand.CacheArgument, obj)
                    with
                    | exn ->
                        result <- RetrievalException exn
                }
            let sequenceGroup = errand.SequenceGroup
            match errand.SequenceGroup with
            | null ->
                ungrouped.Add(retrieve)
            | sequenceGroup ->
                let group = getValue grouped sequenceGroup defaultGroup
                group.Add(retrieve)
            fun () -> result
        with
        | exn -> fun () -> RetrievalException exn
            
    let addToRun (errand : Errand) =
        let ran = lazy run errand
        pending.Add(ran)
        fun () -> ran.Value()

    let addWithDedup (errand : Errand) =
        let cacheInfo = errand.CacheInfo
        let dedupKey = cacheInfo.Category, cacheInfo.Identity, errand.CacheArgument
        let succ, already = deduped.TryGetValue(dedupKey)
        if succ then already else
        let added = addToRun errand
        deduped.Add(dedupKey, added)
        added

    member __.AddRequest(errand : Errand) =
        let cacheInfo = errand.CacheInfo
        if cacheInfo.Cacheable then
            match cache.Retrieve(cacheInfo, errand.CacheArgument) with
            | None ->
                addWithDedup errand
            | Some cached ->
                anyCached := true
                fun () -> RetrievalSuccess cached
        else
            addToRun errand
            
    member __.Execute(token) =
        for i = 0 to pending.Count - 1 do ignore <| pending.[i].Force()
        let taskCount = ungrouped.Count + grouped.Count
        if taskCount <= 0 then completedTask else
        let all = Array.zeroCreate taskCount
        let mutable i = 0
        for group in grouped.Values do
            all.[i] <-
                task {
                    for sub in group do
                        do! sub token
                } :> Task
            i <- i + 1
        for ungrouped in ungrouped do
            all.[i] <- upcast ungrouped token
            i <- i + 1
        Task.WhenAll(all)

type private ExecutionServiceContext(config : IServiceConfig) =
    inherit ServiceContext()
    let services = Dictionary<Type, obj>()
    let locals = Stack<_>()
    let globals = Stack<_>()
    let mutable totalSuccess = false
    override __.Configuration = config
    override this.GetService<'f, 'a when 'f :> ServiceFactory<'a> and 'f : (new : unit -> 'f)>() =
        let ty = typeof<'f>
        let succ, service = services.TryGetValue(ty)
        if succ then Unchecked.unbox service else
        let factory = new 'f()
        let service = factory.CreateService(this)
        let stack =
            match factory.ServiceLifetime with
            | ServiceLifetime.ExecutionLocal -> globals
            | ServiceLifetime.StepLocal -> locals
            | other -> failwithf "Unknown service lifetime: %O" other
        services.Add(ty, box service)
        stack.Push(fun state ->
            factory.DisposeService(state, service)
            ignore <| services.Remove(ty))
        service
    static member private ClearStack(stack : _ Stack, state) =
        let mutable exn = null
        while stack.Count > 0 do
            let disposer = stack.Pop()
            try
                disposer state
            with
            | e ->
                if isNull exn then exn <- e
                else exn <- AggregateException(exn, e)
        if not (isNull exn) then raise exn
    member __.ClearLocals(state) = ExecutionServiceContext.ClearStack(locals, state)
    member __.SetSuccessful() = totalSuccess <- true
    member this.Dispose() =
        let state = if totalSuccess then ExecutionSuccess else ExecutionFault
        try
            this.ClearLocals(state)
        finally
            ExecutionServiceContext.ClearStack(globals, state)
    interface IDisposable with
        member this.Dispose() = this.Dispose()

let executeWithCancellation (token : CancellationToken) (config : ExecutionConfig) (plan : 'a Plan) =
    task {
        let instance = config.Instance()
        let cache = instance.CreateCache()
        use context = new ExecutionServiceContext(config.ServiceConfig)
        let mutable planState = Plan.advance plan
        let mutable looping = true
        let mutable returned = Unchecked.defaultof<_>
        while looping do
            match planState with
            | Result r ->
                looping <- false
                returned <- r 
            | Step (requests, resume) ->
                instance.Log.OnBeginStep()
                let mutable stepState = ExecutionFault
                try
                    let step = Step(instance, context, cache)
                    let retrievals = requests.Map(step.AddRequest)
                    do! step.Execute(token).ConfigureAwait(continueOnCapturedContext = true)
                    planState <- retrievals.Map((|>) ()) |> resume |> Plan.advance
                    stepState <- ExecutionSuccess
                finally
                    context.ClearLocals(stepState)
                    instance.Log.OnEndStep()
        context.SetSuccessful() // if we got this far we can dispose with success (commit)
        return returned
    }

let execute (config : ExecutionConfig) (plan : 'a Plan) =
    let token = CancellationToken()
    executeWithCancellation token config plan
    