namespace Data.Resumption

type ServiceLifetime =
    | ExecutionLocal = 1
    | StepLocal = 2

[<AllowNullLiteral>]
type LivingService<'svc> =
    class
        val public Lifetime : ServiceLifetime
        val public Service : 'svc
        new (life, svc) = { Lifetime = life; Service = svc }
    end

[<AbstractClass>]
type ServiceContext() =
    abstract member GetService<'svc> : unit -> 'svc

[<AbstractClass>]
type ServiceFactory() =
    abstract member CreateService : unit -> LivingService<'svc>

type StepLocal<'a when 'a : (new : unit -> 'a)>() =
    let a = new 'a()
    member __.Service = a

type ExecutionLocal<'a when 'a : (new : unit -> 'a)>() =
    let a = new 'a()
    member __.Service = a
