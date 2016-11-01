namespace Rezoom
open System
open System.Collections.Generic

type ServiceLifetime =
    | ExecutionLocal = 1
    | StepLocal = 2

type IServiceConfig =
    abstract member TryGetConfig<'cfg> : unit -> 'cfg option

type ServiceConfig() =
    let configs = Dictionary<Type, obj>()
    member this.SetConfiguration(cfg : 'cfg) =
        let ty = typeof<'cfg>
        configs.[ty] <- box cfg
        this
    interface IServiceConfig with
        member __.TryGetConfig<'cfg>() =
            let ty = typeof<'cfg>
            let succ, config = configs.TryGetValue(ty)
            if succ then Some (Unchecked.unbox config : 'cfg)
            else None

[<AbstractClass>]
type ServiceFactory<'a>() =
    abstract member CreateService : ServiceContext -> 'a
    abstract member DisposeService : 'a -> unit
    abstract member ServiceLifetime : ServiceLifetime
and [<AbstractClass>] ServiceContext() =
    abstract member Configuration : IServiceConfig
    abstract member GetService<'f, 'a when 'f :> ServiceFactory<'a> and 'f : (new : unit -> 'f)> : unit -> 'a

type StepLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit ServiceFactory<'a>()
    override __.ServiceLifetime = ServiceLifetime.StepLocal
    override __.CreateService(_) = new 'a()
    override __.DisposeService(s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()

type ExecutionLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit ServiceFactory<'a>()
    override __.ServiceLifetime = ServiceLifetime.ExecutionLocal
    override __.CreateService(_) = new 'a()
    override __.DisposeService(s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()