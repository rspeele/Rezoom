namespace Data.Resumption.Test
open Data.Resumption
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestCaching() =
    [<TestMethod>]
    member __.TestStrictCachedPair() =
        {
            Task = fun () ->
                datatask {
                    let! q1 = send "q"
                    let! q2 = send "q"
                    return q1 + q2
                }
            Batches =
                [
                    [ "q" ]
                ]
            Result = Good "qq"
        } |> test
        
    [<TestMethod>]
    member __.TestConcurrentCachedPair() =
        {
            Task = fun () ->
                datatask {
                    let! q1, q2 = send "q", send "q"
                    return q1 + q2
                }
            Batches =
                [
                    [ "q" ]
                ]
            Result = Good "qq"
        } |> test

    [<TestMethod>]
    member __.TestChainingCachedConcurrency() =
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
                    let! x1, x2, x3 =
                        testTask "x", testTask "x", testTask "x"
                    return x1 + " " + x2 + " " + x3
                }
            Batches =
                [
                    [ "x1" ]
                    [ "x2" ]
                    [ "x3" ]
                ]
            Result = Good "x1x2x3 x1x2x3 x1x2x3"
        } |> test