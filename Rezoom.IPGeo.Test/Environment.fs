[<AutoOpen>]
module Rezoom.IPGeo.Test.Environment
open Rezoom
open Rezoom.Execution
open System
open System.Collections
open System.Collections.Generic
    
type 'a ExpectedResult =
    | Exception of (exn -> bool)
    | Value of 'a

type 'a TestTask =
    {   Task : 'a Plan
        Batches : string list list
        ExpectedResult : 'a ExpectedResult
    }

type TestExecutionLog() =
    inherit ExecutionLog()
    let batches = new ResizeArray<string ResizeArray>()
    member __.Batches =
        batches |> Seq.map List.ofSeq |> List.ofSeq
    override this.OnPreparedErrand(cacheInfo, arg) =
        batches.[batches.Count - 1].Add(string cacheInfo.Identity.Value.Identity)
    override this.OnBeginStep() = batches.Add(new ResizeArray<_>())

let test (task : 'a TestTask) =
    let log = new TestExecutionLog()
    let answer =
        try
            let task = executeWithLog log (ZeroServiceFactory()) task.Task
            Some task.Result
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
        