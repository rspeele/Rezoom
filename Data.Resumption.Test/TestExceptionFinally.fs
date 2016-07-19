namespace Data.Resumption.Test
open Data.Resumption
open System
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestExceptionFinally() =
    [<TestMethod>]
    member __.TestFinallyNoThrow() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    try
                        let! q = send "q"
                        let! r = send "r"
                        return q + r
                    finally
                        ran <- ran + 1
                }
            Batches =
                [
                    [ "q" ]
                    [ "r" ]
                ]
            Result = Good "qr"
        } |> test
        if ran <> 1 then
            failwithf "ran was %d" ran

    [<TestMethod>]
    member __.TestSimpleFinally() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    try
                        explode "fail"
                        return 2
                    finally
                        ran <- ran + 1
                }
            Batches = []
            Result = Bad (fun _ -> ran = 1)
        } |> test

    [<TestMethod>]
    member __.TestBoundFinally() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = send "x"
                        explode "fail"
                        return 2
                    finally
                        ran <- ran + 1
                }
            Batches = [["x"]]
            Result = Bad (fun _ -> ran = 1)
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingPrepare() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = failingPrepare "fail" "x"
                        return 2
                    finally
                        ran <- ran + 1
                }
            Batches = []
            Result = Bad (fun _ -> ran = 1)
        } |> test

    [<TestMethod>]
    member __.TestSimpleFailingRetrieve() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    try
                        let! x = failingRetrieve "fail" "x"
                        return 2
                    finally
                        ran <- ran + 1
                }
            Batches = [["x"]]
            Result = Bad (fun _ -> ran = 1)
        } |> test

    [<TestMethod>]
    member __.TestUsingThrow() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    use d = { new IDisposable with member x.Dispose() = ran <- ran + 1 }
                    let! x = failingRetrieve "fail" "x"
                    return 2
                }
            Batches = [["x"]]
            Result = Bad (fun _ -> ran = 1)
        } |> test

    [<TestMethod>]
    member __.TestUsingNoThrow() =
        let mutable ran = 0
        {
            Task = fun () ->
                datatask {
                    use d = { new IDisposable with member x.Dispose() = ran <- ran + 1 }
                    let! x = send "x"
                    return 2
                }
            Batches = [["x"]]
            Result = Good 2
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

    [<TestMethod>]
    member __.TestConcurrentLoopAbortion() =
        let mutable ranFinally = false
        let deadly query =
            datatask {
                let! x = failingRetrieve "fail" query
                return ()
            }
        let good query =
            datatask {
                try
                    let! result = send query
                    let! next = send "jim"
                    return ()
                finally
                    ranFinally <- true
            }
        {
            Task = fun () ->
                datatask {
                    for q in batch ["x"; "y"; "z"] do
                        if q = "y" then
                            do! good q
                        else
                            do! deadly q
                    return ()
                }
            Batches =
                [
                    [ "x"; "y"; "z" ]
                ]
            Result = Bad (fun _ ->
                ranFinally)
        } |> test
        