namespace Rezoom.Test
open Rezoom
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestConcurrency() =
    [<TestMethod>]
    member __.TestStrictPair() =
        {
            Task = fun () ->
                datatask {
                    let! q = send "q"
                    let! r = send "r"
                    return q + r
                }
            Batches =
                [
                    [ "q" ]
                    [ "r" ]
                ]
            Result = Good "qr"
        } |> test
        
    [<TestMethod>]
    member __.TestConcurrentPair() =
        {
            Task = fun () ->
                datatask {
                    let! q, r = send "q", send "r"
                    return q + r
                }
            Batches =
                [
                    [ "q"; "r" ]
                ]
            Result = Good "qr"
        } |> test

    [<TestMethod>]
    member __.TestChainingConcurrency() =
        let testTask x =
            datatask {
                let! a = send (x + "1")
                let! b = send (x + "2")
                let! c = send (x + "3")
                return a + b + c
            }
        {
            Task = fun () ->
                datatask {
                    let! x, y, z =
                        testTask "x", testTask "y", testTask "z"
                    return x + " " + y + " " + z
                }
            Batches =
                [
                    [ "x1"; "y1"; "z1" ]
                    [ "x2"; "y2"; "z2" ]
                    [ "x3"; "y3"; "z3" ]
                ]
            Result = Good "x1x2x3 y1y2y3 z1z2z3"
        } |> test