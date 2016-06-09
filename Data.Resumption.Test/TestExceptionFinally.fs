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