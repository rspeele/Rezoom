namespace Data.Resumption
open System

type ZeroServiceFactory() =
    interface IServiceFactory with
        member this.CreateService() = null

type CoalescingServiceFactory
    (main : IServiceFactory, fallback : IServiceFactory) =
    member __.CreateService<'svc>() =
        let main = main.CreateService<'svc>()
        if isNull main then
            fallback.CreateService<'svc>()
        else main
    interface IServiceFactory with
        member this.CreateService<'svc>() =
            this.CreateService<'svc>()

type private DefaultServiceFactory() =
    interface IServiceFactory with
        member __.CreateService<'svc>() =
            let ty = typeof<'svc>
            if not ty.IsConstructedGenericType then null else
            let tyDef = ty.GetGenericTypeDefinition()
            let stepLocal = tyDef = typedefof<StepLocal<_>>
            let execLocal = tyDef = typedefof<ExecutionLocal<_>>
            if not stepLocal && not execLocal then null else
            let instance = Activator.CreateInstance(ty)
            new LivingService<'svc>
                ( if stepLocal
                    then ServiceLifetime.StepLocal
                    else ServiceLifetime.ExecutionLocal
                , Unchecked.unbox instance
                )
