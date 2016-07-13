[<AutoOpen>]
module Data.Resumption.DataTaskBuilder
open Data.Resumption.DataTaskInternals
open Data.Resumption
open System
open System.Threading.Tasks

type DataTaskBuilder() =
    member inline __.Zero() : unit Immediate = zero
    member inline __.Return(value) = retI value

    member inline __.ReturnFrom(task : _ DataTask) = task
    member inline __.ReturnFrom(task : _ Immediate) = task
    member inline __.ReturnFrom(task : _ Step) = task

    member inline __.Bind(task, cont) = BindInternals.bindII task cont
    member inline __.Bind(task, cont) = BindInternals.bindIS task cont
    member inline __.Bind(task, cont) = BindInternals.bindIT task cont
    member inline __.Bind(task, cont) = BindInternals.bindSI task cont
    member inline __.Bind(task, cont) = BindInternals.bindSS task cont
    member inline __.Bind(task, cont) = BindInternals.bindST task cont
    member inline __.Bind(task, cont) = BindInternals.bindTI task cont
    member inline __.Bind(task, cont) = BindInternals.bindTS task cont
    member inline __.Bind(task, cont) = BindInternals.bindTT task cont

    member inline __.Bind((a, b), cont) = BindInternals.bindTT (ApplyInternals.tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = BindInternals.bindTT (ApplyInternals.tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = BindInternals.bindTT (ApplyInternals.tuple4 a b c d) cont

    member inline __.Bind((a, b), cont) = BindInternals.bindTI (ApplyInternals.tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = BindInternals.bindTI (ApplyInternals.tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = BindInternals.bindTI (ApplyInternals.tuple4 a b c d) cont

    member inline __.Bind((a, b), cont) = BindInternals.bindTS (ApplyInternals.tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = BindInternals.bindTS (ApplyInternals.tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = BindInternals.bindTS (ApplyInternals.tuple4 a b c d) cont

    member inline __.Delay(task : unit -> _ DataTask) = task
    member inline __.Delay(task : unit -> _ Immediate) = fun () -> task().ToDataTask()
    member inline __.Delay(task : unit -> _ Step) = fun () -> task().ToDataTask()

    member inline __.Run(task : unit -> _ DataTask) = task()

    member inline __.Using(disposable : #IDisposable, body) =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        ExceptionInternals.finallyTT (fun () -> %%body disposable) dispose

    member inline __.TryFinally(task, onExit) = ExceptionInternals.finallyTT task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchTT task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchTI task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchTS task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchST task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchSI task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchSS task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchIT task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchIS task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchII task onExit

    member inline __.Combine(task, cont) = BindInternals.bindII task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindIS task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindIT task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindSI task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindSS task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindST task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindTI task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindTS task (fun _ -> cont())
    member inline __.Combine(task, cont) = BindInternals.bindTT task (fun _ -> cont())
//
//    member __.For(dataSequence, iteration) : datatask<unit> =
//        DataTaskMonad.forEachData dataSequence iteration
//    member __.For(sequence, iteration) : datatask<unit> =
//        DataTaskMonad.forEach sequence iteration
//    member __.For(Batch sequence, iteration) : datatask<unit> =
//        let tasks =
//            sequence |> Seq.map iteration
//        DataTaskMonad.sum tasks () (fun () () -> ())
//
//    member __.While(condition, iteration) =
//        DataTaskMonad.loop condition iteration

let datatask = new DataTaskBuilder()
