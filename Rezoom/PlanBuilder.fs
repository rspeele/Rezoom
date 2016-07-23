[<AutoOpen>]
module Rezoom.PlanBuilder
open Rezoom.Plan
open Rezoom
open System
open System.Collections.Generic
open System.Threading.Tasks

type PlanBuilder() =
    member inline __.Zero() : unit Plan = zero
    member inline __.Return(value) = ret value

    member inline __.ReturnFrom(task : _ Plan) = task
    member inline __.ReturnFrom(task : _ Step) = task

    member inline __.Bind(task, cont) = bind task cont

    member inline __.Bind((a, b), cont) = bind (tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = bind (tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = bind (tuple4 a b c d) cont

    member inline __.Delay(task : unit -> _ Plan) = task

    member inline __.Run(task : unit -> _ Plan) = task()

    member inline __.For(sequence : #seq<'a>, iteration : 'a -> unit Plan) =
        forM sequence iteration
    member __.For(BatchHint (sequence : #seq<'a>), iteration : 'a -> unit Plan) =
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

let plan = new PlanBuilder()
