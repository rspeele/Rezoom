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
let inline catchTS (wrapped : unit -> 'a DataTask) (catcher : exn -> 'a Step) =
    catchTT wrapped (fun ex -> %%catcher ex)
let inline catchTI (wrapped : unit -> 'a DataTask) (catcher : exn -> 'a Immediate) =
    catchTT wrapped (fun ex -> %%catcher ex)
let inline catchST (wrapped : unit -> 'a Step) (catcher : exn -> 'a DataTask) =
    catchTT (fun () -> %%wrapped()) catcher
let inline catchIT (wrapped : unit -> 'a Immediate) (catcher : exn -> 'a DataTask) =
    catchTT (fun () -> %%wrapped()) catcher
let inline catchIS (wrapped : unit -> 'a Immediate) (catcher : exn -> 'a Step) =
    catchTT (fun () -> %%wrapped()) (fun ex -> %%catcher ex)

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