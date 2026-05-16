namespace Rezoom
open System
open System.Collections.Generic

type ServiceLifetime =
    | ExecutionLocal = 1
    | StepLocal = 2

type ExecutionState =
    | ExecutionFault
    | ExecutionSuccess

type IServiceConfig =
    abstract member TryGetConfig<'cfg> : unit -> 'cfg option

type ServiceConfig() =
    let configs = Dictionary<Type, obj>()
    let fallbacks = ResizeArray<IServiceProvider>()
    member this.SetConfiguration(cfg : 'cfg) =
        let ty = typeof<'cfg>
        configs.[ty] <- box cfg
        this
    /// Add a standard .NET <see cref="System.IServiceProvider"/> as a fallback
    /// resolver. Explicit <c>SetConfiguration</c> entries always win; anything not
    /// found there is queried from the registered providers, MOST RECENTLY ADDED
    /// FIRST. Adding several is fine and useful — e.g. a test override can be
    /// pushed on top of an app-wide registration without rebuilding the config.
    /// Lets ASP.NET Core consumers reuse their existing <c>services.AddSingleton</c>
    /// registrations without juggling a separate Rezoom-only config layer.
    member this.UseServiceProvider(provider : IServiceProvider) =
        fallbacks.Add(provider)
        this
    interface IServiceConfig with
        member __.TryGetConfig<'cfg>() =
            let ty = typeof<'cfg>
            let succ, config = configs.TryGetValue(ty)
            if succ then Some (Unchecked.unbox config : 'cfg)
            else
                let rec walk i =
                    if i < 0 then None
                    else
                        match fallbacks.[i].GetService(ty) with
                        | null -> walk (i - 1)
                        | v -> Some (Unchecked.unbox v : 'cfg)
                walk (fallbacks.Count - 1)

[<AbstractClass>]
type ServiceFactory<'a>() =
    abstract member CreateService : ServiceContext -> 'a
    abstract member DisposeService : ExecutionState * 'a -> unit
    abstract member ServiceLifetime : ServiceLifetime
and [<AbstractClass>] ServiceContext() =
    abstract member Configuration : IServiceConfig
    abstract member GetService<'f, 'a when 'f :> ServiceFactory<'a> and 'f : (new : unit -> 'f)> : unit -> 'a

type StepLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit ServiceFactory<'a>()
    override __.ServiceLifetime = ServiceLifetime.StepLocal
    override __.CreateService(_) = new 'a()
    override __.DisposeService(_, s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()

type ExecutionLocal<'a when 'a : (new : unit -> 'a)>() =
    inherit ServiceFactory<'a>()
    override __.ServiceLifetime = ServiceLifetime.ExecutionLocal
    override __.CreateService(_) = new 'a()
    override __.DisposeService(_, s) =
        match box s with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()