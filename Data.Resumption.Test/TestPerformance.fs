namespace Data.Resumption.Test
open Data.Resumption
open System
open System.Text
open System.Diagnostics
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestPerformance() =
    static let ret1 = datatask { return 1 }
    static let time f =
        let sw = new Stopwatch()
        sw.Start()
        let mutable iterations = 0L
        while sw.ElapsedMilliseconds < 1000L do
            testSpeed f
            iterations <- iterations + 1L
        sw.Stop()
        printfn "%s iterations in %O" (iterations.ToString("#,###")) sw.Elapsed
    [<TestMethod>]
    member __.TestSingleReturn() =
        time <|
            {
                Task = fun () -> ret1
                Batches = []
                Result = Good 1
            }

    [<TestMethod>]
    member __.TestNestedReturn() =
        time <|
            {
                Task = fun () -> datatask {
                    return! datatask {
                        return! datatask {
                            return! ret1
                        }
                    }
                }
                Batches = []
                Result = Good 1
            }

    [<TestMethod>]
    member __.TestBindChain() =
        time <|
            {
                Task = fun () -> datatask {
                    let! one1 = ret1
                    let! one2 = ret1
                    let! one3 = ret1
                    return one1 + one2 + one3
                }
                Batches = []
                Result = Good 3
            }

    [<TestMethod>]
    member __.TestBindChainWithRequests() =
        time <|
            {
                Task = fun () -> datatask {
                    let! _ = send "x"
                    let! one1 = ret1
                    let! _ = send "y"
                    let! one2 = ret1
                    let! _ = send "z"
                    let! one3 = ret1
                    return one1 + one2 + one3
                }
                Batches =
                    [
                        ["x"]
                        ["y"]
                        ["z"]
                    ]
                Result = Good 3
            }

    [<TestMethod>]
    member __.TestBindChainWithBatchedRequests() =
        time <|
            {
                Task = fun () -> datatask {
                    let! one1 = ret1
                    let! _ = send "x", send "y", send "z"
                    let! one2 = ret1
                    let! _ = send "q", send "r", send "s"
                    let! one3 = ret1
                    return one1 + one2 + one3
                }
                Batches =
                    [
                        ["x";"y";"z"]
                        ["q";"r";"s"]
                    ]
                Result = Good 3
            }