namespace Rezoom
open System
open System.Threading
open System.Threading.Tasks
open Rezoom.Execution

/// DI-friendly entry point for executing Rezoom plans. Wraps a host's
/// <see cref="System.IServiceProvider"/> so that plan dependencies (e.g.
/// Rezoom.SQL's ConnectionProvider) can resolve through whatever DI container the host
/// uses, with no Rezoom-specific registration step required.
///
/// Typical ASP.NET Core usage:
/// <code>
///   services.AddScoped&lt;PlanExecutor&gt;();
///   // ...then constructor-inject PlanExecutor anywhere you need to run a plan.
/// </code>
type PlanExecutor(serviceProvider : IServiceProvider) =
    let config =
        lazy { ExecutionConfig.Default with Services = serviceProvider }
    /// Execute a plan, returning its result. Plans look up their dependencies
    /// from the wrapped <see cref="System.IServiceProvider"/>.
    member this.Execute(plan : 'a Plan) : 'a Task =
        execute config.Value plan
    /// Execute a plan with cancellation. The token reaches errands that opt into
    /// cooperative cancellation.
    member this.Execute(plan : 'a Plan, cancellationToken : CancellationToken) : 'a Task =
        defaultExecutionStrategy.Execute(config.Value, plan, cancellationToken)
