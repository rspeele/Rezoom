module Data.Resumption.DataTaskInternals
open System

let inline retI (result : 'a) = Result result
let zero = retI ()

let abortTask (task : 'a DataTask) (reason : exn) : 'b =
    match task with
    | Step (_, resume) ->
        try
            ignore <| resume BatchAbort
            dispatchRaise reason
        with
        | DataTaskAbortException _ -> dispatchRaise reason
        | exn -> raise (new AggregateException(reason, exn))
    | _ ->
        dispatchRaise reason

let fromDataRequest (request : DataRequest<'a>) : Step<'a> =
    BatchLeaf (request :> DataRequest), function
        | BatchLeaf (RetrievalSuccess suc) -> Result (Unchecked.unbox suc : 'a)
        | BatchLeaf (RetrievalException exn) -> dispatchRaise exn
        | _ -> logicFault "Invalid response shape for data request"