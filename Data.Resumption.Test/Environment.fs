[<AutoOpen>]
module Data.Resumption.Test.Environment
open Data.Resumption
open Data.Resumption.Services
open Data.Resumption.Execution
open System
open System.Collections
open System.Collections.Generic

type TestContext() =
    let batches = new List<List<string>>()
    let mutable inProgress = false
    member __.Prepare(query : string) =
        if not inProgress then
            batches.Add(new List<string>())
            inProgress <- true
        let batch = batches.[batches.Count - 1]
        let index = batch.Count
        batch.Add(query)
        ()
    member __.Execute() =
        inProgress <- false // end this batch
    member __.Batches() =
        batches
        |> Seq.map List.ofSeq
        |> List.ofSeq

type TestServiceFactory() =
    interface IServiceFactory with
        member __.CreateService<'a>() =
            if typeof<'a> = typeof<TestContext> then
                let service = new TestContext() :> obj
                let living = new LivingService<'a>(unbox service, ServiceLifetime.ExecutionContext)
                new Nullable<_>(living)
            else 
                new Nullable<_>()

type TestRequest<'a>(idem : bool, query : string, pre : unit -> unit, post : string -> 'a) =
    inherit SynchronousDataRequest<'a>()
    new (query, pre, post) =
        TestRequest<_>(true, query, pre, post)
    override __.Mutation = not idem
    override __.Idempotent = idem
    override __.DataSource = box typeof<TestContext>
    override __.Identity = box query
    override __.PrepareSynchronous(serviceContext) =
        let db = serviceContext.GetService<TestContext>()
        pre()
        db.Prepare(query)
        Func<_>(fun () ->
            db.Execute()
            post query)

exception PrepareFailure of string
exception RetrieveFailure of string
exception ArtificialFailure of string

let explode str = raise <| ArtificialFailure str

let sendWith query post = TestRequest<_>(query, id, post).ToDataTask()
let send query = sendWith query id
let mutateWith query post = TestRequest<_>(false, query, id, post).ToDataTask()
let mutate query = mutateWith query id
let failingPrepare msg query =
    TestRequest<_>
        ( query
        , fun () -> raise <| PrepareFailure msg
        , fun _ -> Unchecked.defaultof<_>
        ) |> fun r -> r.ToDataTask()
let failingRetrieve msg query = sendWith query (fun _ -> raise <| RetrieveFailure msg)

type ExpectedResult<'a> =
    | Good of 'a
    | Bad of (exn -> bool)

type ExpectedResultTest<'a> =
    {
        Task : unit -> IDataTask<'a>
        Batches : string list list
        Result : ExpectedResult<'a>
    }

let test expectedResult =
    let execContext = new ExecutionContext(new TestServiceFactory())
    let result =
        try
            execContext.Execute(expectedResult.Task()).Result |> Choice1Of2
        with
        | ex -> Choice2Of2 ex
    let testContext = execContext.GetService<TestContext>()
    let batches = testContext.Batches()

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
