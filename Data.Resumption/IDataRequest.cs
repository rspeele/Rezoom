using System;
using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption
{
    /// <summary>
    /// Represents a request for data, which may execute in two stages:
    /// 1. when `Prepare()` is called, adds its queries or external calls to a batch.
    /// 2. when the function returned from `Prepare()` is evaluated, forces execution
    ///    of the batch and retrieves the result from the batch.
    /// </summary>
    /// <remarks>
    /// The `Prepare()` method is the main point of `IRequest`, but its other properties provide
    /// opportunities for automatic optimization and caching of request execution, so that some
    /// requests can be skipped when their result value is already known in context.
    /// </remarks>
    public interface IDataRequest
    {
        /// <summary>
        /// Identifies this request for caching, de-duplication, and logging purposes.
        /// </summary>
        /// <remarks>
        /// Ideally, identity objects should be serializable, but this is not required.
        /// </remarks>
        object Identity { get; }
        /// <summary>
        /// If non-null, identifies a data source that this request's caching should be localized to.
        /// </summary>
        /// <remarks>
        /// This allows mutations to only wipe out relevant cache information, instead of the entire cache.
        /// </remarks>
        object DataSource { get; }
        /// <summary>
        /// If non-null, this request should not be executed concurrently with others in the same `SequenceGroup`.
        /// </summary>
        /// <remarks>
        /// Useful for ensuring sequential access to e.g. database contexts with `SaveChangesAsync()`.
        /// </remarks>
        object SequenceGroup { get; }
        /// <summary>
        /// If true, executing this request more than once is indistinguishable from executing it once.
        /// </summary>
        /// <remarks>
        /// Idempotent requests can be cached and de-duplicated.
        /// </remarks>
        bool Idempotent { get; }
        /// <summary>
        /// If true, this request might mutate the underlying data source.
        /// </summary>
        /// <remarks>
        /// Mutations invalidate the cache for their data source.
        /// </remarks>
        bool Mutation { get; }
        /// <summary>
        /// Prepare to run this request.
        /// Calling the resulting function will force evaluation to get the result, but just calling
        /// `Prepare()` can have side effects like queuing up SQL to run.
        /// </summary>
        /// <returns></returns>
        Func<Task<object>> Prepare(IServiceContext context);
    }
    /// <summary>
    /// Marker interface for an IRequest whose Prepare method's task
    /// produces a <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IDataRequest<TResponse> : IDataRequest
    {
    }
}