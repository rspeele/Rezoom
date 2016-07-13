namespace Data.Resumption
open System
open System.Collections.Generic
    

[<AllowNullLiteral>]
type private ServiceContextCache() =
    let services = new Dictionary<Type, obj>()
    let disposalStack = new Stack<IDisposable>()

    member __.TryGetService(ty, svc : obj byref) =
        services.TryGetValue(ty, &svc)
    member __.CacheService(ty, svc : obj) =
        services.[ty] <- svc
        match svc with
        | :? IDisposable as disp ->
            disposalStack.Push(disp)
        | _ -> ()
    member __.Dispose() =
        let exns = new ResizeArray<exn>()
        while disposalStack.Count > 0 do
            try
                disposalStack.Pop().Dispose()
            with
            | exn -> exns.Add(exn)
        if exns.Count > 0 then
            raise (aggregate exns)
    interface IDisposable with
        member this.Dispose() = this.Dispose()


type ServiceContext(factory : ServiceFactory) =
    let factory = new DefaultServiceFactory(factory)
    let execution = new ServiceContextCache()
    let sync = new obj()
    let mutable step : ServiceContextCache = null

    member __.BeginStep() =
        lock sync <| fun () ->
            if not (isNull step) then step.Dispose()
            step <- new ServiceContextCache()
    member __.EndStep() =
        lock sync <| fun () ->
            if not (isNull step) then step.Dispose()
            step <- null

    member public this.Dispose() =
        lock sync <| fun () ->
            this.EndStep()
            execution.Dispose()

    member public __.GetService<'svc>() : 'svc =
        lock sync <| fun () ->
            let ty = typeof<'svc>
            let mutable cached : obj = null
            if (not (isNull step) && step.TryGetService(ty, &cached)
                || execution.TryGetService(ty, &cached)) then Unchecked.unbox cached else
            let living = factory.CreateService<'svc>()
            if isNull living then
                notSupported (sprintf "The service type %O is not supported by the service factory" ty)
            else
            match living.Lifetime with
            | ServiceLifetime.ExecutionLocal ->
                execution.CacheService(ty, living.Service)
            | ServiceLifetime.StepLocal ->
                if isNull step then logicFault "Can't get step-local service outside of a step"
                step.CacheService(ty, living.Service)
            | _ -> invalidArg "lifetime" (sprintf "Unknown lifetime %d" (int living.Lifetime))
            living.Service

    interface IDisposable with
        member this.Dispose() = this.Dispose()
    