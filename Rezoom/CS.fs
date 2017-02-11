namespace Rezoom.CS
open Rezoom
open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices

[<AbstractClass>]
type AsynchronousErrand<'a>() =
    inherit Errand<'a>()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    abstract member Prepare : ServiceContext -> Func<CancellationToken, 'a Task>
    override this.PrepareUntyped(cxt) : CancellationToken -> obj Task =
        let typed = this.Prepare(cxt)
        fun token ->
            let t = typed.Invoke(token)
            t.ContinueWith(AsynchronousErrand<'a>.BoxResult, TaskContinuationOptions.ExecuteSynchronously)

[<AbstractClass>]
type SynchronousErrand<'a>() =
    inherit Errand<'a>()
    abstract member Prepare : ServiceContext -> Func<'a>
    override this.PrepareUntyped(cxt) : CancellationToken -> obj Task =
        let sync = this.Prepare(cxt)
        fun _ ->
            Task.FromResult(box (sync.Invoke()))

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member ToPlan(request : Errand<'a>) =
        Plan.ofErrand request
