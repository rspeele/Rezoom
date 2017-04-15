module Rezoom.Test.TestCaching
open Rezoom
open NUnit.Framework
open FsUnit

[<Test>]
let ``strict cached pair`` () =
    {   Task = fun () ->
            plan {
                let! q1 = send "q"
                let! q2 = send "q"
                return q1 + q2
            }
        Batches =
            [   [ "q" ]
            ]
        Result = Good "qq"
    } |> test

[<Test>]
let ``concurrent cached pair`` () =
    {   Task = fun () ->
            plan {
                let! q1, q2 = send "q", send "q"
                return q1 + q2
            }
        Batches =
            [   [ "q" ]
            ]
        Result = Good "qq"
    } |> test

[<Test>]
let ``chaining cached concurrency`` () =
    let testTask x =
        plan {
            let! a = send (x + "1")
            let! b = send (x + "2")
            let! c = send (x + "3")
            return a + b + c
        }
    {   Task = fun () ->
            plan {
                let! x1, x2, x3 =
                    testTask "x", testTask "x", testTask "x"
                return x1 + " " + x2 + " " + x3
            }
        Batches =
            [   [ "x1" ]
                [ "x2" ]
                [ "x3" ]
            ]
        Result = Good "x1x2x3 x1x2x3 x1x2x3"
    } |> test

[<Test>]
let ``still valid after other`` () =
    {   Task = fun () ->
            plan {
                let! q1 = send "q"
                let! q2 = send "q"
                let! m = send "x"
                let! q3 = send "q"
                return q1 + q2 + m + q3
            }
        Batches =
            [   [ "q" ]
                [ "x" ]
            ]
        Result = Good "qqxq"
    } |> test

[<Test>]
let ``invalidation invalidates`` () =
    {   Task = fun () ->
            plan {
                let! q1 = send "q"
                let! q2 = send "q"
                let! m = mutate "x"
                let! q3 = send "q"
                let! q4 = send "q"
                return q1 + q2 + m + q3 + q4
            }
        Batches =
            [   [ "q" ]
                [ "x" ]
                [ "q" ]
            ]
        Result = Good "qqxqq"
    } |> test

[<Test>]
let ``deferred execution`` () =
    let px =
        plan {
            let! q = send "q"
            let! x = send "x"
            return x
        }
    let py = send "y"
    {   Task = fun () ->
            plan {
                let! q = send "q"
                // when px and py are batched together, at first there is a step with both
                // q (from px) and y (from py) pending.
                // q will be pulled from the cache, but rather than just executing y, we should
                // defer y and advance px so we can batch x and y together.
                let! x, y = px, py
                let! z = send "z"
                return q + x + y + z
            }
        Batches =
            [   [ "q" ]
                [ "x"; "y" ] // x and y batched together
                [ "z" ]
            ]
        Result = Good "qxyz"
    } |> test