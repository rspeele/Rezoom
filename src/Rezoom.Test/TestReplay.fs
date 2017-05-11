module Rezoom.Test.TestReplay
open FSharp.Control.Tasks.ContextInsensitive
open Rezoom
open Rezoom.Replay
open System
open System.Text
open System.Diagnostics
open NUnit.Framework
open FsUnit
open MBrace.FsPickler
open Rezoom.Execution
open FSharp.Control.Tasks.ContextSensitive
open System.Threading

let serializer =
    let binarySerializer = FsPickler.CreateBinarySerializer()
    { new IReplaySerializer with
        member __.Serialize(x) = binarySerializer.Pickle(x)
        member __.Deserialize(blob) = binarySerializer.UnPickle(blob)
    }

type Result<'a> =
    | Good of 'a
    | Bad of exn

exception MismatchedReplay

let runReplayTest plan =
    task {
        let mutable saved = None
        let save state arr =
            saved <- Some (state, arr())
        let strategy =
            RecordingExecutionStrategy.Create(defaultExecutionStrategy, serializer, save)
        let config = ExecutionConfig.Default
        let! firstResult =
            task {
                try
                    let! result = strategy.Execute(config, plan, CancellationToken.None)
                    return Good result
                with
                | exn -> return Bad exn
            }
        do! unitTask <| System.Threading.Tasks.Task.Delay(50)
        match saved with
        | None -> failwith "didn't save"
        | Some (state, blob) ->
            match state, firstResult with
            | ExecutionSuccess, Good _ -> ()
            | ExecutionFault, Bad _ -> ()
            | _ -> failwith "weird state"
            let! secondResult =
                task {
                    try
                        let! result = replay config serializer blob
                        return Good result
                    with
                    | exn -> return Bad exn
                }
            match firstResult, secondResult with
            | Good f, Good s when f = unbox s ->
                printfn "They both returned %O" f
            | Bad ef, Bad es when ef.Message = es.Message ->
                printfn "They both failed with %s" ef.Message
            | _ ->
                raise MismatchedReplay
    }

let testReplay plan = (runReplayTest plan).Wait()

[<Test>]
let ``simple replay works`` () =
    plan {
        let! x = send "x"
        let! y = send "y"
        return x + y
    } |> testReplay

[<Test>]
let ``simple throwing replay works`` () =
    plan {
        let! x = send "x"
        let! y = send "y"
        failwith "hi"
        return x + y
    } |> testReplay

[<Test>]
let ``throwing in prepare works`` () =
    plan {
        let! x = send "x"
        let! y = failingPrepare "bad prepare" "y"
        return x + y
    } |> testReplay

[<Test>]
let ``throwing in retrieval works`` () =
    plan {
        let! x = send "x"
        let! y = failingRetrieve "bad retrieve" "y"
        return x + y
    } |> testReplay

[<Test>]
let ``batches work`` () =
    plan {
        let! x, y = send "x", send "y"
        let! z, q = send "z", send "q"
        return x + y + z + q
    } |> testReplay

[<Test>]
let ``batches with throwing in prepares work`` () =
    plan {
        let! x, y = send "x", send "y"
        let! z, q = failingPrepare "bad prepare" "z", send "q"
        return x + y + z + q
    } |> testReplay

[<Test>]
let ``batches with throwing in retrieves work`` () =
    plan {
        let! x, y = send "x", send "y"
        let! z, q = failingRetrieve "bad retrieve" "z", send "q"
        return x + y + z + q
    } |> testReplay

[<Test>]
let ``plan-based times work`` () =
    plan {
        let! now = DateTime.UtcNowPlan
        let! x = send "x"
        let! nowOffset = DateTimeOffset.NowPlan
        return now, x, nowOffset
    } |> testReplay

[<Test>]
let ``regular times don't work`` () =
    try
        plan {
            let now = DateTime.UtcNow
            let! x = send "x"
            let nowOffset = DateTimeOffset.Now
            return now, x, nowOffset
        } |> testReplay
        failwith "should've failed"
    with
    | :? AggregateException as agg when agg.InnerExceptions.Count = 1 && agg.InnerException = MismatchedReplay -> ()