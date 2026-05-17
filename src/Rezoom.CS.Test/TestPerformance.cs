using System;
using System.Diagnostics;
using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestPerformance
{
    private static Plan<int> Ret1() => Plan<int>.NewResult(1);

    private static void Time<T>(Func<Plan<T>> task, ExpectedResult<T> expectedResult)
    {
        var sw = new Stopwatch();
        sw.Start();
        const int iterations = 10 * 1000;
        for (int i = 0; i < iterations; i++)
        {
            TestRunner.RunSpeed(task, expectedResult);
        }
        sw.Stop();
        TestContext.WriteLine($"{(long)iterations * 1000L / Math.Max(1L, sw.ElapsedMilliseconds)} iterations per second");
    }

    [Test]
    public void Single_return()
    {
        Time(() => Ret1(), ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Nested_return()
    {
        async Plan<int> Inner3() => await Ret1();
        async Plan<int> Inner2() => await Inner3();
        async Plan<int> Inner1() => await Inner2();
        Time(() => Inner1(), ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Bind_chain()
    {
        async Plan<int> Task()
        {
            var a = await Ret1();
            var b = await Ret1();
            var c = await Ret1();
            return a + b + c;
        }
        Time(Task, ExpectedResult<int>.Ok(3));
    }

    [Test]
    public void Bind_chain_with_requests()
    {
        async Plan<int> Task()
        {
            _ = await Send("x");
            var a = await Ret1();
            _ = await Send("y");
            var b = await Ret1();
            _ = await Send("z");
            var c = await Ret1();
            return a + b + c;
        }
        Time(Task, ExpectedResult<int>.Ok(3));
    }

    [Test]
    public void Bind_chain_with_batched_requests()
    {
        async Plan<int> Task()
        {
            var a = await Ret1();
            _ = await Plans.Tuple(Send("x"), Send("y"), Send("z"));
            var b = await Ret1();
            _ = await Plans.Tuple(Send("q"), Send("r"), Send("s"));
            var c = await Ret1();
            return a + b + c;
        }
        Time(Task, ExpectedResult<int>.Ok(3));
    }
}
