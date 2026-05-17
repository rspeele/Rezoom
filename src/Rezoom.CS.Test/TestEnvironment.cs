using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using Rezoom;

namespace Rezoom.CS.Test;

/// <summary>
/// Step-by-step execution log used by tests to assert which errands were
/// batched together in each step. Mirrors `TestExecutionLog` in Environment.fs.
/// </summary>
public class TestExecutionLog : Execution.ExecutionLog
{
    private readonly List<List<string>> _steps = new();
    public override void OnBeginStep() => _steps.Add(new List<string>());
    public override void OnPreparedErrand(Errand errand) =>
        _steps[^1].Add(errand.CacheInfo.Identity.ToString()!);
    /// Non-empty step batches in order.
    public List<List<string>> Batches() =>
        _steps.Where(s => s.Count > 0).ToList();
}

public sealed class PrepareFailure : Exception { public PrepareFailure(string msg) : base(msg) { } }
public sealed class RetrieveFailure : Exception { public RetrieveFailure(string msg) : base(msg) { } }
public sealed class ArtificialFailure : Exception { public ArtificialFailure(string msg) : base(msg) { } }

/// <summary>
/// Synchronous errand used by tests: identified by a query string,
/// invokes <c>pre</c> when prepared and runs <c>post</c> on the query for its result.
/// </summary>
public class TestRequest<T> : Rezoom.CS.SynchronousErrand<T>
{
    private readonly bool _idem;
    private readonly string _query;
    private readonly Action _pre;
    private readonly Func<string, T> _post;
    private readonly CacheInfo _cacheInfo;

    public TestRequest(bool idem, string query, Action pre, Func<string, T> post)
    {
        _idem = idem; _query = query; _pre = pre; _post = post;
        _cacheInfo = new TestCacheInfo(idem, query);
    }

    public TestRequest(string query, Action pre, Func<string, T> post) : this(true, query, pre, post) { }

    public override CacheInfo CacheInfo => _cacheInfo;

    public override Func<T> Prepare(PlanContext ctx)
    {
        _pre();
        return () => _post(_query);
    }

    private sealed class TestCacheInfo : CacheInfo
    {
        private readonly bool _idem;
        private readonly string _query;
        public TestCacheInfo(bool idem, string query) { _idem = idem; _query = query; }
        public override BitMask DependencyMask => new BitMask(0UL, 1UL);
        public override BitMask InvalidationMask => _idem ? BitMask.Zero : BitMask.Full;
        public override bool Cacheable => _idem;
        public override object Category => typeof(TestExecutionLog);
        public override object Identity => _query;
    }
}

/// <summary>
/// Helpers mirroring `send`, `mutate`, `failingPrepare`, etc. from F# Environment.fs.
/// </summary>
public static class Helpers
{
    public static Plan<string> SendWith(string query, Func<string, string> post) =>
        new TestRequest<string>(query, () => { }, post).ToPlan();

    public static Plan<string> Send(string query) => SendWith(query, x => x);

    public static Plan<string> MutateWith(string query, Func<string, string> post) =>
        new TestRequest<string>(false, query, () => { }, post).ToPlan();

    public static Plan<string> Mutate(string query) => MutateWith(query, x => x);

    public static Plan<string> FailingPrepare(string msg, string query) =>
        new TestRequest<string>(query, () => throw new PrepareFailure(msg), _ => default!).ToPlan();

    public static Plan<string> FailingRetrieve(string msg, string query) =>
        SendWith(query, _ => throw new RetrieveFailure(msg));

    public static void Explode(string msg) => throw new ArtificialFailure(msg);
}

public abstract record ExpectedResult<T>
{
    public sealed record Good(T Value) : ExpectedResult<T>;
    public sealed record Bad(Func<Exception, bool> Check) : ExpectedResult<T>;

    public static ExpectedResult<T> Ok(T value) => new Good(value);
    public static ExpectedResult<T> Fail(Func<Exception, bool> check) => new Bad(check);
    public static ExpectedResult<T> Fail() => new Bad(_ => true);
}

public static class TestRunner
{
    public static void Run<T>(
        Func<Plan<T>> task,
        IReadOnlyList<IReadOnlyList<string>> expectedBatches,
        ExpectedResult<T> expectedResult)
    {
        var log = new TestExecutionLog();
        var instanceFactory = FuncConvert.FromFunc<Execution.ExecutionInstance>(
            () => new Execution.ExecutionInstance(log));
        var defaultConfig = Execution.ExecutionConfig.Default;
        var config = new Execution.ExecutionConfig(defaultConfig.Services, instanceFactory);

        T value = default!;
        Exception thrown = null!;
        try
        {
            value = Execution.execute(config, task()).Result;
        }
        catch (AggregateException ae) when (ae.InnerExceptions.Count == 1)
        {
            thrown = ae.InnerException!;
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        var batches = log.Batches();

        if (!BatchesEqual(batches, expectedBatches))
        {
            Assert.Fail($"Batches do not match.\n  Actual:   {Format(batches)}\n  Expected: {Format(expectedBatches)}");
        }

        switch (expectedResult)
        {
            case ExpectedResult<T>.Good g:
                if (thrown != null)
                    Assert.Fail($"Expected {g.Value} but got exception: {thrown}");
                if (!EqualityComparer<T>.Default.Equals(value, g.Value))
                    Assert.Fail($"Expected {g.Value} but got {value}");
                break;
            case ExpectedResult<T>.Bad b:
                if (thrown == null)
                    Assert.Fail($"Expected failure but got result {value}");
                if (!b.Check(thrown))
                    Assert.Fail($"Exception did not match expectations: {thrown}");
                break;
        }
    }

    /// <summary>
    /// Run a "speed" test (no batch/result assertions, just verifies it doesn't throw and result matches).
    /// </summary>
    public static void RunSpeed<T>(Func<Plan<T>> task, ExpectedResult<T> expectedResult)
    {
        var log = new TestExecutionLog();
        var instanceFactory = FuncConvert.FromFunc<Execution.ExecutionInstance>(
            () => new Execution.ExecutionInstance(log));
        var defaultConfig = Execution.ExecutionConfig.Default;
        var config = new Execution.ExecutionConfig(defaultConfig.Services, instanceFactory);
        var result = Execution.execute(config, task()).Result;
        if (expectedResult is ExpectedResult<T>.Good g)
        {
            if (!EqualityComparer<T>.Default.Equals(result, g.Value))
                throw new Exception($"Invalid result for speed test: {result} vs {g.Value}");
        }
    }

    private static bool BatchesEqual(
        IReadOnlyList<IReadOnlyList<string>> a,
        IReadOnlyList<IReadOnlyList<string>> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!a[i].SequenceEqual(b[i])) return false;
        }
        return true;
    }

    private static string Format(IEnumerable<IEnumerable<string>> lists) =>
        "[" + string.Join("; ", lists.Select(l => "[" + string.Join("; ", l.Select(s => $"\"{s}\"")) + "]")) + "]";
}
