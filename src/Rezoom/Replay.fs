module Rezoom.Replay
open FSharp.Control.Tasks.ContextInsensitive
open Rezoom
open Rezoom.Execution
open System.Collections.Generic
open System

type IReplaySerializer =
    abstract member Serialize : it : 'a -> byte array
    abstract member Deserialize : blob : byte array -> 'a

type private HistoryEntryId =
    {   Category : obj
        Identity : obj
        Argument : obj
    }
    static member OfErrand(errand : Errand) =
        {   Category = errand.CacheInfo.Category
            Identity = errand.CacheInfo.Identity
            Argument = errand.CacheArgument
        }

type private HistoryResponse =
    | ResponseSucceeded of obj
    | ResponseFailed of exn

type private HistoryEntry =
    {   Id : HistoryEntryId
        Response : HistoryResponse
    }

type private History =
    {   Plan : obj Plan
        Entries : HistoryEntry array
    }

type private RecordingInstance(baseInstance : ExecutionInstance) =
    inherit ExecutionInstance(baseInstance.Log)
    let entries = ResizeArray()
    override __.RunErrand(errand, context) =
        let id = HistoryEntryId.OfErrand(errand)
        try
            let baseRan = baseInstance.RunErrand(errand, context)
            fun token ->
                task {
                    try
                        let! response = baseRan token
                        {   Id = id
                            Response = ResponseSucceeded response
                        } |> entries.Add
                        return response
                    with
                    | exn ->
                        {   Id = id
                            Response = ResponseFailed exn
                        } |> entries.Add
                        return raise exn // sadly can't reraise() here
                }
        with
        | exn ->
            {   Id = id
                Response = ResponseFailed exn
            } |> entries.Add
            reraise()
    override __.CreateCache() = baseInstance.CreateCache()
    member __.HistoryEntries() =
        let arr = Array.zeroCreate entries.Count
        entries.CopyTo(arr)
        arr

type RecordingExecutionStrategy
    private
        ( execution : IExecutionStrategy
        , serializer : IReplaySerializer
        , saveBlob : ExecutionState -> (unit -> byte array) -> unit
        ) =
    interface IExecutionStrategy with
        member __.Execute(config, plan, token) =
            task {
                let instance = RecordingInstance(config.Instance())
                let config =
                    { config with
                        Instance = fun () -> upcast instance
                    }
                let mutable succeeded = false
                try
                    let! it = execution.Execute(config, plan, token)
                    succeeded <- true
                    return it
                finally
                    let state = if succeeded then ExecutionSuccess else ExecutionFault
                    saveBlob state <| fun () ->
                        {   Entries = instance.HistoryEntries()
                            Plan = Plan.map box plan
                        } |> serializer.Serialize
            }
    static member Create(execution, serializer, saveBlob) =
        RecordingExecutionStrategy(execution, serializer, saveBlob) :> IExecutionStrategy
    static member Create(execution, serializer, saveBlob : Action<ExecutionState, Func<byte array>>) =
        RecordingExecutionStrategy.Create(execution, serializer, fun state arr ->
            saveBlob.Invoke(state, fun () -> arr.Invoke()))

exception UnreplayableException of string

let private unreplayable() =
    raise <|
        UnreplayableException
            ( "Errand ID does not match."
            + " The code may have changed or the plan you are trying to replay may not be purely functional."
            )
    

type private ReplayingInstance(baseInstance : ExecutionInstance, history : HistoryEntry array) =
    inherit ExecutionInstance(baseInstance.Log)
    let mutable i = 0
    override __.RunErrand(errand, _) =
        if i >= history.Length then
            unreplayable()
        let entry = history.[i]
        let id = HistoryEntryId.OfErrand(errand)
        if id <> entry.Id then
            unreplayable()
        i <- i + 1
        fun _ ->
            task {
                match entry.Response with
                | ResponseFailed exn -> return raise exn
                | ResponseSucceeded o -> return o
            }
    override __.CreateCache() = baseInstance.CreateCache()

let replay (config : ExecutionConfig) (serializer : IReplaySerializer) (blob : byte array) =
    let history = serializer.Deserialize(blob)
    let plan = history.Plan
    let config =
        { config with
            Instance = fun () -> upcast ReplayingInstance(config.Instance(), history.Entries)
        }
    execute config plan




