namespace Rezoom
open System
open System.Collections
open System.Collections.Generic
open System.Runtime.InteropServices

[<Struct>]
[<CustomEquality>]
[<NoComparison>]
type Key =
    /// Used for a fast check to determine that a key is definitely *not* cached.
    /// The rules for this are about the same as for a hashcode: must be equal for two equal keys,
    /// but doesn't need to be inequal for two inequal keys. However, this should be precomputed,
    /// whereas getting a hashcode (e.g. of a big string like a SQL query) can potentially be expensive.
    val public Bucket : uint16
    /// The identity of the key, used to compare keys for equality after they are confirmed to have the same bucket.
    val public Identity : obj
    new(bucket, id : obj) = { Bucket = bucket; Identity = id }
    new(id : obj) =
        {   Bucket =
                match id with
                | null -> 0us
                | :? Type as t -> uint16 (t.GetHashCode())
                | id -> uint16 (id.GetType().GetHashCode())
            Identity = id
        }
    static member OfArray(keys : Key array) =
        let mutable bucket = 0us
        for i = 0 to min 10 (keys.Length - 1) do
            bucket <- ((bucket <<< 3) + bucket) ^^^ keys.[i].Bucket
        Key(bucket, KeyArrayEquatable(keys))
    member this.Equals(k : Key) = k.Bucket = this.Bucket && k.Identity = this.Identity
    override this.Equals(other : obj) =
        match other with
        | :? Key as k -> k.Bucket = this.Bucket && k.Identity = this.Identity
        | _ -> false
    override this.GetHashCode() = this.Identity.GetHashCode()
    interface IEquatable<Key> with
        member this.Equals(other) = other.Bucket = this.Bucket && other.Identity = this.Identity
and KeyArrayEquatable(keys : Key array) =
    member inline private this.Keys = keys
    member this.Equals(k : KeyArrayEquatable) =
        let k = k.Keys
        if keys.Length <> k.Length then false else
        let mutable i = 0
        let mutable eq = true
        while i < keys.Length && eq do
            eq <- keys.[i].Equals(k.[i])
            i <- i + 1
        eq
    override this.Equals(other : obj) =
        match other with
        | :? KeyArrayEquatable as k ->
            this.Equals(k)
        | _ -> false
    override __.GetHashCode() =
        let mutable h = 0
        for i = 0 to keys.Length - 1 do
            h <- ((h <<< 5) + h) ^^^ keys.[i].Identity.GetHashCode()
        h
    interface IEquatable<KeyArrayEquatable> with
        member this.Equals(other) = this.Equals(other)

[<AbstractClass>]
[<AllowNullLiteral>]
type CacheInfo() =
    abstract member Category : Key
    abstract member Identity : Key Nullable
    default __.Identity = Nullable()
    abstract member TagDependencies : Key IReadOnlyCollection
    default __.TagDependencies = upcast [||]
    abstract member TagInvalidations : Key IReadOnlyCollection
    default __.TagInvalidations = upcast [||]
