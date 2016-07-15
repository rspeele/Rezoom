module Data.Resumption.ExceptionInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let rec catchTT (wrapped : unit -> 'a DataTask) (catcher : exn -> 'a DataTask) =
    try
        let wrapped = wrapped()
        let step = wrapped.Step
        if isNull step then wrapped else
        let onResponses (responses : Responses) =
            catchTT (fun () -> step.Resume responses) catcher
        DataTask<'a>(Step(step.Pending, onResponses))
    with
    | DataTaskAbortException _ -> reraise() // don't let them catch these
    | ex -> catcher(ex)

let rec finallyTT (wrapped : unit -> 'a DataTask) (onExit : unit -> unit) =
    let mutable cleanExit = false
    let task =
        try
            let wrapped = wrapped()
            let step = wrapped.Step
            if isNull step then
                cleanExit <- true
                wrapped
            else
                let onResponses (responses : Responses) =
                    finallyTT (fun () -> step.Resume responses) onExit
                DataTask<'a>(Step(step.Pending, onResponses))
        with
        | DataTaskAbortException _ -> reraise()
        | ex ->
            onExit()
            reraise()
    if cleanExit then
        // run outside of the try/catch so we don't risk recursion
        onExit()
    task