/// Provides an F# compatibility layer over the extension methods in Data.Resumption.DataTask.
/// This handles converting between Funcs and FSharpFuncs.
module Data.Resumption.DataTaskMonad
open Data.Resumption
open System

let ret value = DataTask.Return(value)

let zero = ret ()

let bind (task : datatask<'a>) (continuation : 'a -> datatask<'b>) =
    DataTask.SelectMany(task, Func<_, _>(continuation))

let map (mapping : 'a -> 'b) (task : datatask<'a>) =
    DataTask.Select(task, Func<_, _>(mapping))

let apply (functionTask : datatask<'a -> 'b>) (inputTask : datatask<'a>) =
    DataTask.Apply(functionTask |> map (fun f -> Func<'a, 'b>(f)), inputTask)

let sum (tasks : datatask<'a> seq) (initial : 's) (add : 's -> 'a -> 's) =
    DataTask.Sum(tasks, initial, fun sum extra -> add sum extra)

let tryWith (wrapped : unit -> datatask<'a>) (exceptionHandler : exn -> datatask<'a>) =
    try
        DataTask.TryCatch(wrapped(), Func<_, _>(exceptionHandler))
    with
    | ex -> exceptionHandler(ex)

let tryFinally (wrapped : unit -> datatask<'a>) (onExit : unit -> unit) =
        try
            DataTask.TryFinally(wrapped(), Action(onExit))
        with
        | _ ->
            onExit()
            reraise()

let combineStrict (taskA : datatask<'a>) (taskB : unit -> datatask<'b>) =
    bind taskA (fun _ -> taskB())

let combineLazy (taskA : datatask<'a>) (taskB : unit -> datatask<'b>) =
    apply (map (fun _ b -> b) taskA) (taskB())

let rec loop (condition : unit -> bool) (iteration : unit -> datatask<'a>) =
    if condition() then
        bind (iteration()) (fun _ -> loop condition iteration)
    else
        zero

let forEach (sequence : 'a seq) (iteration : 'a -> unit datatask) =
    DataTask.ForEach(sequence, Func<_, _>(iteration))

let forEachData (sequence : IDataEnumerable<'a>) (iteration : 'a -> datatask<unit>) =
    DataTask.ForEach(sequence, Func<_, _>(iteration))