namespace Rezoom.CS
open Rezoom
open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices

/// C#-friendly base for asynchronous errands. Takes a CLR `Func<CancellationToken, Task<'a>>`
/// rather than an F# `CancellationToken -> Task<'a>` so C# subclasses can override
/// `Prepare` with a familiar delegate type.
[<AbstractClass>]
type AsynchronousErrand<'a>() =
    inherit Errand<'a>()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    abstract member Prepare : PlanContext -> Func<CancellationToken, 'a Task>
    override this.PrepareUntyped(cxt) : CancellationToken -> obj Task =
        let typed = this.Prepare(cxt)
        fun token ->
            let t = typed.Invoke(token)
            t.ContinueWith(AsynchronousErrand<'a>.BoxResult, TaskContinuationOptions.ExecuteSynchronously)

/// C#-friendly base for synchronous errands. Takes a CLR `Func<'a>` rather than
/// an F# `unit -> 'a`.
[<AbstractClass>]
type SynchronousErrand<'a>() =
    inherit Errand<'a>()
    abstract member Prepare : PlanContext -> Func<'a>
    override this.PrepareUntyped(cxt) : CancellationToken -> obj Task =
        let sync = this.Prepare(cxt)
        fun _ ->
            Task.FromResult(box (sync.Invoke()))

[<Extension>]
type CSExtensions =
    /// Lift an `Errand<'a>` into a `Plan<'a>` from C#.
    [<Extension>]
    static member ToPlan(request : Errand<'a>) =
        Plan.ofErrand request

/// C#-facing entry points that mirror the `plan { ... }` computation expression
/// for cases that `await` alone can't express: applicative tuples, batched
/// for-each, and concurrent lists. F# code should keep using the computation
/// expression; this exists so C# `async Plan<T>` methods can opt into batching
/// at the points where they need it.
[<Sealed; AbstractClass>]
type Plans private () =
    /// Run two plans concurrently, returning their results as a value-tuple.
    /// Outstanding requests from both plans are batched into one step.
    static member Tuple<'a, 'b>(planA : Plan<'a>, planB : Plan<'b>) : Plan<struct ('a * 'b)> =
        Plan.apply
            (Plan.map (fun a -> fun b -> struct (a, b)) planA)
            planB

    static member Tuple<'a, 'b, 'c>(planA : Plan<'a>, planB : Plan<'b>, planC : Plan<'c>)
        : Plan<struct ('a * 'b * 'c)> =
        Plan.apply
            (Plan.apply
                (Plan.map (fun a -> fun b -> fun c -> struct (a, b, c)) planA)
                planB)
            planC

    static member Tuple<'a, 'b, 'c, 'd>
        (planA : Plan<'a>, planB : Plan<'b>, planC : Plan<'c>, planD : Plan<'d>)
        : Plan<struct ('a * 'b * 'c * 'd)> =
        Plan.apply
            (Plan.apply
                (Plan.apply
                    (Plan.map (fun a -> fun b -> fun c -> fun d -> struct (a, b, c, d)) planA)
                    planB)
                planC)
            planD

    /// Run all the given plans concurrently and collect their results into a list.
    static member ConcurrentList<'a>(plans : seq<Plan<'a>>) : Plan<'a list> =
        plans |> List.ofSeq |> Plan.concurrentList

    /// Monadic iteration: run `body` for each element in `source`, one at a time
    /// (no batching across iterations). The body's result value is discarded.
    static member ForEach<'a, 'r>(source : seq<'a>, body : Func<'a, Plan<'r>>) : Plan<unit> =
        Plan.forM source (fun x -> Plan.map ignore (body.Invoke(x)))

    /// Applicative iteration: build a Plan for every element in `source` up front,
    /// then run them concurrently with their first-round requests batched.
    /// The body's result value is discarded.
    static member ForBatch<'a, 'r>(source : seq<'a>, body : Func<'a, Plan<'r>>) : Plan<unit> =
        Plan.forA source (fun x -> Plan.map ignore (body.Invoke(x)))

    /// `Plan<'a> -> Plan<'b>` via a synchronous function.
    static member Map<'a, 'b>(plan : Plan<'a>, f : Func<'a, 'b>) : Plan<'b> =
        Plan.map f.Invoke plan

    /// `Plan<'a> -> Plan<'b>` via a plan-producing function.
    static member Bind<'a, 'b>(plan : Plan<'a>, f : Func<'a, Plan<'b>>) : Plan<'b> =
        Plan.bind plan f.Invoke

    /// Replay-safe `DateTime.UtcNow` as a Plan. C# wrapper for `DateTime.UtcNowPlan`.
    static member UtcNow : Plan<DateTime> = DateTime.UtcNowPlan
    /// Replay-safe `DateTime.Now` as a Plan.
    static member LocalNow : Plan<DateTime> = DateTime.NowPlan
    /// Replay-safe `DateTimeOffset.UtcNow` as a Plan.
    static member UtcNowOffset : Plan<DateTimeOffset> = DateTimeOffset.UtcNowPlan
    /// Replay-safe `DateTimeOffset.Now` as a Plan.
    static member LocalNowOffset : Plan<DateTimeOffset> = DateTimeOffset.NowPlan

    /// Wrap a CLR `Task<'a>` so it can participate in a Plan. This is a *non-batched*
    /// errand - each await runs its own round-trip. Use sparingly; it's an escape
    /// hatch for one-off async APIs that don't have Plan-aware versions.
    static member FromTask<'a>(taskFactory : Func<CancellationToken, 'a Task>) : Plan<'a> =
        let errand =
            { new AsynchronousErrand<'a>() with
                override __.CacheInfo =
                    { new CacheInfo() with
                        override __.Category = upcast typeof<Plans>
                        override __.Identity = upcast (Guid.NewGuid())
                        override __.Cacheable = false }
                override __.Prepare(_) = taskFactory
            }
        Plan.ofErrand errand
