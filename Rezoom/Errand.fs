namespace Rezoom
open System
open System.Collections.Generic
open System.Threading.Tasks

[<AbstractClass>]
type Errand() =
    abstract member CacheInfo : CacheInfo
    default __.CacheInfo = null
    abstract member CacheArgument : Key Nullable
    default __.CacheArgument = Nullable()
    abstract member SequenceGroup : Key Nullable
    default __.SequenceGroup = Nullable()
    abstract member PrepareUntyped : ServiceContext -> (unit -> obj Task)

[<AbstractClass>]
type Errand<'a>() =
    inherit Errand()

[<AbstractClass>]
type AsynchronousErrand<'a>() =
    inherit Errand<'a>()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    abstract member Prepare : ServiceContext -> (unit -> 'a Task)
    override this.PrepareUntyped(cxt) : unit -> obj Task =
        let typed = this.Prepare(cxt)
        fun () ->
            let t = typed()
            t.ContinueWith(AsynchronousErrand<'a>.BoxResult, TaskContinuationOptions.ExecuteSynchronously)

[<AbstractClass>]
type SynchronousErrand<'a>() =
    inherit Errand<'a>()
    abstract member Prepare : ServiceContext -> (unit -> 'a)
    override this.PrepareUntyped(cxt) : unit -> obj Task =
        let sync = this.Prepare(cxt)
        fun () ->
            Task.FromResult(box (sync()))
        
        
    

