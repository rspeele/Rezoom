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
    abstract member Prepare : ServiceContext -> (unit -> obj Task)

type DataRequest<'a>() =
    inherit DataRequest()
    abstract member Prepare : ServiceContext -> (unit -> 'a Task)
    override this.Prepare(cxt) : unit -> obj Task =
        let typed = this.Prepare(cxt)
        fun () ->
            let t = typed()
            t.ContinueWith()

[<AbstractClass>]
type SynchronousDataRequest() =
    inherit DataRequest()
    abstract member Prepare : ServiceContext -> (unit -> obj)
    override __.Prepare(cxt) =
        
    

