namespace Rezoom
open System
open System.Reflection
open System.Collections
open System.Collections.Generic

[<Struct>]
[<CustomEquality>]
[<NoComparison>]
type BitMask(high : uint64, low : uint64) =
    new(low) = BitMask(0UL, low)
    member inline private __.High = high
    member inline private __.Low = low

    member __.HighBits = high
    member __.LowBits = low

    static member Zero = BitMask(0UL, 0UL)
    static member Full = BitMask(~~~0UL, ~~~0UL)

    static member BitLength = 128

    member __.IsZero = 0UL = (high ||| low)
    member __.IsFull = ~~~0UL = (high &&& low)
    member __.WithBit(bit : int, set : bool) =
        if bit < 32 then
            BitMask(high, if set then low ||| (1UL <<< bit) else low &&& ~~~(1UL <<< bit))
        else
            let bit = bit - 32
            BitMask((if set then high ||| (1UL <<< bit) else low &&& ~~~(1UL <<< bit)), low)

    static member (&&&) (left : BitMask, right : BitMask) =
        BitMask(left.High &&& right.High, left.Low &&& right.Low)
    static member (|||) (left : BitMask, right : BitMask) =
        BitMask(left.High ||| right.High, left.Low ||| right.Low)
    static member (^^^) (left : BitMask, right : BitMask) =
        BitMask(left.High ^^^ right.High, left.Low ^^^ right.Low)
    static member (~~~) (bits : BitMask) =
        BitMask(~~~bits.High, ~~~bits.Low)

    override __.ToString() =
        high.ToString("X16") + low.ToString("X16")
    member __.Equals(other : BitMask) = high = other.High && low = other.Low
    override this.Equals(other : obj) =
        match other with
        | :? BitMask as bm -> this.Equals(bm)
        | _ -> false
    override __.GetHashCode() =
        int high
        ^^^ int (high >>> 32)
        ^^^ int low
        ^^^ int (low >>> 32)
    interface IEquatable<BitMask> with
        member this.Equals(other) = this.Equals(other)

[<AbstractClass>]
type CacheInfo() =
    /// A non-null comparable object which identifies the cache that this errand should use. Each category gets its
    /// own isolated cache, so results associated with it can't be interfered with by errands from other categories.
    /// Typically all errands defined by a library will have the same category because their identities are known
    /// not to collide. A good choice for overriding this is `typeof<PrivateTypeInMyLibrary>`.
    abstract member Category : obj
    /// A non-null comparable object that uniquely (within Category) identifies the function that produced this errand.
    /// For example, the string "userById" might be a reasonable identity for an errand returned from
    /// `getUserById 27`.
    abstract member Identity : obj
    /// Determines whether this errand's result can be cached. It is useful to specify cache info even for
    /// errands that can't be cached, because they can still invalidate other cached results, and specifying their
    /// identities is useful for logging.
    abstract member Cacheable : bool
    /// A 128-bit mask where bits set to 1 represent dependencies of this errand. If another errand runs in this or a
    /// later execution step that includes *any* of these bits in its InvalidationMask, the cached result for this
    /// errand will be discarded.
    abstract member DependencyMask : BitMask
    default __.DependencyMask = BitMask.Full // default is safest: any invalidations kill us
    /// A 128-bit mask where bits set to 1 represent dependencies invalidated by executing this errand.
    /// When the errand runs, any cached results from errands in this `Category` which had any of these bits in their
    /// `DependencyMask` will be discarded.
    abstract member InvalidationMask : BitMask
    default this.InvalidationMask =
        // default is that anything that isn't itself cachable wipes out the whole cache
        if this.Cacheable then BitMask.Zero else BitMask.Full
