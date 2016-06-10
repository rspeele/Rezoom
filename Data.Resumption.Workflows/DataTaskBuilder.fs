[<AutoOpen>]
module Data.Resumption.DataTaskBuilder
open Data.Resumption
open System
open System.Threading.Tasks

type DataTaskBuilder() =
    member __.Zero() : IDataTask<unit> =
        DataTaskMonad.zero

    member __.Return(value) : IDataTask<_> =
        DataTaskMonad.ret value
    member __.ReturnFrom(task : IDataTask<_>) : IDataTask<_> =
        task

    member __.Bind(Strict task, continuation) : IDataTask<_> =
        DataTaskMonad.bind task continuation
    member __.Bind(task : IDataTask<'a>, continuation) : IDataTask<_> =
        if typeof<'a> = typeof<unit> then
            DataTaskMonad.apply
                ((fun _ b -> b) <@> task)
                (continuation Unchecked.defaultof<'a>)
        else
            DataTaskMonad.bind task continuation
    member __.Bind((taskA, taskB), continuation) =
        DataTaskMonad.bind (datatuple2 taskA taskB) continuation
    member __.Bind((taskA, taskB, taskC), continuation) =
        DataTaskMonad.bind (datatuple3 taskA taskB taskC) continuation
    member __.Bind((taskA, taskB, taskC, taskD), continuation) =
        DataTaskMonad.bind (datatuple4 taskA taskB taskC taskD) continuation

    member __.Using(disposable : #IDisposable, body) =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        DataTaskMonad.tryFinally (fun () -> body disposable) dispose

    member __.Combine(Strict task, continuation) : IDataTask<_> =
        DataTaskMonad.combineStrict task continuation
    member __.Combine(task, continuation) : IDataTask<_> =
        DataTaskMonad.combineLazy task continuation

    member __.TryFinally(task : unit -> IDataTask<_>, onExit) : IDataTask<_> =
        DataTaskMonad.tryFinally task onExit
    member __.TryWith(task : unit -> IDataTask<_>, exceptionHandler) : IDataTask<_> =
        DataTaskMonad.tryWith task exceptionHandler

    member __.For(dataSequence, iteration) : IDataTask<unit> =
        DataTaskMonad.forEach dataSequence iteration
    member __.For(Strict sequence, iteration) : IDataTask<unit> =
        let binder soFar nextElement =
            DataTaskMonad.combineStrict soFar (fun () -> iteration nextElement)
        sequence
        |> Seq.fold binder DataTaskMonad.zero
    member __.For(sequence, iteration) : IDataTask<unit> =
        let tasks =
            sequence |> Seq.map iteration
        DataTaskMonad.sum tasks () (fun () () -> ())

    member __.While(condition, iteration) =
        DataTaskMonad.loop condition iteration

    member __.Delay(f : unit -> IDataTask<_>) = f
    member __.Run(f : unit -> IDataTask<_>) = f()

let datatask = new DataTaskBuilder()
