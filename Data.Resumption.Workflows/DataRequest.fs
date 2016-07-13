namespace Data.Resumption
open System.Threading.Tasks

[<AbstractClass>]
type DataRequest() =
    abstract member Identity : obj
    default __.Identity = null
    abstract member DataSource : obj
    default __.DataSource = null
    abstract member SequenceGroup : obj
    default __.SequenceGroup = null
    abstract member Idempotent : bool
    default __.Idempotent = false
    abstract member Mutation : bool
    default __.Mutation = true
    abstract member Parallelizable : bool
    default __.Parallelizable = false
    abstract member Prepare : ServiceContext -> (unit -> obj Task)

[<AbstractClass>]
type DataRequest<'a>() =
    inherit DataRequest()
    static member private BoxResult(task : 'a Task) =
        box task.Result
    abstract member Prepare : ServiceContext -> (unit -> 'a Task)
    override this.Prepare(cxt) : unit -> obj Task =
        let typed = this.Prepare(cxt)
        fun () ->
            let t = typed()
            t.ContinueWith(DataRequest<'a>.BoxResult)

[<AbstractClass>]
type SynchronousDataRequest<'a>() =
    inherit DataRequest<'a>()
    abstract member Prepare : ServiceContext -> (unit -> 'a)
    override this.Prepare(cxt) : unit -> 'a Task =
        let sync = this.Prepare(cxt)
        fun () ->
            Task.FromResult(sync())
        
        
    

