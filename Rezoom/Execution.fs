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
    abstract member OnPreparingErrand : CacheInfo * Key Nullable -> unit
    default __.OnPreparingErrand(_, _) = ()
    abstract member OnPreparedErrand : CacheInfo * Key Nullable -> unit
    default __.OnPreparedErrand(_, _) = ()

type private KeyDictionary<'a>() =
    static let bucketBits = 0x800
    let buckets = BitArray(bucketBits)
    let byIdentity = Dictionary<obj, 'a>()
    member this.TryGetValue(key : Key, [<Out>] value : 'a byref) =
        buckets.[int key.Bucket % bucketBits]
        && byIdentity.TryGetValue(key.Identity, &value)
    member this.SetValue(key : Key, value : 'a) =
        buckets.[int key.Bucket % bucketBits] <- true
        byIdentity.[key.Identity] <- value
    member this.GetValue(key : Key, defaultValue : Key -> 'a) =
        let succ, v = this.TryGetValue(key)
        if succ then v else
        let v = defaultValue key
        this.SetValue(key, v)
        v
    member this.Remove(key : Key) = ignore <| byIdentity.Remove(key)
    member this.Count = byIdentity.Count
    member this.Values = byIdentity.Values

type private CacheResponse =
    {   mutable Response : obj
        Tags : CacheTag array
        Stamps : int array
    }
    member this.Valid() =
        let mutable i = 0
        let mutable good = true
        while i < this.Stamps.Length do
            if this.Stamps.[i] <> this.Tags.[i].Stamp then
                good <- false
                i <- this.Stamps.Length
            else
                i <- i + 1
        good

and private CacheTag() =
    let mutable stamp = 0
    member __.Stamp = stamp
    member __.Invalidate() =
        stamp <- stamp + 1

type private IdentityCache() =
    let byArg = KeyDictionary()
    let mutable self = Unchecked.defaultof<_>
    let mutable hasSelf = false
    member __.TryGetValue(arg : Key Nullable, [<Out>] value : CacheResponse byref) =
        if arg.HasValue then
            if byArg.TryGetValue(arg.Value, &value) then
                if value.Valid() then true else
                byArg.Remove(arg.Value)
                false
            else false
        elif hasSelf then
            value <- self
            if value.Valid() then true else
            hasSelf <- false
            false
        else false
    member this.GetValue(arg : Key Nullable, defaultValue : unit -> CacheResponse) =
        let succ, v = this.TryGetValue(arg)
        if succ then v else
        let v = defaultValue()
        if arg.HasValue then
            byArg.SetValue(arg.Value, v)
        else
            hasSelf <- true
            self <- v
        v

type private Cache() =
    let tags = KeyDictionary<CacheTag>()
    let byCategory = KeyDictionary<KeyDictionary<IdentityCache>>()
    member __.InvalidateTag(tag) =
        let succ, ctag = tags.TryGetValue(tag)
        if succ then ctag.Invalidate()
    member __.Retrieve(category : Key, id : Key, arg : Key Nullable) =
        let succ, category = byCategory.TryGetValue(category)
        if not succ then None else
        let succ, identity = category.TryGetValue(id)
        if not succ then None else
        let succ, response = identity.TryGetValue(arg)
        if not succ then None else
        Some response.Response
    member __.Set(info : CacheInfo, arg : Key Nullable, result : obj) =
        if isNull info then () else
        let id = info.Identity
        if not id.HasValue then () else
        let category = byCategory.GetValue(info.Category, fun _ -> KeyDictionary())
        let identity = category.GetValue(id.Value, fun _ -> IdentityCache())
        let dependencies = info.TagDependencies
        let tagCount = dependencies.Count
        let defaultCached() =
            {   Response = result
                Stamps = Array.zeroCreate tagCount
                Tags = Array.zeroCreate tagCount
            }
        let cached = identity.GetValue(arg, defaultCached)
        cached.Response <- result
        let tagRefs = cached.Tags
        let stamps = cached.Stamps
        for i, tag in dependencies |> Seq.indexed do
            let tagRef = tags.GetValue(tag, fun _ -> CacheTag())
            tagRefs.[i] <- tagRef
            stamps.[i] <- tagRef.Stamp

type private Step(log : ExecutionLog, context : ServiceContext, cache : Cache) =
    let ungrouped = ResizeArray()
    let grouped = KeyDictionary()
    let defaultGroup _ = ResizeArray()
    let invalidateTags (cacheInfo : CacheInfo) =
        if isNull cacheInfo then () else
        let invalidations = cacheInfo.TagInvalidations
        if isNull invalidations || invalidations.Count <= 0 then () else
        for tag in invalidations do
            cache.InvalidateTag(tag)
    let deduped = Dictionary()
    let addToRun (errand : Errand) =
        invalidateTags errand.CacheInfo
        try
            let mutable result = Unchecked.defaultof<_>
            log.OnPreparingErrand(errand.CacheInfo, errand.CacheArgument)
            let prepared = errand.PrepareUntyped(context)
            log.OnPreparedErrand(errand.CacheInfo, errand.CacheArgument)
            let retrieve () =
                task {
                    try
                        let! obj = prepared()
                        result <- RetrievalSuccess obj
                        cache.Set(errand.CacheInfo, errand.CacheArgument, obj)
                    with
                    | exn ->
                        result <- RetrievalException exn
                }
            let sequenceGroup = errand.SequenceGroup
            if errand.SequenceGroup.HasValue then
                let group = grouped.GetValue(errand.SequenceGroup.Value, defaultGroup)
                group.Add(retrieve)
            else
                ungrouped.Add(retrieve)
            fun () -> result
        with
        | exn -> fun () -> RetrievalException exn
    let addWithDedup (errand : Errand) (category : Key) (identity : Key) =
        let dedupKey = category, identity, errand.CacheArgument
        let succ, already = deduped.TryGetValue(dedupKey)
        if succ then already else
        let added = addToRun errand
        deduped.Add(dedupKey, added)
        added

    member __.AddRequest(errand : Errand) =
        match errand.CacheInfo with
        | null -> addToRun errand
        | cacheInfo when not cacheInfo.Identity.HasValue -> addToRun errand
        | cacheInfo ->
            match cache.Retrieve(cacheInfo.Category, cacheInfo.Identity.Value, errand.CacheArgument) with
            | None ->
                addWithDedup errand cacheInfo.Category cacheInfo.Identity.Value
            | Some cached ->
                fun () -> RetrievalSuccess cached
            
    member __.Execute() =
        task {
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
            do! Task.WhenAll(all)
        }

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
    