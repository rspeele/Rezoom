module Data.Resumption.ApplyInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open Data.Resumption.MapInternals
open System

let rec applyTT (taskF : DataTask<'a -> 'b>) (taskA : DataTask<'a>) : DataTask<'b> =
    if isNull taskF.Step then
        let f = taskF.Immediate
        mapT (fun a -> f a) taskA
    else if isNull taskA.Step then
        let a = taskA.Immediate
        mapT (fun f -> f a) taskF
    else
        let stepF = taskF.Step
        let stepA = taskA.Step
        let pending = BatchPair (stepF.Pending, stepA.Pending)
        let onResponses =
            function
            | BatchPair (rspF, rspA) ->
                let mutable exnF : exn = null
                let mutable exnA : exn = null
                let mutable resF : DataTask<'a -> 'b> = Unchecked.defaultof<_>
                let mutable resA : DataTask<'a> = Unchecked.defaultof<_>
                try
                    resF <- stepF.Resume rspF
                with
                | exn ->
                    exnF <- exn
                try
                    resA <- stepA.Resume rspA
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
        DataTask<'b>(Step(pending, onResponses))

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