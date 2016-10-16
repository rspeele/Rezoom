namespace Rezoom.Test
open Rezoom
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestCaching() =
    [<TestMethod>]
    member __.TestStrictCachedPair() =
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
        
    [<TestMethod>]
    member __.TestConcurrentCachedPair() =
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

    [<TestMethod>]
    member __.TestChainingCachedConcurrency() =
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

    [<TestMethod>]
    member __.TestStillValid() =
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

    [<TestMethod>]
    member __.TestInvalidation() =
        {   Task = fun () ->
                plan {
                    let! q1 = send "q"
                    let! q2 = send "q"
                    let! m = mutate "x"
                    let! q3 = send "q"
                    return q1 + q2 + m + q3
                }
            Batches =
                [   [ "q" ]
                    [ "x" ]
                    [ "q" ]
                ]
            Result = Good "qqxq"
        } |> test