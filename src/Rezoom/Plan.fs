namespace Rezoom
open System
open System.Runtime.CompilerServices

type DataResponse =
    /// The errand ran and produced a result.
    | RetrievalSuccess of result : obj
    /// The errand failed with an exception.
    | RetrievalException of exn : exn
    /// The errand has not yet been run.
    | RetrievalDeferred

type Batch<'a> =
    | BatchNone
    | BatchLeaf of 'a
    | BatchPair of 'a Batch * 'a Batch
    | BatchMany of 'a Batch array
    | BatchAbort
    member this.Map(f : 'a -> 'b) =
        match this with
        | BatchNone -> BatchNone
        | BatchLeaf x -> BatchLeaf (f x)
        | BatchPair (l, r) -> BatchPair (l.Map(f), r.Map(f))
        | BatchMany arr -> BatchMany (arr |> Array.map (fun b -> b.Map(f)))
        | BatchAbort -> BatchAbort

type Requests = Errand Batch
type Responses = DataResponse Batch

/// Marker interface for awaiters that participate in Rezoom plan async methods.
/// `PlanMethodBuilder` constrains its awaiter type parameter to this, so C#
/// `async Plan<T>` methods can only `await` Plan-bearing awaitables - attempting to
/// `await` a `Task` or other arbitrary awaitable is a compile-time error.
and IPlanAwaiter =
    /// Returns a `Plan<unit>` which, when executed, drives the awaiter's underlying
    /// plan to completion and records its result (or exception) on the awaiter.
    /// Once that Plan finishes, the awaiter's `GetResult()` returns or throws as
    /// appropriate.
    abstract member CapturePlan : unit -> Plan<unit>

and [<AsyncMethodBuilder(typedefof<PlanMethodBuilder<_>>)>]
    Plan<'result> =
    | Result of 'result
    | Step of Requests * (Responses -> Plan<'result>)
    /// Hook for C# `await`. F# code should keep using `let!` inside `plan { }`.
    member this.GetAwaiter() = PlanAwaiter<'result>(this)

and PlanAwaiter<'U>(source : Plan<'U>) =
    let mutable result : 'U = Unchecked.defaultof<'U>
    let mutable capturedExn : exn = null
    let mutable hasResult = false
    do
        match source with
        | Result v ->
            result <- v
            hasResult <- true
        | _ -> ()
    /// True if the awaited plan has already produced a result or thrown.
    /// The C# `await` codegen skips the suspend/resume dance when this is true.
    member __.IsCompleted = hasResult || not (isNull capturedExn)
    /// Called by the resumed state machine. Returns the captured value or
    /// rethrows the captured exception.
    member __.GetResult() : 'U =
        if not (isNull capturedExn) then dispatchRaise capturedExn
        else result
    interface IPlanAwaiter with
        member __.CapturePlan() : Plan<unit> =
            if hasResult || not (isNull capturedExn) then Result () else
            let rec capture (p : Plan<'U>) : Plan<unit> =
                match p with
                | Result v ->
                    result <- v
                    hasResult <- true
                    Result ()
                | Step (reqs, resume) ->
                    Step (reqs, fun responses ->
                        try
                            capture (resume responses)
                        with
                        // Includes PlanAbortException: by routing aborts through
                        // GetResult we give the state machine's finally blocks a
                        // chance to run. The compiler-generated outer catch then
                        // surfaces the abort back through SetException so the
                        // executor sees it via the normal failure path.
                        | e ->
                            capturedExn <- e
                            Result ())
            capture source
    interface ICriticalNotifyCompletion with
        // The builder drives resumption via the Plan executor; the standard
        // awaiter callbacks are unused.
        member __.OnCompleted(_) = ()
        member __.UnsafeOnCompleted(_) = ()

and internal PlanBuilderState<'T> =
    | BuilderNotStarted
    | BuilderWaiting of Plan<unit>
    | BuilderCompleted of 'T
    | BuilderFaulted of exn

/// Mutable heap state shared across copies of a `PlanMethodBuilder` struct.
/// Holds the boxed state machine plus the current outcome (waiting / completed / faulted).
and [<Sealed>] PlanBuilderBox<'T>() =
    let mutable state : PlanBuilderState<'T> = BuilderNotStarted
    let mutable stateMachine : IAsyncStateMachine = null
    let mutable startCalled = false
    member __.SetStateMachine(sm : IAsyncStateMachine) = stateMachine <- sm
    member __.SetCompleted(v : 'T) = state <- BuilderCompleted v
    member __.SetFaulted(e : exn) = state <- BuilderFaulted e
    member __.SetWaiting(p : Plan<unit>) = state <- BuilderWaiting p
    /// Returns the outer `Plan<'T>` that represents the state machine's execution.
    /// First access wraps the initial `MoveNext` in a `Step(BatchNone, _)` so
    /// nothing actually runs until the plan is advanced - matching F#'s
    /// `plan { }` "nothing happens until stepped" semantics. Without this,
    /// pre-await side effects (e.g. setting a `started` flag) and synchronous
    /// throws would run at the *call site* of the async method, defeating
    /// applicative batching's ability to abort siblings cleanly.
    member this.GetPlan() : Plan<'T> =
        if not startCalled then
            startCalled <- true
            Step (BatchNone, fun _ ->
                stateMachine.MoveNext()
                this.DrivePlan())
        else
            this.DrivePlan()
    member private this.DrivePlan() : Plan<'T> =
        let rec drive () : Plan<'T> =
            match state with
            | BuilderNotStarted ->
                dispatchRaise (LogicFaultException "PlanMethodBuilder.Task accessed before state machine produced an outcome")
            | BuilderCompleted v -> Result v
            | BuilderFaulted e -> dispatchRaise e
            | BuilderWaiting waitPlan ->
                state <- BuilderNotStarted
                let rec wrap (p : Plan<unit>) =
                    match p with
                    | Result () ->
                        stateMachine.MoveNext()
                        drive()
                    | Step (reqs, resume) ->
                        Step (reqs, fun responses -> wrap (resume responses))
                wrap waitPlan
        drive()

/// `[AsyncMethodBuilder]` target for `Plan<'T>`. The compiler-generated state
/// machine for an `async Plan<T>` method drives an instance of this struct.
and [<Struct; NoEquality; NoComparison>]
    PlanMethodBuilder<'T> =
    val mutable private Box : PlanBuilderBox<'T>

    static member Create() : PlanMethodBuilder<'T> =
        let mutable b = Unchecked.defaultof<PlanMethodBuilder<'T>>
        b.Box <- PlanBuilderBox<'T>()
        b

    member this.Task : Plan<'T> = this.Box.GetPlan()

    /// Box the state machine but DO NOT call MoveNext yet - that's deferred to
    /// when the returned Plan is actually stepped (see `PlanBuilderBox.GetPlan`).
    member this.Start<'TSm when 'TSm :> IAsyncStateMachine>(stateMachine : byref<'TSm>) =
        let boxed = stateMachine :> IAsyncStateMachine
        boxed.SetStateMachine(boxed)
        this.Box.SetStateMachine(boxed)

    member this.SetResult(v : 'T) = this.Box.SetCompleted(v)
    member this.SetException(e : exn) = this.Box.SetFaulted(e)
    /// Required by the protocol but unused: we manage our own boxed reference in Start.
    member __.SetStateMachine(_ : IAsyncStateMachine) = ()

    member this.AwaitOnCompleted<'TAw, 'TSm
        when 'TAw :> INotifyCompletion
        and 'TAw :> IPlanAwaiter
        and 'TSm :> IAsyncStateMachine>
        (awaiter : byref<'TAw>, _stateMachine : byref<'TSm>) =
        this.Box.SetWaiting((awaiter :> IPlanAwaiter).CapturePlan())

    member this.AwaitUnsafeOnCompleted<'TAw, 'TSm
        when 'TAw :> ICriticalNotifyCompletion
        and 'TAw :> IPlanAwaiter
        and 'TSm :> IAsyncStateMachine>
        (awaiter : byref<'TAw>, _stateMachine : byref<'TSm>) =
        this.Box.SetWaiting((awaiter :> IPlanAwaiter).CapturePlan())

/// Hint that it is OK to batch the given sequence or task
type BatchHint<'a> = internal | BatchHint of 'a

[<AutoOpen>]
module BatchHints =
    let batch x = BatchHint x
