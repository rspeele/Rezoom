namespace Rezoom.Test
open Rezoom
open System
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestExceptionCatching() =
    [<TestMethod>]
    member __.TestSimpleCatch() =
        {   Task = fun () ->
                plan {
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
    member __.TestBoundCatch() =
        {   Task = fun () ->
                plan {
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
        {   Task = fun () ->
                plan {
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
        {   Task = fun () ->
                plan {
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
            plan {
                let guid = Guid.NewGuid().ToString()
                try
                    let! x = failingRetrieve guid query
                    return x
                with
                | RetrieveFailure msg when msg = guid -> return "bad"

            }
        let good query =
            plan {
                let! result = send query
                return result
            }
        {   Task = fun () ->
                plan {
                    let! x, y, z =
                        catching "x", catching "y", good "z"
                    return x + y + z
                }
            Batches =
                [   [ "x"; "y"; "z" ]
                ]
            Result = Good "badbadz"
        } |> test

    [<TestMethod>]
    member __.TestConcurrentLoopCatching() =
        let catching query =
            plan {
                let guid = Guid.NewGuid().ToString()
                try
                    let! x = failingRetrieve guid query
                    return ()
                with
                | RetrieveFailure msg when msg = guid -> return ()

            }
        let good query =
            plan {
                let! result = send query
                return ()
            }
        {   Task = fun () ->
                plan {
                    for q in batch ["x"; "y"; "z"] do
                        if q = "y" then
                            do! good q
                        else
                            do! catching q
                    return ()
                }
            Batches =
                [   [ "x"; "y"; "z" ]
                ]
            Result = Good ()
        } |> test

    [<TestMethod>]
    member __.TestConcurrentNonCatching() =
        let notCatching query =
            plan {
                let! x = failingRetrieve "fail" query
                return x
            }
        let good query =
            plan {
                let! result = send query
                let! next = send "jim"
                return result + next
            }
        {   Task = fun () ->
                plan {
                    let! x, y, z =
                        notCatching "x", notCatching "y", good "z"
                    return x + y + z
                }
            Batches =
                [   [ "x"; "y"; "z" ]
                ]
            Result = Bad (fun ex -> true)
        } |> test