[<AutoOpen>]
module private Rezoom.InternalUtilities
open System
open System.Collections.Generic
open System.Runtime.ExceptionServices

let aggregate (exns : exn ICollection) =
    if exns.Count = 1 then exns |> Seq.head
    else
        let exns =
            seq {
                for exn in exns do
                    match exn with
                    | :? AggregateException as agg -> yield! agg.InnerExceptions
                    | _ -> yield exn
            }
        new AggregateException(exns) :> exn

let inline notSupported (reason : string) =
    raise (new NotSupportedException(reason))

let inline logicFault (reason : string) =
    raise (LogicFaultException reason)

let inline dispatchRaise (ex : exn) : 'a =
    ExceptionDispatchInfo.Capture(ex).Throw()
    raise ex // shouldn't get here

