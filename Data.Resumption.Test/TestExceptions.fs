namespace Data.Resumption.Test
open Data.Resumption
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestExceptions() =
    [<TestMethod>]
    member __.TestSimpleCatch() =
        {
            Task =
                datatask {
                    try
                        explode "fail"
                        return 2
                    with
                    | ArtificialFailure "fail" -> return 1
                }
            Batches = []
            Result = 1
        } |> test
        
    [<TestMethod>]
    member __.TestNestedCatch() =
        {
            Task =
                datatask {
                    try
                        let! x = send "x"
                        explode "fail"
                        return 2
                    with
                    | ArtificialFailure "fail" -> return 1
                }
            Batches = [["x"]]
            Result = 1
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingPrepare() =
        {
            Task =
                datatask {
                    try
                        let! x = failingPrepare "fail" "x"
                        return 2
                    with
                    | PrepareFailure "fail" -> return 1
                }
            Batches = []
            Result = 1
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingRetrieve() =
        {
            Task =
                datatask {
                    try
                        let! x = failingRetrieve "fail" "x"
                        return 2
                    with
                    | RetrieveFailure "fail" -> return 1
                }
            Batches = [["x"]]
            Result = 1
        } |> test