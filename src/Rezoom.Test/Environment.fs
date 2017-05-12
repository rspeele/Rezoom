[<AutoOpen>]
module Rezoom.Test.Environment
open Rezoom
open Rezoom.Execution
open System
open System.Collections
open System.Collections.Generic

type TestExecutionLog() =
    inherit ExecutionLog()
    let steps = ResizeArray()
    override __.OnBeginStep() =
        steps.Add(ResizeArray())
    override __.OnPreparedErrand(errand) =
        steps.[steps.Count - 1].Add(string errand.CacheInfo.Identity)
    member __.Batches() =
        steps |> Seq.map List.ofSeq |> Seq.filter (not << List.isEmpty) |> List.ofSeq

type TestRequest<'a>(idem : bool, query : string, pre : unit -> unit, post : string -> 'a) =
    inherit SynchronousErrand<'a>()
    new (query, pre, post) =
        TestRequest<_>(true, query, pre, post)
    override __.CacheInfo =
        { new CacheInfo() with
            override __.DependencyMask = BitMask(0UL, 1UL)
            override __.InvalidationMask =
                if idem then BitMask.Zero
                else BitMask.Full
            override __.Cacheable = idem
            override __.Category = typeof<TestExecutionLog>
            override __.Identity = upcast query
        }
    override __.Prepare(serviceContext : ServiceContext) =
        pre()
        fun () ->
            post query

exception PrepareFailure of string
exception RetrieveFailure of string
exception ArtificialFailure of string

let explode str = raise <| ArtificialFailure str

let sendWith query post = Plan.ofErrand <| TestRequest<_>(query, id, post)
let send query = sendWith query id
let mutateWith query post = Plan.ofErrand <| TestRequest<_>(false, query, id, post)
let mutate query = mutateWith query id
let failingPrepare msg query =
    TestRequest<_>
        ( query
        , fun () -> raise <| PrepareFailure msg
        , fun _ -> Unchecked.defaultof<_>
        ) |> Plan.ofErrand
let failingRetrieve msg query = sendWith query (fun _ -> raise <| RetrieveFailure msg)

type ExpectedResult<'a> =
    | Good of 'a
    | Bad of (exn -> bool)

type ExpectedResultTest<'a> =
    {   Task : unit -> 'a Plan
        Batches : string list list
        Result : ExpectedResult<'a>
    }

let testSpeed expectedResult =
    let log = TestExecutionLog()
    let config = { ExecutionConfig.Default with Instance = fun () -> ExecutionInstance(log) }
    let result = (execute config (expectedResult.Task())).Result
    match expectedResult.Result with
    | Good x when x = result -> ()
    | _ -> failwith "Invalid result for speed test (try running this as a regular test)"

let test expectedResult =
    let log = TestExecutionLog()
    let execContext = execute { ExecutionConfig.Default with Instance = fun () -> ExecutionInstance(log) }
    let result =
        try
            (execContext (expectedResult.Task())).Result |> Choice1Of2
        with
        | ex -> Choice2Of2 ex
    let batches = log.Batches()

    if batches <> expectedResult.Batches then
        failwithf "Batches do not match (actual: %A)" batches
    match expectedResult.Result, result with
    | Good expected, Choice1Of2 actual ->
        if expected <> actual then
            failwithf "Results do not match (actual: %A) (expected: %A)" actual expected
    | Good expected, Choice2Of2 ex ->
        failwithf "Expected result %A but got exception %A" expected ex
    | Bad _, Choice1Of2 actual ->
        failwithf "Expected failure but got result %A" actual
    | Bad check, Choice2Of2 ex ->
        if not (check ex) then
            failwithf "Exception %A did not match expectations" ex
