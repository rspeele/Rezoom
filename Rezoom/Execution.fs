module Rezoom.Execution
open System
open System.Collections
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Threading.Tasks
open FSharp.Control.Tasks.ContextInsensitive
open Rezoom

type ExecutionLog() =
    abstract member OnBeginStep : unit -> unit
    default __.OnBeginStep() = ()
    abstract member OnEndStep : unit -> unit
    default __.OnEndStep() = ()
    abstract member OnPreparingErrand : Errand -> unit
    default __.OnPreparingErrand(_) = ()
    abstract member OnPreparedErrand : Errand -> unit
    default __.OnPreparedErrand(_) = ()

type Dictionary<'k, 'v> with
    member this.GetValue(key : 'k, generate : 'k -> 'v) =
        let succ, v = this.TryGetValue(key)
        if succ then v else
        let generated = generate key
        this.Add(key, generated)
        generated

[<Struct>]
[<CustomEquality>]
[<NoComparison>]
type private CacheKey(identity : obj, argument : obj) =
    member inline private __.Identity = identity
    member inline private __.Argument = argument
    member this.Equals(other : CacheKey) =
        identity = other.Identity && argument = other.Argument
    override this.Equals(other : obj) =
        match other with
        | :? CacheKey as k -> this.Equals(k)
        | _ -> false
    override __.GetHashCode() =
        let h1 = identity.GetHashCode()
        if isNull argument then h1 else
        ((h1 <<< 5) + h1) ^^^ argument.GetHashCode()
    interface IEquatable<CacheKey> with
        member this.Equals(other) = this.Equals(other)

[<Struct>]
[<NoEquality>]
[<NoComparison>]
type private CacheValue(generation : int, value : obj) =
    member __.Generation = generation
    member __.Value = value

[<AllowNullLiteral>]
type private CategoryCache(windowSize : int, category : obj) =
    let cache = Dictionary<CacheKey, CacheValue>()
    /// Moving window of dependency bitmasks, indexed by generation % windowSize.
    let history = Array.zeroCreate windowSize : BitMask array
    /// Generation of invalidations we're on.
    let mutable generation = 0
    /// Pending invalidation mask.
    let mutable invalidationMask = BitMask.Full

    new(category) = CategoryCache(16, category)

    member __.Category = category

    member private __.Sweep() =
        if invalidationMask.IsFull then () else
        let mask = invalidationMask
        let latest = generation % windowSize
        let mutable i = latest
        let mutable sweeping = true
        // Go back in time invalidating bits. We can stop when we perform a mask that has no effect, since older
        // entries will necessarily have only the same or a subset of the bits of newer entries.
        while sweeping && i >= 0 do
            let existing = history.[i]
            let updated = existing &&& mask
            sweeping <- not (existing.Equals(updated))
            history.[i] <- updated
            i <- i - 1
        let anySwept = sweeping || i <> latest - 1
        i <- windowSize - 1
        while sweeping && i > latest do
            let existing = history.[i]
            let masked = existing &&& mask
            sweeping <- not (existing.Equals(mask))
            history.[i] <- masked
            i <- i - 1
        invalidationMask <- BitMask.Full
        if anySwept then
            generation <- generation + 1
            history.[generation % windowSize] <- history.[latest]

    member this.Store(info : CacheInfo, arg : obj, result : obj) =
        this.Sweep()
        cache.[CacheKey(info.Identity, arg)] <- CacheValue(generation, result)
        let index = generation % windowSize
        history.[index] <- history.[index] ||| info.DependencyMask

    member this.Retrieve(info : CacheInfo, arg : obj) =
        this.Sweep()
        let mask = info.DependencyMask
        // If we're not valid in the current generation, we definitely won't be valid in any older ones.
        // This might save us from doing a dictionary lookup with a complex object as the key.
        if not <| mask.Equals(mask &&& history.[generation % windowSize]) then None else
        let succ, cached = cache.TryGetValue(CacheKey(info.Identity, arg))
        if not succ then None else
        if generation - cached.Generation >= windowSize then None else
        // Check that all the dependency bits are still 1
        if mask.Equals(mask &&& history.[cached.Generation % windowSize]) then Some cached.Value
        else None

    member __.Invalidate(info : CacheInfo) =
        invalidationMask <- invalidationMask &&& ~~~info.InvalidationMask

type private Cache() =
    let byCategory = Dictionary<obj, CategoryCache>()
    // Remember the last one touched as a shortcut.
    let mutable lastCategory = CategoryCache(null)
    let getExistingCategory (category : obj) =
        if lastCategory.Category = category then lastCategory else
        let succ, found = byCategory.TryGetValue(category)
        if succ then found else null
    let getCategory (category : obj) =
        let existing = getExistingCategory category
        if isNull existing then
            let newCategory = CategoryCache(category)
            lastCategory <- newCategory
            byCategory.[category] <- newCategory
            newCategory
        else existing
    member __.Invalidate(info : CacheInfo) =
        match getExistingCategory info.Category with
        | null -> () // nothing to invalidate
        | cat -> cat.Invalidate(info)
    member __.Retrieve(info : CacheInfo, arg : obj) =
        match getExistingCategory info.Category with
        | null -> None
        | cat -> cat.Retrieve(info, arg)
    member __.Store(info : CacheInfo, arg : obj, result : obj) =
        let cat = getCategory info.Category
        cat.Store(info, arg, result)

type private Step(log : ExecutionLog, context : ServiceContext, cache : Cache) =
    static let defaultGroup _ = ResizeArray()
    let ungrouped = ResizeArray()
    let grouped = Dictionary()
    let deduped = Dictionary()
    let addToRun (errand : Errand) =
        try
            let mutable result = Unchecked.defaultof<_>
            log.OnPreparingErrand(errand)
            let prepared = errand.PrepareUntyped(context)
            log.OnPreparedErrand(errand)
            let retrieve () =
                match errand.CacheInfo with
                | null -> ()
                | cacheInfo -> cache.Invalidate(cacheInfo)
                task {
                    try
                        let! obj = prepared()
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
                let group = grouped.GetValue(sequenceGroup, defaultGroup)
                group.Add(retrieve)
            fun () -> result
        with
        | exn -> fun () -> RetrievalException exn
    let addWithDedup (errand : Errand) =
        let cacheInfo = errand.CacheInfo
        let dedupKey = cacheInfo.Category, cacheInfo.Identity, errand.CacheArgument
        let succ, already = deduped.TryGetValue(dedupKey)
        if succ then already else
        let added = addToRun errand
        deduped.Add(dedupKey, added)
        added

    member __.AddRequest(errand : Errand) =
        match errand.CacheInfo with
        | null -> addToRun errand
        | cacheInfo when isNull cacheInfo.Identity -> addToRun errand
        | cacheInfo ->
            match cache.Retrieve(cacheInfo, errand.CacheArgument) with
            | None ->
                addWithDedup errand
            | Some cached ->
                fun () -> RetrievalSuccess cached
            
    member __.Execute() =
        let all = Array.zeroCreate (ungrouped.Count + grouped.Count)
        let mutable i = 0
        for group in grouped.Values do
            all.[i] <-
                task {
                    for sub in group do
                        do! sub()
                }
            i <- i + 1
        for ungrouped in ungrouped do
            all.[i] <- ungrouped()
            i <- i + 1
        Task.WhenAll(all)

let executeWithLog (log : ExecutionLog) (factory : ServiceFactory) (plan : 'a Plan) =
    task {
        let cache = Cache()
        use context = new DefaultServiceContext(factory)
        let mutable plan = plan
        let mutable looping = true
        let mutable returned = Unchecked.defaultof<_>
        while looping do
            match plan with
            | Result r ->
                looping <- false
                returned <- r
            | Step (requests, resume) ->
                log.OnBeginStep()
                context.BeginStep()
                try
                    let step = Step(log, context, cache)
                    let retrievals = requests.Map(step.AddRequest)
                    do! step.Execute()
                    plan <- resume <| retrievals.Map((|>) ())
                finally
                    context.EndStep()
                    log.OnEndStep()
        return returned
    }

let private noLog = ExecutionLog()

let execute factory plan = executeWithLog noLog factory plan
    