namespace Rezoom
open System
open System.Reflection
open System.Reflection.Emit

type ZeroServiceFactory() =
    inherit ServiceFactory()
    override __.CreateService(_) = null

[<AbstractClass>]
type SingleServiceFactory<'a>() =
    inherit ServiceFactory()
    abstract Create : ServiceContext -> 'a LivingService
    override this.CreateService<'svc>(cxt) =
        if obj.ReferenceEquals(typeof<'a>, typeof<'svc>) then
            this.Create(cxt) |> box |> Unchecked.unbox
            : 'svc LivingService
        else null

type CoalescingServiceFactory
    (main : ServiceFactory, fallback : ServiceFactory) =
    inherit ServiceFactory()
    override __.CreateService<'svc>(cxt) =
        let main = main.CreateService<'svc>(cxt)
        if isNull main then
            fallback.CreateService<'svc>(cxt)
        else main
