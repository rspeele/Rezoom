// If you use external state like DateTime.UtcNow in a Plan, that's fine, it'll work how you expect.
// But you won't be able to replay it using the tools in the Replay module, because when replaying it
// it might generate errands with different IDs than it did the first time (on account of their arguments
// including these things pulled from external state).

/// This module provides versions of IO functions like DateTime.UtcNow that are wrapped in errands,
/// so you can use them in plans without breaking replays.
[<AutoOpen>]
module Rezoom.Stdlib
open System

module TrivialErrand =
    let ofSynchronous category id f =
        let cacheInfo =
            { new CacheInfo() with
                override __.Category = category
                override __.Identity = id
                override __.Cacheable = false
            }
        fun arg ->
            { new SynchronousErrand<_>() with
                override __.CacheInfo = cacheInfo
                override __.CacheArgument = box arg
                override __.Prepare(_) =
                    fun () -> f arg
            }

type private StdlibFunction = StdlibFunction

let private category = box StdlibFunction
let private trivial name f =
    TrivialErrand.ofSynchronous category name f
let private trivialUnit name f =
    trivial name f () |> Plan.ofErrand

let private dateTimeUtcNow = trivialUnit "dtUtcNow" (fun () -> DateTime.UtcNow)
let private dateTimeNow = trivialUnit "dtLocalNow" (fun () -> DateTime.Now)
let private dateTimeOffsetUtcNow = trivialUnit "dtoUtcNow" (fun () -> DateTimeOffset.UtcNow)
let private dateTimeOffsetNow = trivialUnit "dtoLocalNow" (fun () -> DateTimeOffset.Now)

type DateTime with
    /// Get the current time in UTC as a plan.
    /// Use this so replaying your plan at a later time returns the same result.
    static member UtcNowPlan = dateTimeUtcNow
    /// Get the current local time as a plan.
    /// Use this so replaying your plan at a later time returns the same result.
    static member NowPlan = dateTimeNow

type DateTimeOffset with
    /// Get the current time in UTC as a plan.
    /// Use this so replaying your plan at a later time returns the same result.
    static member UtcNowPlan = dateTimeOffsetUtcNow
    /// Get the current local time as a plan.
    /// Use this so replaying your plan at a later time returns the same result.
    static member NowPlan = dateTimeOffsetNow
