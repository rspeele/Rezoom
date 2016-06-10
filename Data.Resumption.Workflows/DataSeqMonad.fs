/// Provides an F# compatibility layer over the extension methods in Data.Resumption.DataEnumerable.
/// This handles converting between Funcs and FSharpFuncs.
module Data.Resumption.DataSeqMonad
open Data.Resumption
open System

let zero () = DataEnumerable.Zero()
let yieldA a = DataEnumerable.Yield(a)
let yieldM xs = DataEnumerable.YieldMany(xs)

let bind (enumerable : IDataEnumerable<'a>) (continuation : 'a -> IDataEnumerable<'b>) =
    DataEnumerable.SelectMany(enumerable, Func<_, _>(continuation))

let bindTask (task : IDataTask<'a>) (continuation : 'a -> IDataEnumerable<'b>) =
    DataEnumerable.SelectMany(task, Func<_, _>(continuation))

let combine (first : IDataEnumerable<'a>) (next : unit -> IDataEnumerable<'a>) =
    DataEnumerable.Combine(first, Func<_>(next))

let tryWith (wrapped : unit -> IDataEnumerable<'a>) (exceptionHandler : exn -> IDataEnumerable<'a>) =
    try
        DataEnumerable.TryCatch(wrapped(), Func<_, _>(exceptionHandler))
    with
    | ex -> exceptionHandler(ex)

let tryFinally (wrapped : unit -> IDataEnumerable<'a>) (onExit : unit -> unit) =
    try
        DataEnumerable.TryFinally(wrapped(), Action(onExit))
    with
    | _ ->
        onExit()
        reraise()

let rec loop (condition : unit -> bool) (iteration : unit -> IDataEnumerable<'a>) =
    if condition() then
        combine (iteration()) (fun () -> loop condition iteration)
    else
        zero()