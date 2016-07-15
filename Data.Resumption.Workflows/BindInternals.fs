module Data.Resumption.BindInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let rec bindTT (task : 'a DataTask) (cont : 'a -> 'b DataTask) : 'b DataTask =
    let step = task.Step
    if isNull step then cont task.Immediate else
    let res = step.Resume
    let onResponses (responses : Responses) =
        bindTT (res responses) cont
    DataTask<'b>(Step(step.Pending, onResponses))

    
