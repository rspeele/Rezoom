[<AutoOpen>]
module Rezoom.PlanBuilder
open Rezoom.Plan
open Rezoom
open System

type PlanBuilder() =
    member inline __.Zero() : unit Plan = zero
    member inline __.Return(value : 'a) : 'a Plan = ret value

    member inline __.ReturnFrom(plan : 'a Plan) : 'a Plan = plan

    member inline __.Combine(plan : unit Plan, cont : unit -> 'b Plan) : 'b Plan = combine plan cont
    member inline __.Bind(plan : 'a Plan, cont : 'a -> 'b Plan) : 'b Plan = bind plan cont

    member inline __.Bind((a, b), cont) = bind (tuple2 a b) cont
    member inline __.Bind((a, b, c), cont) = bind (tuple3 a b c) cont
    member inline __.Bind((a, b, c, d), cont) = bind (tuple4 a b c d) cont

    member inline __.Delay(delayed : unit -> 'a Plan) : unit -> 'a Plan = delayed
    member inline __.Run(plan : unit -> 'a Plan) : 'a Plan = Plan.delayed plan

    member inline __.For(sequence : #seq<'a>, iteration : 'a -> unit Plan) : unit Plan =
        forM sequence iteration
    member __.For(BatchHint (sequence : #seq<'a>), iteration : 'a -> unit Plan) : unit Plan =
        forA sequence iteration

    member inline __.Using(disposable : #IDisposable, body : #IDisposable -> 'a Plan) : 'a Plan =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        tryFinally (fun () -> body disposable) dispose

    member inline __.TryFinally(body : unit -> 'a Plan, onExit : unit -> unit) : 'a Plan = tryFinally body onExit
    member inline __.TryWith(body : unit -> 'a Plan, onExn : exn -> 'a Plan) : 'a Plan = tryCatch body onExn

let plan = new PlanBuilder()
