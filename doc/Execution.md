# Executing plans

Creating a `Plan` is no good unless you have a way of running it.

Here is the simplest way to run a plan.

```fsharp
open Rezoom
open System
open System.Threading.Tasks

let run (services : IServiceProvider) (plan : Plan<'a>) : Task<'a> =
    PlanExecutor(services).Execute(plan)

```

`services` is the host's `IServiceProvider`. In an ASP.NET Core app, register
`PlanExecutor` and inject it directly:

```csharp
builder.Services.AddScoped<PlanExecutor>();
```

`PlanExecutor` wraps the underlying `Execution.execute` / `ExecutionConfig`
machinery; reach for those directly only when you need to plug in a custom
`ExecutionInstance` or `ExecutionStrategy`.

You get a `System.Threading.Task`, which
is the usual .NET asynchronous task representation. You can wait on the result
of that task synchronously with `task.Result`, convert it to an F# `Async` with
`Async.AwaitTask`, or compose tasks together directly with F#'s built-in
`task { }` computation expression (FSharp.Core 5.0+).

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

let runWithErrorsRecorded (services : System.IServiceProvider) (plan : Plan<'a>) : Task<PlanResult<'a>> =
    task {
        let mutable recording = None
        let save executionState serializeRecording =
            match executionState with
            | ExecutionFault ->
                 recording <- Some (serializeRecording()) // save result
            | ExecutionSuccess -> () // don't save on success
        let config = { ExecutionConfig.Default with Services = services }
        let strategy =
            RecordingExecutionStrategy.Create(defaultExecutionStrategy, serializer, save)
        try
            let! result = strategy.Execute(config, plan, CancellationToken.None)
            return Good result
        with
        | exn -> return Bad (exn, Option.get saved)
    }

let replayRecordedError (services : System.IServiceProvider) (recording : byte array) : Task<obj> =
    let config = { ExecutionConfig.Default with Services = services }
    replay config serializer recording

```

