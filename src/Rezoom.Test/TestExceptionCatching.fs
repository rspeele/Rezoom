module Rezoom.Test.TestExceptionCaching
open System
open Rezoom
open NUnit.Framework
open FsUnit

[<Test>]
let ``simple catch`` () =
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
        
[<Test>]
let ``bound catch`` () =
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

[<Test>]
let ``simple failing prepare`` () =
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

[<Test>]
let ``simple failing retrieve`` () =
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

[<Test>]
let ``concurrent catching`` () =
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

[<Test>]
let ``concurrent loop catching`` () =
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

[<Test>]
let ``concurrent non-catching`` () =
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