[<AutoOpen>]
module Data.Resumption.Builder
open Data.Resumption
open System
open System.Threading.Tasks

/// Wrapper type to indicate computations should be evaluated in strict sequence
/// with `DataMonad.bind` instead of concurrently combined with `DataMonad.apply`.
type Strict<'a> = Strict of 'a

/// Mark a data task or sequence to be evaluated in strict sequence.
let strict x = Strict x

/// Convert a TPL task to a data task.
let await (task : unit -> Task<'a>) = (Func<_>(task)).ToDataTask()

type DataTaskBuilder() =
    member __.Zero : IDataTask<unit> =
        DataMonad.zero

    member __.Return(value) : IDataTask<_> =
        DataMonad.ret value
    member __.ReturnFrom(task : IDataTask<_>) : IDataTask<_> =
        task

    member __.Bind(Strict task, continuation) : IDataTask<_> =
        DataMonad.bind task continuation
    member __.Bind(task : IDataTask<'a>, continuation) : IDataTask<_> =
        if typeof<'a> = typeof<unit> then
            DataMonad.apply
                ((fun _ b -> b) <@> task)
                (continuation Unchecked.defaultof<'a>)
        else
            DataMonad.bind task continuation
    member __.Bind((taskA, taskB), continuation) =
        DataMonad.bind (datatuple2 taskA taskB) continuation
    member __.Bind((taskA, taskB, taskC), continuation) =
        DataMonad.bind (datatuple3 taskA taskB taskC) continuation
    member __.Bind((taskA, taskB, taskC, taskD), continuation) =
        DataMonad.bind (datatuple4 taskA taskB taskC taskD) continuation

    member __.Using(disposable : #IDisposable, body) =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        try
            DataMonad.tryFinally (body disposable) dispose
            // all try/with has extra handling because the code
            // that creates the body of the try could itself fail
        with
        | _ ->
            dispose()
            reraise()

    member __.Combine(Strict task, continuation) : IDataTask<_> =
        DataMonad.combineStrict task continuation
    member __.Combine(task, continuation) : IDataTask<_> =
        DataMonad.combineLazy task continuation

    member __.TryFinally(task, onExit) : IDataTask<_> =
        try
            DataMonad.tryFinally (task()) onExit
        with
        | _ ->
            onExit()
            reraise()
    member __.TryWith(task, exceptionHandler) : IDataTask<_> =
        try
            DataMonad.tryWith (task()) exceptionHandler
        with
        | ex -> exceptionHandler(ex)

    member __.For(Strict sequence, iteration) : IDataTask<unit> =
        let binder soFar nextElement =
            DataMonad.combineStrict soFar (fun () -> iteration nextElement)
        sequence
        |> Seq.fold binder DataMonad.zero
    member __.For(sequence, iteration) : IDataTask<unit> =
        let tasks =
            sequence |> Seq.map iteration
        DataMonad.sum tasks () (fun () () -> ())

    member __.While(condition, iteration) =
        DataMonad.loop condition iteration

    member __.Delay(f : unit -> IDataTask<_>) = f
    member __.Run(f : unit -> IDataTask<_>) = f()

let datatask = new DataTaskBuilder()
