[<AutoOpen>]
module Data.Resumption.DataTaskBuilder
open Data.Resumption
open System
open System.Threading.Tasks

type DataTaskBuilder() =
    member __.Zero() : datatask<unit> =
        DataTaskMonad.zero

    member __.Return(value) : datatask<_> =
        DataTaskMonad.ret value
    member __.ReturnFrom(task : datatask<_>) : datatask<_> =
        task

    member __.Bind(Weave (task : datatask<unit>), continuation) : datatask<_> =
        DataTaskMonad.combineWeave task continuation
    member __.Bind(task : datatask<'a>, continuation) : datatask<_> =
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

    member __.Combine(Weave task, continuation) : datatask<_> =
        DataTaskMonad.combineWeave task continuation
    member __.Combine(task, continuation) : datatask<_> =
        DataTaskMonad.combineStrict task continuation

    member __.TryFinally(task : unit -> datatask<_>, onExit) : datatask<_> =
        DataTaskMonad.tryFinally task onExit
    member __.TryWith(task : unit -> datatask<_>, exceptionHandler) : datatask<_> =
        DataTaskMonad.tryWith task exceptionHandler

    member __.For(dataSequence, iteration) : datatask<unit> =
        DataTaskMonad.forEachData dataSequence iteration
    member __.For(sequence, iteration) : datatask<unit> =
        DataTaskMonad.forEach sequence iteration
    member __.For(Weave sequence, iteration) : datatask<unit> =
        let tasks =
            sequence |> Seq.map iteration
        DataTaskMonad.sum tasks () (fun () () -> ())

    member __.While(condition, iteration) =
        DataTaskMonad.loop condition iteration

    member __.Delay(f : unit -> datatask<_>) = f
    member __.Run(f : unit -> datatask<_>) = f()

let datatask = new DataTaskBuilder()
