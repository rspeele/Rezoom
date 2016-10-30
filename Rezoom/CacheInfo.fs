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
[<AllowNullLiteral>]
type CacheInfo() =
    abstract member Category : obj
    abstract member Identity : obj
    default __.Identity = null
    abstract member DependencyMask : BitMask
    default __.DependencyMask = BitMask.Zero
    abstract member InvalidationMask : BitMask
    default __.InvalidationMask = BitMask.Zero
