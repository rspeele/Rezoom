module Data.Resumption.MapInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let inline mapTI mapS f task =
    match task with
    | Result r -> Result (f r)
    | Step s -> Step (mapS f s)

let rec mapS (f : 'a -> 'b) ((pending, resume) : 'a Step) : 'b Step =
    pending, fun responses -> mapTI mapS f (resume responses)
and inline mapT (f : 'a -> 'b) (task : 'a DataTask): 'b DataTask =
    mapTI mapS f task