using System;
using System.Threading;
using System.Threading.Tasks;
using MBrace.FsPickler;
using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;
// `Rezoom.Replay` is the compiled-class name for the F# module, so it can't
// be `using`-imported. Reference its nested types via the type name directly.
using IReplaySerializer = Rezoom.Replay.IReplaySerializer;
using RecordingExecutionStrategy = Rezoom.Replay.RecordingExecutionStrategy;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestReplay
{
    private static readonly IReplaySerializer Serializer = new FsPicklerSerializer();

    private sealed class FsPicklerSerializer : IReplaySerializer
    {
        private readonly BinarySerializer _binary = FsPickler.CreateBinarySerializer();
        public byte[] Serialize<T>(T it) => _binary.Pickle(it);
        public T Deserialize<T>(byte[] blob) => _binary.UnPickle<T>(blob);
    }

    private sealed class MismatchedReplay : Exception { public MismatchedReplay(string msg) : base(msg) { } }

    /// <summary>
    /// Runs the plan with recording, then replays it from the captured blob and
    /// asserts the two runs produced the same outcome.
    /// </summary>
    private static async Task RunReplayTest<T>(Func<Plan<T>> planFactory)
    {
        (ExecutionState State, byte[] Blob)? saved = null;
        Action<ExecutionState, Func<byte[]>> save = (state, arr) =>
            saved = (state, arr());

        var strategy = RecordingExecutionStrategy.Create(
            Execution.DefaultExecutionStrategy,
            Serializer,
            save);

        var config = Execution.ExecutionConfig.Default;

        (T Value, Exception Err) firstResult;
        try
        {
            var result = await strategy.Execute(config, planFactory(), CancellationToken.None);
            firstResult = (result, null);
        }
        catch (AggregateException ae) when (ae.InnerExceptions.Count == 1)
        {
            firstResult = (default, ae.InnerException!);
        }
        catch (Exception ex)
        {
            firstResult = (default, ex);
        }

        await Task.Delay(50);

        Assert.That(saved, Is.Not.Null, "didn't save");

        var (state, blob) = saved!.Value;
        if (state.IsExecutionSuccess && firstResult.Err != null)
            throw new Exception("State says success but first run threw");
        if (state.IsExecutionFault && firstResult.Err == null)
            throw new Exception("State says fault but first run succeeded");

        (T Value, Exception Err) secondResult;
        try
        {
            var result = await Rezoom.Replay.replay(config, Serializer, blob);
            secondResult = ((T)(object)result!, null);
        }
        catch (AggregateException ae) when (ae.InnerExceptions.Count == 1)
        {
            secondResult = (default, ae.InnerException!);
        }
        catch (Exception ex)
        {
            secondResult = (default, ex);
        }

        if (firstResult.Err == null && secondResult.Err == null)
        {
            if (!Equals(firstResult.Value, secondResult.Value))
                throw new MismatchedReplay($"{firstResult.Value} vs {secondResult.Value}");
        }
        else if (firstResult.Err != null && secondResult.Err != null)
        {
            if (firstResult.Err.Message != secondResult.Err.Message)
                throw new MismatchedReplay($"{firstResult.Err.Message} vs {secondResult.Err.Message}");
        }
        else
        {
            throw new MismatchedReplay($"first={firstResult.Err?.Message ?? firstResult.Value?.ToString()} vs second={secondResult.Err?.Message ?? secondResult.Value?.ToString()}");
        }
    }

    [Test]
    public async Task Simple_replay_works()
    {
        async Plan<string> P()
        {
            var x = await Send("x");
            var y = await Send("y");
            return x + y;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Simple_throwing_replay_works()
    {
        async Plan<string> P()
        {
            var x = await Send("x");
            var y = await Send("y");
            throw new Exception("hi");
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Throwing_in_prepare_works()
    {
        async Plan<string> P()
        {
            var x = await Send("x");
            var y = await FailingPrepare("bad prepare", "y");
            return x + y;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Throwing_in_retrieval_works()
    {
        async Plan<string> P()
        {
            var x = await Send("x");
            var y = await FailingRetrieve("bad retrieve", "y");
            return x + y;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Batches_work()
    {
        async Plan<string> P()
        {
            var (x, y) = await Plans.Tuple(Send("x"), Send("y"));
            var (z, q) = await Plans.Tuple(Send("z"), Send("q"));
            return x + y + z + q;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Batches_with_throwing_in_prepares_work()
    {
        async Plan<string> P()
        {
            var (x, y) = await Plans.Tuple(Send("x"), Send("y"));
            var (z, q) = await Plans.Tuple(FailingPrepare("bad prepare", "z"), Send("q"));
            return x + y + z + q;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Batches_with_throwing_in_retrieves_work()
    {
        async Plan<string> P()
        {
            var (x, y) = await Plans.Tuple(Send("x"), Send("y"));
            var (z, q) = await Plans.Tuple(FailingRetrieve("bad retrieve", "z"), Send("q"));
            return x + y + z + q;
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public async Task Plan_based_times_work()
    {
        async Plan<(DateTime, string, DateTimeOffset)> P()
        {
            var now = await Plans.UtcNow;
            var x = await Send("x");
            var nowOffset = await Plans.LocalNowOffset;
            return (now, x, nowOffset);
        }
        await RunReplayTest(() => P());
    }

    [Test]
    public void Regular_times_dont_work()
    {
        async Plan<(DateTime, string, DateTimeOffset)> P()
        {
            var now = DateTime.UtcNow;
            var x = await Send("x");
            var nowOffset = DateTimeOffset.Now;
            return (now, x, nowOffset);
        }
        Assert.ThrowsAsync<MismatchedReplay>(async () => await RunReplayTest(() => P()));
    }
}
