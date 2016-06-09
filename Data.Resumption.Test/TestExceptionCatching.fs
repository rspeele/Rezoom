namespace Data.Resumption.Test
open Data.Resumption
open System
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestExceptionCatching() =
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
            Result = Good 1
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
            Result = Good 1
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
            Result = Good 1
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
            Result = Good 1
        } |> test

    [<TestMethod>]
    member __.TestConcurrentCatching() =
        let catching query =
            datatask {
                let guid = Guid.NewGuid().ToString()
                try
                    let! x = failingRetrieve guid query
                    return x
                with
                | RetrieveFailure msg when msg = guid -> return "bad"

            }
        let good query =
            datatask {
                let! result = send query
                return result
            }
        {
            Task =
                datatask {
                    let! x, y, z =
                        catching "x", catching "y", good "z"
                    return x + y + z
                }
            Batches =
                [
                    [ "x"; "y"; "z" ]
                ]
            Result = Good "badbadz"
        } |> test

    [<TestMethod>]
    member __.TestConcurrentNonCatching() =
        let notCatching query =
            datatask {
                let! x = failingRetrieve "fail" query
                return x
            }
        let good query =
            datatask {
                let! result = send query
                let! next = send "jim"
                return result + next
            }
        {
            Task =
                datatask {
                    let! x, y, z =
                        notCatching "x", notCatching "y", good "z"
                    return x + y + z
                }
            Batches =
                [
                    [ "x"; "y"; "z" ]
                ]
            Result = Bad (fun ex -> true)
        } |> test