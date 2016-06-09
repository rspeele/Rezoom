namespace Data.Resumption.Test
open Data.Resumption
open System
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestExceptionFinally() =
    [<TestMethod>]
    member __.TestSimpleFinally() =
        let mutable ran = false
        {
            Task = fun () ->
                datatask {
                    try
                        explode "fail"
                        return 2
                    finally
                        ran <- true
                }
            Batches = []
            Result = Bad (fun _ -> ran)
        } |> test

    [<TestMethod>]
    member __.TestBoundFinally() =
        let mutable ran = false
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = send "x"
                        explode "fail"
                        return 2
                    finally
                        ran <- true
                }
            Batches = [["x"]]
            Result = Bad (fun _ -> ran)
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingPrepare() =
        let mutable ran = false
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = failingPrepare "fail" "x"
                        return 2
                    finally
                        ran <- true
                }
            Batches = []
            Result = Bad (fun _ -> ran)
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingRetrieve() =
        let mutable ran = false
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = failingRetrieve "fail" "x"
                        return 2
                    finally
                        ran <- true
                }
            Batches = [["x"]]
            Result = Bad (fun _ -> ran)
        } |> test

    [<TestMethod>]
    member __.TestNestedFinally() =
        let mutable counter = 0
        let mutable first = 0
        let mutable next = 0
        {
            Task = fun () ->
                datatask {
                    try
                        try
                            let! x = failingRetrieve "fail" "x"
                            return 2
                        finally
                            counter <- counter + 1
                            first <- counter
                    finally
                        counter <- counter + 1
                        next <- counter
                }
            Batches = [["x"]]
            Result = Bad (fun _ ->
                counter = 2 && first = 1 && next = 2)
        } |> test

    [<TestMethod>]
    member __.TestConcurrentAbortion() =
        let mutable ranFinally = false
        let deadly query =
            datatask {
                let! x = failingRetrieve "fail" query
                return x
            }
        let good query =
            datatask {
                try
                    let! result = send query
                    let! next = send "jim"
                    return result + next
                finally
                    ranFinally <- true
            }
        {
            Task = fun () ->
                datatask {
                    let! x, y, z =
                        deadly "x", deadly "y", good "z"
                    return x + y + z
                }
            Batches =
                [
                    [ "x"; "y"; "z" ]
                ]
            Result = Bad (fun ex -> ranFinally)
        } |> test