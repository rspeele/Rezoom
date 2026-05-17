using System;
using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestExceptionCatching
{
    [Test]
    public void Simple_catch()
    {
        async Plan<int> Task()
        {
            try
            {
                Explode("fail");
                return 2;
            }
            catch (ArtificialFailure e) when (e.Message == "fail")
            {
                return 1;
            }
        }
        TestRunner.Run(Task, System.Array.Empty<string[]>(), ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Bound_catch()
    {
        async Plan<int> Task()
        {
            try
            {
                var x = await Send("x");
                Explode("fail");
                return 2;
            }
            catch (ArtificialFailure e) when (e.Message == "fail")
            {
                return 1;
            }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Simple_failing_prepare()
    {
        async Plan<int> Task()
        {
            try
            {
                var x = await FailingPrepare("fail", "x");
                return 2;
            }
            catch (PrepareFailure e) when (e.Message == "fail")
            {
                return 1;
            }
        }
        TestRunner.Run(Task, System.Array.Empty<string[]>(), ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Simple_failing_retrieve()
    {
        async Plan<int> Task()
        {
            try
            {
                var x = await FailingRetrieve("fail", "x");
                return 2;
            }
            catch (RetrieveFailure e) when (e.Message == "fail")
            {
                return 1;
            }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Ok(1));
    }

    [Test]
    public void Concurrent_catching()
    {
        async Plan<string> Catching(string query)
        {
            var guid = Guid.NewGuid().ToString();
            try
            {
                return await FailingRetrieve(guid, query);
            }
            catch (RetrieveFailure e) when (e.Message == guid)
            {
                return "bad";
            }
        }
        async Plan<string> Good(string query) => await Send(query);

        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Catching("x"), Catching("y"), Good("z"));
            return x + y + z;
        }

        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<string>.Ok("badbadz"));
    }

    [Test]
    public void Concurrent_loop_catching()
    {
        async Plan<int> Catching(string query)
        {
            var guid = Guid.NewGuid().ToString();
            try
            {
                _ = await FailingRetrieve(guid, query);
                return 0;
            }
            catch (RetrieveFailure e) when (e.Message == guid)
            {
                return 0;
            }
        }
        async Plan<int> Good(string query) { _ = await Send(query); return 0; }

        async Plan<int> Task()
        {
            await Plans.ForBatch(new[] { "x", "y", "z" }, async q =>
            {
                if (q == "y") return await Good(q);
                return await Catching(q);
            });
            return 0;
        }

        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<int>.Ok(0));
    }

    [Test]
    public void Concurrent_non_catching()
    {
        async Plan<string> NotCatching(string query) => await FailingRetrieve("fail", query);
        async Plan<string> Good(string query)
        {
            var result = await Send(query);
            var next = await Send("jim");
            return result + next;
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(NotCatching("x"), NotCatching("y"), Good("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<string>.Fail());
    }
}
