module Data.Resumption.MapInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let rec mapS (f : 'a -> 'b) (task : 'a Step) : 'b Step =
    let res = task.Resume
    Step(task.Pending, fun responses -> mapT f (res responses))
and mapT (f : 'a -> 'b) (task : 'a DataTask): 'b DataTask =
    if isNull task.Step then DataTask<'b>(f task.Immediate) else
    DataTask<'b>(mapS f task.Step)