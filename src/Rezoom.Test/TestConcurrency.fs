module Rezoom.Test.TestConcurrency
open Rezoom
open NUnit.Framework

[<Test>]
let ``strict pair`` () =
    {   Task = fun () ->
            plan {
                let! q = send "q"
                let! r = send "r"
                return q + r
            }
        Batches =
            [   [ "q" ]
                [ "r" ]
            ]
        Result = Good "qr"
    } |> test
        
[<Test>]
let ``concurrent pair`` () =
    {   Task = fun () ->
            plan {
                let! q, r = send "q", send "r"
                return q + r
            }
        Batches =
            [   [ "q"; "r" ]
            ]
        Result = Good "qr"
    } |> test

[<Test>]
let ``chaining concurrency`` () =
    let testTask x =
        plan {
            let! a = send (x + "1")
            let! b = send (x + "2")
            let! c = send (x + "3")
            return a + b + c
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    testTask "x", testTask "y", testTask "z"
                return x + " " + y + " " + z
            }
        Batches =
            [   [ "x1"; "y1"; "z1" ]
                [ "x2"; "y2"; "z2" ]
                [ "x3"; "y3"; "z3" ]
            ]
        Result = Good "x1x2x3 y1y2y3 z1z2z3"
    } |> test

[<Test>]
let ``list concurrency`` () =
    {   Task = fun () ->
            plan {
                let! xs = Plan.concurrentList [ send "x"; send "y"; send "z" ]
                return xs
            }
        Batches =
            [   [ "x"; "y"; "z" ]
            ]
        Result = Good [ "x"; "y"; "z" ]
    } |> test

[<Test>]
let ``chaining list concurrency`` () =
    {   Task = fun () ->
            plan {
                let! xs =
                    [   plan { let! _ = send "x" in return! send "y" }
                        plan { let! _ = send "y" in return! send "q" }
                        plan { let! z = send "z" in return! send (z + "r") }
                    ] |> Plan.concurrentList
                return xs
            }
        Batches =
            [   [ "x"; "y"; "z" ]
                [ "q"; "zr" ]
            ]
        Result = Good [ "y"; "q"; "zr" ]
    } |> test

[<Test>]
let ``chaining list concurrency with preceding cached`` () =
    {   Task = fun () ->
            plan {
                let! x = send "x"
                let! xs =
                    [   [   plan { let! _ = send "x" in return! send "q" }
                            plan { let! _ = send "y" in return! send "r" }
                        ] |> Plan.concurrentList
                        [   plan { let! z = send "z" in return! send "s" }
                        ] |> Plan.concurrentList
                    ] |> Plan.concurrentList
                return xs
            }
        Batches =
            [   [ "x" ]
                [ "q"; "y"; "z" ]
                [ "r"; "s" ]
            ]
        Result = Good [ ["q"; "r";]; ["s"] ]
    } |> test