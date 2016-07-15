module Data.Resumption.BindInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open System

let inline bindTI bindTI task cont =
    match task with
    | Result r -> cont r
    | Step (pending, resume) ->
        Step (pending, fun responses -> bindTI (resume responses) cont)

let rec bindTF task cont =
    bindTI bindTF task cont
        
let inline bindTT (task : 'a DataTask) (cont : 'a -> 'b DataTask) : 'b DataTask =
    bindTI (bindTI bindTF) task cont
