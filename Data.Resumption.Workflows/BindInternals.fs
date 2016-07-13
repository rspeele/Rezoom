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

let inline bindST (task : 'a Step) (cont : 'a -> 'b DataTask) : 'b Step =
    let res = task.Resume
    let onResponses (responses : Responses) =
        bindTT (res responses) cont
    Step(task.Pending, onResponses)

let inline bindIT (task : 'a Immediate) (cont : 'a -> 'b DataTask) : 'b DataTask =
    cont task.Immediate

let inline bindII (task : 'a Immediate) (cont : 'a -> 'b Immediate) : 'b Immediate =
    cont task.Immediate

let inline bindIS (task : 'a Immediate) (cont : 'a -> 'b Step) : 'b Step =
    cont task.Immediate

let inline bindTI (task : 'a DataTask) (cont : 'a -> 'b Immediate) : 'b DataTask =
    bindTT task (fun a -> %%cont a)

let inline bindTS (task : 'a DataTask) (cont : 'a -> 'b Step) : 'b DataTask =
    bindTT task (fun a -> %%cont a)

let inline bindSI (task : 'a Step) (cont : 'a -> 'b Immediate) =
    let res = task.Resume
    let onResponses (responses : Responses) =
        bindTI (res responses) cont
    Step(task.Pending, onResponses)

let inline bindSS (task : 'a Step) (cont : 'a -> 'b Step) =
    let res = task.Resume
    let onResponses (responses : Responses) =
        bindTS (res responses) cont
    Step(task.Pending, onResponses)

    
