namespace Rezoom.CS
open Rezoom
open System
open System.Threading.Tasks

[<AbstractClass>]
type AsynchronousDataRequest<'a>() =
    inherit Errand<'a>()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    abstract member Prepare : ServiceContext -> 'a Task Func
    override this.InternalPrepare(cxt) : unit -> obj Task =
        let typed = this.Prepare(cxt)
        fun () ->
            let t = typed.Invoke()
            t.ContinueWith(AsynchronousDataRequest<'a>.BoxResult, TaskContinuationOptions.ExecuteSynchronously)

[<AbstractClass>]
type SynchronousDataRequest<'a>() =
    inherit Errand<'a>()
    abstract member Prepare : ServiceContext -> 'a Func
    override this.InternalPrepare(cxt) : unit -> obj Task =
        let sync = this.Prepare(cxt)
        fun () ->
            Task.FromResult(box (sync.Invoke()))