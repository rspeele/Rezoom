# Executing plans

Creating a `Plan` is no good unless you have a way of running it.

Here is the simplest way to run a plan.

```fsharp
open Rezoom
open System.Threading.Tasks

let run (plan : Plan<'a>) : Task<'a> =
    let config = Execution.ExecutionConfig.Default
    Execution.execute config plan

```

You get a `System.Threading.Task`, which
is the usual .NET asynchronous task representation. You can wait on the result
of that task synchronously with `task.Result`, or convert it to an F# `Async`
with `Async.AwaitTask`, or compose tasks together directly with a
[TaskBuilder](https://github.com/rspeele/TaskBuilder.fs), which is included with
Rezoom under the namespace `FSharp.Control.Tasks`.

## Replaying failed plans

The execution model of plans has a nice side effect, which is that if you record
the results of errands run, you can play back the plan without needing to access
the real data source.

This means you can have your app store the recorded executions of plans that
failed with exceptions. You can obtain the recordings from staging or production
and replay them with a debugger attached on your development machine, making
tracking down bugs much easier.

Here is how to run a plan and record its errand results.

This example depends on the excellent [FsPickler](https://github.com/mbraceproject/FsPickler)
serialization library, which doesn't come with Rezoom but is easy to install from NuGet.

```fsharp
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading
open System.Threading.Tasks
open Rezoom
open Rezoom.Execution
open Rezoom.Replay
open MBrace.FsPickler

let serializer =
    let binarySerializer = FsPickler.CreateBinarySerializer()
    { new IReplaySerializer with
        member __.Serialize(x) = binarySerializer.Pickle(x)
        member __.Deserialize(blob) = binarySerializer.UnPickle(blob)
    }

type PlanResult<'a> =
   | Good of result : 'a
   | Bad of exception : exn * recording : byte array

let config = ExecutionConfig.Default

let runWithErrorsRecorded (plan : Plan<'a>) : Task<PlanResult'a>> =
    task {
        let mutable recording = None
        let save executionState serializeRecording =
            match executionState with
            | ExecutionFault ->
                 recording <- Some (serializeRecording()) // save result
            | ExecutionSuccess -> () // don't save on success
        let config = ExecutionConfig.Default
        let strategy =
            RecordingExecutionStrategy.Create(defaultExecutionStrategy, serializer, save)
        try
            let! result = strategy.Execute(config, plan, CancellationToken.None)
            return Good result
        with
        | exn -> return Bad (exn, Option.get saved)
    }

let replayRecordedError (recording : byte array) : Task<obj> =
    replay config serializer recording

```

