module Data.Resumption.ApplyInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open Data.Resumption.MapInternals
open System

let rec applyTT (taskF : DataTask<'a -> 'b>) (taskA : DataTask<'a>) : DataTask<'b> =
    match taskF, taskA with
    | Result f, Result a ->
        Result (f a)
    | Result f, step ->
        mapT (fun a -> f a) step
    | step, Result a ->
        mapT (fun f -> f a) step
    | Step (pendingF, resumeF), Step (pendingA, resumeA) ->
        let pending = BatchPair (pendingF, pendingA)
        let onResponses =
            function
            | BatchPair (rspF, rspA) ->
                let mutable exnF : exn = null
                let mutable exnA : exn = null
                let mutable resF : DataTask<'a -> 'b> = Unchecked.defaultof<_>
                let mutable resA : DataTask<'a> = Unchecked.defaultof<_>
                try
                    resF <- resumeF rspF
                with
                | exn ->
                    exnF <- exn
                try
                    resA <- resumeA rspA
                with
                | exn -> exnA <- exn
                if isNull exnF && isNull exnA then
                    applyTT resF resA
                else if not (isNull exnF) && not (isNull exnA) then
                    raise (new AggregateException(exnF, exnA))
                else if isNull exnF then
                    abortTask resF exnA
                else
                    abortTask resA exnF
            | _ -> logicFault "Incorrect response shape for applied pair"
        Step (pending, onResponses)

let tuple2 (taskA : 'a DataTask) (taskB : 'b DataTask) : ('a * 'b) DataTask =
    applyTT
        (mapT (fun a b -> a, b) taskA)
        taskB

let tuple3 (taskA : 'a DataTask) (taskB : 'b DataTask) (taskC : 'c DataTask) : ('a * 'b * 'c) DataTask =
    applyTT
        (applyTT
            (mapT (fun a b c -> a, b, c) taskA)
            taskB)
        taskC

let tuple4
    (taskA : 'a DataTask)
    (taskB : 'b DataTask)
    (taskC : 'c DataTask)
    (taskD : 'd DataTask)
    : ('a * 'b * 'c * 'd) DataTask =
    applyTT
        (applyTT
            (applyTT
                (mapT (fun a b c d -> a, b, c, d) taskA)
                taskB)
            taskC)
        taskD