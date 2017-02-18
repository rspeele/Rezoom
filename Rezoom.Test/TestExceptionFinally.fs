module Rezoom.Test.TestExceptionFinally
open System
open Rezoom
open NUnit.Framework
open FsUnit

[<Test>]
let ``finally no throw`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                try
                    let! q = send "q"
                    let! r = send "r"
                    return q + r
                finally
                    ran <- ran + 1
            }
        Batches =
            [   [ "q" ]
                [ "r" ]
            ]
        Result = Good "qr"
    } |> test
    if ran <> 1 then
        failwithf "ran was %d" ran

[<Test>]
let ``simple finally`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                try
                    explode "fail"
                    return 2
                finally
                    ran <- ran + 1
            }
        Batches = []
        Result = Bad (fun _ -> ran = 1)
    } |> test

[<Test>]
let ``bound finally`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
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

[<Test>]
let ``finally failing prepare`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                try
                    let! x = failingPrepare "fail" "x"
                    return 2
                finally
                    ran <- ran + 1
            }
        Batches = []
        Result = Bad (fun _ -> ran = 1)
    } |> test

[<Test>]
let ``finally failing retrieve`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                try
                    let! x = failingRetrieve "fail" "x"
                    return 2
                finally
                    ran <- ran + 1
            }
        Batches = [["x"]]
        Result = Bad (fun _ -> ran = 1)
    } |> test

[<Test>]
let ``using with throw`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                use d = { new IDisposable with member x.Dispose() = ran <- ran + 1 }
                let! x = failingRetrieve "fail" "x"
                return 2
            }
        Batches = [["x"]]
        Result = Bad (fun _ -> ran = 1)
    } |> test

[<Test>]
let ``using without throw`` () =
    let mutable ran = 0
    {   Task = fun () ->
            plan {
                use d = { new IDisposable with member x.Dispose() = ran <- ran + 1 }
                let! x = send "x"
                return 2
            }
        Batches = [["x"]]
        Result = Good 2
    } |> test

[<Test>]
let ``nested finally`` () =
    let mutable counter = 0
    let mutable first = 0
    let mutable next = 0
    {   Task = fun () ->
            plan {
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

[<Test>]
let ``concurrent retrieval abortion good last`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    deadly "x", deadly "y", good "z"
                return x + y + z
            }
        Batches =
            [   [ "x"; "y"; "z" ]
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent retrieval abortion good first`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    good "x", deadly "y", deadly "z"
                return x + y + z
            }
        Batches =
            [   [ "x"; "y"; "z" ]
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent retrieval abortion good middle`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    deadly "x", good "y", deadly "z"
                return x + y + z
            }
        Batches =
            [   [ "x"; "y"; "z" ]
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent logic abortion good last`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            failwith "exn"
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    deadly "x", deadly "y", good "z"
                return x + y + z
            }
        Batches =
            [
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent logic abortion good first`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            failwith "exn"
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    good "x", deadly "y", deadly "z"
                return x + y + z
            }
        Batches =
            [
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent logic abortion good middle`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            failwith "exn"
            let! x = failingRetrieve "fail" query
            return x
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return result + next
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                let! x, y, z =
                    deadly "x", good "y", deadly "z"
                return x + y + z
            }
        Batches =
            [
            ]
        Result = Bad (fun ex -> ranFinally)
    } |> test

[<Test>]
let ``concurrent loop retrieval abortion`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            let! x = failingRetrieve "fail" query
            return ()
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return ()
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                for q in batch ["x"; "y"; "z"] do
                    if q = "y" then
                        do! good q
                    else
                        do! deadly q
                return ()
            }
        Batches =
            [   [ "x"; "y"; "z" ]
            ]
        Result = Bad (fun _ ->
            ranFinally)
    } |> test

[<Test>]
let ``concurrent loop logic abortion`` () =
    let mutable ranFinally = false
    let deadly query =
        plan {
            failwith "logic"
            let! x = failingRetrieve "fail" query
            return ()
        }
    let good query =
        plan {
            try
                let! result = send query
                let! next = send "jim"
                return ()
            finally
                ranFinally <- true
        }
    {   Task = fun () ->
            plan {
                for q in batch ["x"; "y"; "z"] do
                    if q = "y" then
                        do! good q
                    else
                        do! deadly q
                return ()
            }
        Batches =
            [
            ]
        Result = Bad (fun _ ->
            ranFinally)
    } |> test
        