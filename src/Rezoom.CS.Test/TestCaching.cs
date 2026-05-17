using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestCaching
{
    [Test]
    public void Strict_cached_pair()
    {
        async Plan<string> Task()
        {
            var q1 = await Send("q");
            var q2 = await Send("q");
            return q1 + q2;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "q" } },
            ExpectedResult<string>.Ok("qq"));
    }

    [Test]
    public void Concurrent_cached_pair()
    {
        async Plan<string> Task()
        {
            var (q1, q2) = await Plans.Tuple(Send("q"), Send("q"));
            return q1 + q2;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "q" } },
            ExpectedResult<string>.Ok("qq"));
    }

    [Test]
    public void Chaining_cached_concurrency()
    {
        async Plan<string> TestTask(string x)
        {
            var a = await Send(x + "1");
            var b = await Send(x + "2");
            var c = await Send(x + "3");
            return a + b + c;
        }
        async Plan<string> Task()
        {
            var (x1, x2, x3) = await Plans.Tuple(TestTask("x"), TestTask("x"), TestTask("x"));
            return x1 + " " + x2 + " " + x3;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "x1" },
                new[] { "x2" },
                new[] { "x3" },
            },
            ExpectedResult<string>.Ok("x1x2x3 x1x2x3 x1x2x3"));
    }

    [Test]
    public void Still_valid_after_other()
    {
        async Plan<string> Task()
        {
            var q1 = await Send("q");
            var q2 = await Send("q");
            var m = await Send("x");
            var q3 = await Send("q");
            return q1 + q2 + m + q3;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "q" },
                new[] { "x" },
            },
            ExpectedResult<string>.Ok("qqxq"));
    }

    [Test]
    public void Invalidation_invalidates()
    {
        async Plan<string> Task()
        {
            var q1 = await Send("q");
            var q2 = await Send("q");
            var m = await Mutate("x");
            var q3 = await Send("q");
            var q4 = await Send("q");
            return q1 + q2 + m + q3 + q4;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "q" },
                new[] { "x" },
                new[] { "q" },
            },
            ExpectedResult<string>.Ok("qqxqq"));
    }

    [Test]
    public void Deferred_execution()
    {
        async Plan<string> Px()
        {
            var q = await Send("q");
            var x = await Send("x");
            return x;
        }
        var py = Send("y");

        async Plan<string> Task()
        {
            var q = await Send("q");
            // When px and py are batched together, at first there is a step with
            // both q (from px) and y (from py) pending. q will be pulled from the
            // cache, but rather than just executing y, we defer y and advance px
            // so we can batch x and y together.
            var (x, y) = await Plans.Tuple(Px(), py);
            var z = await Send("z");
            return q + x + y + z;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "q" },
                new[] { "x", "y" },
                new[] { "z" },
            },
            ExpectedResult<string>.Ok("qxyz"));
    }
}
