module Data.Resumption.ExceptionInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let rec catchTT (wrapped : unit -> 'a DataTask) (catcher : exn -> 'a DataTask) =
    try
        match wrapped() with
        | Result _ as result -> result
        | Step (pending, resume) ->
            let onResponses (responses : Responses) =
                catchTT (fun () -> resume responses) catcher
            Step (pending, onResponses)
    with
    | DataTaskAbortException _ -> reraise() // don't let them catch these
    | ex -> catcher(ex)

let rec finallyTT (wrapped : unit -> 'a DataTask) (onExit : unit -> unit) =
    let mutable cleanExit = false
    let task =
        try
            match wrapped() with
            | Result _ as result ->
                cleanExit <- true
                result
            | Step (pending, resume) ->
                let onResponses (responses : Responses) =
                    finallyTT (fun () -> resume responses) onExit
                Step (pending, onResponses)
        with
        | ex ->
            onExit()
            reraise()
    if cleanExit then
        // run outside of the try/catch so we don't risk recursion
        onExit()
    task