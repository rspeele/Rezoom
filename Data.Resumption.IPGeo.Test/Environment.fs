[<AutoOpen>]
module Data.Resumption.IPGeo.Test.Environment
open Data.Resumption
open Data.Resumption.DataRequests
open Data.Resumption.Services
open Data.Resumption.Services.Factories
open Data.Resumption.Execution
open System
open System.Collections
open System.Collections.Generic
    
type 'a ExpectedResult =
    | Exception of (exn -> bool)
    | Value of 'a

type 'a TestTask =
    {
        Task : 'a datatask
        Batches : string list list
        ExpectedResult : 'a ExpectedResult
    }

type TestExecutionLog() =
    let batches = new ResizeArray<string ResizeArray>()
    member __.Batches =
        batches |> Seq.map List.ofSeq |> List.ofSeq
    interface IExecutionLog with
        member this.OnComplete(request, response) = ()
        member this.OnPrepareFailure(exn) = ()
        member this.OnPrepare(request) =
            batches.[batches.Count - 1].Add(string request.Identity)
        member this.OnStepFinish() = ()
        member this.OnStepStart() = batches.Add(new ResizeArray<_>())

let test (task : 'a TestTask) =
    let log = new TestExecutionLog()
    let context =
        new ExecutionContext(new ZeroServiceFactory(), log)
    let answer =
        try
            context.Execute(task.Task).Result |> Some
        with
        | ex ->
            match task.ExpectedResult with
            | Exception predicate ->
                if predicate ex then None
                else reraise()
            | _ -> reraise()
    if log.Batches <> task.Batches then
        failwithf "Batches do not match (actual: %A)" log.Batches
    match answer with
    | None -> ()
    | Some v ->
        match task.ExpectedResult with
        | Exception predicate ->
            failwithf "Got value %A when exception was expected" v
        | Value expect ->
            if expect <> v then
                failwithf "Got %A; expected %A" v expect
            else ()
        