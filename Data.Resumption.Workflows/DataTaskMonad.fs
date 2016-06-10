/// Provides an F# compatibility layer over the extension methods in Data.Resumption.DataTask.
/// This handles converting between Funcs and FSharpFuncs.
module Data.Resumption.DataTaskMonad
open Data.Resumption
open System

let ret value = DataTask.Return(value)

let zero = ret ()

let bind (task : IDataTask<'a>) (continuation : 'a -> IDataTask<'b>) =
    DataTask.SelectMany(task, Func<_, _>(continuation))

let map (mapping : 'a -> 'b) (task : IDataTask<'a>) =
    DataTask.Select(task, Func<_, _>(mapping))

let apply (functionTask : IDataTask<'a -> 'b>) (inputTask : IDataTask<'a>) =
    DataTask.Apply(functionTask |> map (fun f -> Func<'a, 'b>(f)), inputTask)

let sum (tasks : IDataTask<'a> seq) (initial : 's) (add : 's -> 'a -> 's) =
    DataTask.Sum(tasks, initial, fun sum extra -> add sum extra)

let tryWith (wrapped : unit -> IDataTask<'a>) (exceptionHandler : exn -> IDataTask<'a>) =
    try
        DataTask.TryCatch(wrapped(), Func<_, _>(exceptionHandler))
    with
    | ex -> exceptionHandler(ex)

let tryFinally (wrapped : unit -> IDataTask<'a>) (onExit : unit -> unit) =
        try
            DataTask.TryFinally(wrapped(), Action(onExit))
        with
        | _ ->
            onExit()
            reraise()

let combineStrict (taskA : IDataTask<'a>) (taskB : unit -> IDataTask<'b>) =
    bind taskA (fun _ -> taskB())

let combineLazy (taskA : IDataTask<'a>) (taskB : unit -> IDataTask<'b>) =
    apply (map (fun _ b -> b) taskA) (taskB())

let rec loop (condition : unit -> bool) (iteration : unit -> IDataTask<'a>) =
    if condition() then
        bind (iteration()) (fun _ -> loop condition iteration)
    else
        zero