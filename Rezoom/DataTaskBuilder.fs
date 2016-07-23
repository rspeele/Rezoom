[<AutoOpen>]
module Rezoom.DataTaskBuilder
open Rezoom.DataTask
open Rezoom
open System
open System.Collections.Generic
open System.Threading.Tasks

type DataTaskBuilder() =
    member inline __.Zero() : unit DataTask = zero
    member inline __.Return(value) = ret value

    member inline __.ReturnFrom(task : _ DataTask) = task
    member inline __.ReturnFrom(task : _ Step) = task

    member inline __.Bind(task, cont) = bind task cont

    member inline __.Bind((a, b), cont) = bind (tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = bind (tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = bind (tuple4 a b c d) cont

    member inline __.Delay(task : unit -> _ DataTask) = task

    member inline __.Run(task : unit -> _ DataTask) = task()

    member inline __.For(sequence : #seq<'a>, iteration : 'a -> unit DataTask) =
        forM sequence iteration
    member __.For(BatchHint (sequence : #seq<'a>), iteration : 'a -> unit DataTask) =
        forA sequence iteration

    member inline __.Using(disposable : #IDisposable, body) =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        tryFinally (fun () -> body disposable) dispose

    member inline __.TryFinally(task, onExit) = tryFinally task onExit
    member inline __.TryWith(task, onExit) = tryCatch task onExit

    member inline __.Combine(task, cont) = bind task (fun _ -> cont())

let datatask = new DataTaskBuilder()
