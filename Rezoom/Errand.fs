namespace Rezoom
open System
open System.Collections.Generic
open System.Threading.Tasks

/// Base class for all errands.
[<AbstractClass>]
type Errand() =
    /// Specifies caching information for the errand. This can be used for purposes other than
    /// caching as well, such as logging and replaying logged/serialized results.
    abstract member CacheInfo : CacheInfo
    /// A comparable object (or null) that represents the dynamic part of this errand's caching identity.
    /// This is separate because typically the rest of the cache info can be static for a given function that produces
    /// errands, and the argument can be the only thing that varies for each individual errand.
    abstract member CacheArgument : obj
    default __.CacheArgument = null
    /// A comparable object (or null).
    /// Errands with the same non-null sequence group will not be executed concurrently with one another.
    abstract member SequenceGroup : obj
    default __.SequenceGroup = null
    /// Given a `ServiceContext` with which to obtain execution-local or step-local shared services,
    /// adds the work this errand needs to do to a shared batch, and returns a function that can be called to
    /// force execution of the entire batch and return a task that will get this errand's result.
    /// Untyped version intended for internal use only.
    abstract member PrepareUntyped : ServiceContext -> (unit -> obj Task)

/// An errand implements an activity that might run in batches or have a cacheable result.
/// A SQL query, an HTTP request, or an FTP operation would all be good candidates to represent as errands.
/// An `Errand<'a>` returns data of type `'a`.
[<AbstractClass>]
type Errand<'a>() =
    inherit Errand()

/// Base class for errands that retrieve their data asynchronously using a `System.Threading.Task`.
[<AbstractClass>]
type AsynchronousErrand<'a>() =
    inherit Errand<'a>()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    /// Given a `ServiceContext` with which to obtain execution-local or step-local shared services,
    /// adds the work this errand needs to do to a shared batch, and returns a function that can be called to
    /// force execution of the entire batch and return a task that will get this errand's result.
    abstract member Prepare : ServiceContext -> (unit -> 'a Task)
    override this.PrepareUntyped(cxt) : unit -> obj Task =
        let typed = this.Prepare(cxt)
        fun () ->
            let t = typed()
            t.ContinueWith(AsynchronousErrand<'a>.BoxResult, TaskContinuationOptions.ExecuteSynchronously)

/// Base class for errands that retrieve their data synchronously (i.e. with a plain old function call).
[<AbstractClass>]
type SynchronousErrand<'a>() =
    inherit Errand<'a>()
    /// Given a `ServiceContext` with which to obtain execution-local or step-local shared services,
    /// adds the work this errand needs to do to a shared batch, and returns a function that can be called to
    /// force execution of the entire batch and return this errand's result.
    abstract member Prepare : ServiceContext -> (unit -> 'a)
    override this.PrepareUntyped(cxt) : unit -> obj Task =
        let sync = this.Prepare(cxt)
        fun () ->
            Task.FromResult(box (sync()))
        
        
    

