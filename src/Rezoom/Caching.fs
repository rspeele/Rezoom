namespace Rezoom.Caching
open System
open System.Collections.Generic
open Rezoom

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

[<AbstractClass>]
type Cache() =
    abstract member Invalidate : info : CacheInfo -> unit
    abstract member Retrieve : info : CacheInfo * arg : obj -> obj option
    abstract member Store : info : CacheInfo * arg : obj * result : obj -> unit

type DefaultCache() =
    inherit Cache()
    static let comparer = EqualityComparer<obj>.Default
    let byCategory = Dictionary<obj, CategoryCache>()
    let sync = obj()
    // Remember the last one touched as a shortcut.
    let mutable lastCategory = CategoryCache(null)
    let getExistingCategory (category : obj) =
        if comparer.Equals(lastCategory.Category, category) then lastCategory else
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
    override __.Invalidate(info : CacheInfo) =
        match getExistingCategory info.Category with
        | null -> () // nothing to invalidate
        | cat -> cat.Invalidate(info)
    override __.Retrieve(info : CacheInfo, arg : obj) =
        match getExistingCategory info.Category with
        | null -> None
        | cat -> cat.Retrieve(info, arg)
    override __.Store(info : CacheInfo, arg : obj, result : obj) =
        lock sync <| fun unit -> // only stores run asynchronously and might need to be thread-safe
            let cat = getCategory info.Category
            cat.Store(info, arg, result)