module Data.Resumption.DataTaskInternals
open System

let inline (~%%) (a : ^a when ^a : (member ToDataTask : unit -> DataTask< ^b >)) =
    (^a : (member ToDataTask : unit -> DataTask< ^b >)a)

let abortTask (task : 'a DataTask) (reason : exn) : 'b =
    if isNull task.Step then dispatchRaise reason else
    try
        ignore <| task.Step.Resume(BatchAbort)
        dispatchRaise reason
    with
    | DataTaskAbortException _ -> dispatchRaise reason
    | exn -> raise (new AggregateException(reason, exn))