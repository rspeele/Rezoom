[<AutoOpen>]
module Data.Resumption.DataTaskBuilder
open Data.Resumption.DataTaskInternals
open Data.Resumption
open System
open System.Collections.Generic
open System.Threading.Tasks

type DataTaskBuilder() =
    member inline __.Zero() : unit DataTask = zero
    member inline __.Return(value) = retI value

    member inline __.ReturnFrom(task : _ DataTask) = task
    member inline __.ReturnFrom(task : _ Step) = task

    member inline __.Bind(task, cont) = BindInternals.bindTT task cont

    member inline __.Bind((a, b), cont) = BindInternals.bindTT (ApplyInternals.tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = BindInternals.bindTT (ApplyInternals.tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = BindInternals.bindTT (ApplyInternals.tuple4 a b c d) cont

    member inline __.Delay(task : unit -> _ DataTask) = task

    member inline __.Run(task : unit -> _ DataTask) = task()

    member inline __.For(sequence : #seq<'a>, iteration : 'a -> unit DataTask) =
        LoopInternals.forM sequence iteration
    member __.For(BatchHint (sequence : #seq<'a>), iteration : 'a -> unit DataTask) =
        LoopInternals.forA sequence iteration

    member inline __.Using(disposable : #IDisposable, body) =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        ExceptionInternals.finallyTT (fun () -> body disposable) dispose

    member inline __.TryFinally(task, onExit) = ExceptionInternals.finallyTT task onExit
    member inline __.TryWith(task, onExit) = ExceptionInternals.catchTT task onExit

    member inline __.Combine(task, cont) = BindInternals.bindTT task (fun _ -> cont())

let datatask = new DataTaskBuilder()
