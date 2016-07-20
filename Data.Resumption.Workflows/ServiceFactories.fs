namespace Data.Resumption
open System
open System.Reflection
open System.Reflection.Emit

type ZeroServiceFactory() =
    inherit ServiceFactory()
    override __.CreateService(_) = null

type CoalescingServiceFactory
    (main : ServiceFactory, fallback : ServiceFactory) =
    inherit ServiceFactory()
    override __.CreateService<'svc>(cxt) =
        let main = main.CreateService<'svc>(cxt)
        if isNull main then
            fallback.CreateService<'svc>(cxt)
        else main
