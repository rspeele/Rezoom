namespace Rezoom
open System

/// The scope of a <see cref="PlanLocal{T}"/> instance during plan execution.
type Lifetime =
    /// One instance per plan execution. Created on first request within an execution,
    /// disposed when the execution finishes (success OR fault).
    | Execution = 1
    /// One instance per plan step. Created on first request within a step, disposed
    /// at the end of the step. This is what makes batching-style errands share
    /// per-step state with their peers.
    | Step = 2

type ExecutionState =
    | ExecutionFault
    | ExecutionSuccess

/// Plumbing for errands that need scoped per-plan state. Typically batch
/// coordinators that collect peer errands within a step so they can be sent as a
/// single round-trip. NOT a DI container; "PlanLocal" should be thought of as
/// "AsyncLocal-like, but scoped to a Rezoom plan run." End users register actual
/// services in their host's <see cref="System.IServiceProvider"/>; libraries use
/// PlanLocal to manage their own coordination state without exposing the
/// machinery to consumers.
[<AbstractClass>]
type PlanLocal<'a>() =
    /// Build an instance for the relevant scope, using the PlanContext to look up
    /// other PlanLocal instances or host services.
    abstract member Create : PlanContext -> 'a
    /// Tear down the instance when its scope ends.
    abstract member Dispose : ExecutionState * 'a -> unit
    abstract member Lifetime : Lifetime
/// Context handed to errands during plan execution. Exposes the host's
/// <see cref="System.IServiceProvider"/> for application-level DI lookups, and
/// <see cref="GetPlanLocal"/> for resolving Rezoom's scoped coordination state.
and [<AbstractClass>] PlanContext() =
    /// The host's service provider. End-user code and library code can pull
    /// application services from this; e.g. Rezoom.SQL retrieves a registered
    /// <c>ConnectionProvider</c> from here.
    abstract member Services : IServiceProvider
    /// Resolve a PlanLocal-managed instance of 'a, materializing it via factory 'f
    /// on first access within its lifetime scope.
    abstract member GetPlanLocal<'f, 'a when 'f :> PlanLocal<'a> and 'f : (new : unit -> 'f)> : unit -> 'a
    /// Convenience: typed query against <see cref="Services"/>. Returns None when
    /// the service provider returns null for the requested type.
    member this.TryGetService<'a>() : 'a option =
        match this.Services.GetService(typeof<'a>) with
        | null -> None
        | v -> Some (Unchecked.unbox v : 'a)

/// PlanLocal helper for new()-constructable step-scoped coordinators.
/// Coordinator gets created during one execution step, is used by multiple errands,
/// then is disposed.
type StepLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit PlanLocal<'a>()
    override __.Lifetime = Lifetime.Step
    override __.Create(_) = new 'a()
    override __.Dispose(_, s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()

/// PlanLocal helper for new()-constructable execution-scoped coordinators.
/// Lives for the duration of one plan execution.
type ExecutionLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit PlanLocal<'a>()
    override __.Lifetime = Lifetime.Execution
    override __.Create(_) = new 'a()
    override __.Dispose(_, s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()
