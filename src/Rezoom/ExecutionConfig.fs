namespace Rezoom
open System
open System.Threading
open System.Threading.Tasks
open Rezoom.Caching

type ExecutionLog() =
    abstract member OnBeginStep : unit -> unit
    default __.OnBeginStep() = ()
    abstract member OnEndStep : unit -> unit
    default __.OnEndStep() = ()
    abstract member OnPreparingErrand : Errand -> unit
    default __.OnPreparingErrand(_) = ()
    abstract member OnPreparedErrand : Errand -> unit
    default __.OnPreparedErrand(_) = ()

type ConsoleExecutionLog() =
    inherit ExecutionLog()
    let write str =
        Diagnostics.Debug.WriteLine(str)
        Console.WriteLine(str)
    override __.OnBeginStep() = write "Step {"
    override __.OnEndStep() = write "} // end step"
    override __.OnPreparingErrand(errand) =
        write ("    Preparing errand " + string errand)
    override __.OnPreparedErrand(errand) =
        write ("    Prepared errand " + string errand)

type ExecutionInstance(log : ExecutionLog) =
    member __.Log = log
    abstract member RunErrand : Errand * ServiceContext -> (CancellationToken -> obj Task)
    default __.RunErrand(errand, context) = errand.PrepareUntyped context
    abstract member CreateCache : unit -> Cache
    default __.CreateCache() = upcast DefaultCache()

type ExecutionConfig =
    {   ServiceConfig : IServiceConfig
        Instance : unit -> ExecutionInstance
    }
    static member Default =
        {   ServiceConfig = { new IServiceConfig with member __.TryGetConfig() = None }
            Instance = fun () -> ExecutionInstance(ExecutionLog())
        }