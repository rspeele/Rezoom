module Data.Resumption.DataTaskInternals
open System

let inline retI (result : 'a) = Result result
let zero = retI ()

let abort() = raise (DataTaskAbortException "Task aborted")

let abortSteps (steps : 'a Step seq) (reason : exn) : 'b =
    let exns = new ResizeArray<_>()
    exns.Add(reason)
    for _, resume in steps do
        try
            ignore <| resume BatchAbort // this should fail with a DataTaskAbortException
        with
        | DataTaskAbortException _ -> ()
        | exn -> exns.Add(exn)
    if exns.Count > 1 then raise (aggregate exns)
    else dispatchRaise reason

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
        | BatchAbort -> abort()
        | BatchPair _
        | BatchMany _ -> logicFault "Incorrect response shape for data request"