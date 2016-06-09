namespace Data.Resumption.Test
open Data.Resumption
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestConcurrency() =
    [<TestMethod>]
    member __.TestStrictPair() =
        {
            Task =
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
            Result = "qr"
        } |> test
        
    [<TestMethod>]
    member __.TestConcurrentPair() =
        {
            Task =
                datatask {
                    let! q, r = send "q", send "r"
                    return q + r
                }
            Batches =
                [
                    [ "q"; "r" ]
                ]
            Result = "qr"
        } |> test

