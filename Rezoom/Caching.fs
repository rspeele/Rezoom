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
    new(bucket, id) = { Bucket = bucket; Identity = id }
    new(id : obj) =
        {   Bucket =
                match id with
                | null -> 0us
                | :? Type as t -> uint16 (t.GetHashCode())
                | id -> uint16 (id.GetType().GetHashCode())
            Identity = id
        }
    override this.Equals(other : obj) =
        match other with
        | :? Key as k -> k.Bucket = this.Bucket && k.Identity = this.Identity
        | _ -> false
    override this.GetHashCode() = this.Identity.GetHashCode()
    interface IEquatable<Key> with
        member this.Equals(other) = other.Bucket = this.Bucket && other.Identity = this.Identity

[<AbstractClass>]
type CachingInfo() =
    abstract member Category : Key
    abstract member Identity : Key option
    default __.Identity = None
    abstract member TagDependencies : Key seq
    default __.TagDependencies = Seq.empty
    abstract member TagInvalidations : Key seq
    default __.TagInvalidations = Seq.empty

type KeyDictionary<'a>() =
    static let bucketBits = 0x800
    let buckets = BitArray(bucketBits)
    let byIdentity = Dictionary<obj, 'a>()
    member this.TryGetValue(key : Key, [<Out>] value : 'a byref) =
        buckets.[int key.Bucket % bucketBits]
        && byIdentity.TryGetValue(key.Identity, &value)
    member this.SetValue(key : Key, value : 'a) =
        buckets.[int key.Bucket % bucketBits] <- true
        byIdentity.[key.Identity] <- value

