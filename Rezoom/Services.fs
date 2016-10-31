namespace Rezoom
open System
open System.Collections.Generic

type ServiceLifetime =
    | ExecutionLocal = 1
    | StepLocal = 2

[<AbstractClass>]
type ServiceFactory<'a>() =
    abstract member CreateService : ServiceContext -> 'a
    abstract member DisposeService : 'a -> unit
    abstract member ServiceLifetime : ServiceLifetime
and [<AbstractClass>] ServiceContext() =
    abstract member TryGetConfiguration<'cfg> : unit -> 'cfg option
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

[<AbstractClass>]
type ConfigurableServiceContext() =
    inherit ServiceContext()
    let configuration = Dictionary<Type, obj>()
    override __.TryGetConfiguration<'cfg>() =
        let ty = typeof<'cfg>
        let succ, config = configuration.TryGetValue(ty)
        if succ then Some (Unchecked.unbox config : 'cfg)
        else None
    member __.SetConfiguration(cfg : 'cfg) =
        let ty = typeof<'cfg>
        configuration.[ty] <- box cfg

type private InternalServiceContext() =
    inherit ConfigurableServiceContext()
    let services = Dictionary<Type, obj>()
    let locals = Stack()
    let globals = Stack()
    override this.GetService<'f, 'a when 'f :> ServiceFactory<'a> and 'f : (new : unit -> 'f)>() =
        let ty = typeof<'f>
        let succ, service = services.TryGetValue(ty)
        if succ then Unchecked.unbox service else
        let factory = new 'f()
        let service = factory.CreateService(this)
        let stack =
            match factory.ServiceLifetime with
            | ServiceLifetime.ExecutionLocal -> globals
            | ServiceLifetime.StepLocal -> locals
            | other -> failwithf "Unknown service lifetime: %O" other
        services.Add(ty, box service)
        stack.Push(fun () ->
            factory.DisposeService(service)
            ignore <| services.Remove(ty))
        service
    static member private ClearStack(stack : _ Stack) =
        let mutable exn = null
        while stack.Count > 0 do
            let disposer = stack.Pop()
            try
                disposer()
            with
            | e -> exn <- e
        if not (isNull exn) then raise exn
    member __.ClearLocals() = InternalServiceContext.ClearStack(locals)
    member this.Dispose() =
        try
            this.ClearLocals()
        finally
            InternalServiceContext.ClearStack(globals)
    interface IDisposable with
        member this.Dispose() = this.Dispose()