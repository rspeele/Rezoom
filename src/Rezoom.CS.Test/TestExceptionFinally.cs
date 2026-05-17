using System;
using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestExceptionFinally
{
    private sealed class DisposeRecorder : IDisposable
    {
        private readonly Action _onDispose;
        public DisposeRecorder(Action onDispose) { _onDispose = onDispose; }
        public void Dispose() => _onDispose();
    }

    [Test]
    public void Finally_no_throw()
    {
        int ran = 0;
        async Plan<string> Task()
        {
            try
            {
                var q = await Send("q");
                var r = await Send("r");
                return q + r;
            }
            finally { ran++; }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "q" }, new[] { "r" } },
            ExpectedResult<string>.Ok("qr"));
        Assert.That(ran, Is.EqualTo(1));
    }

    [Test]
    public void Simple_finally()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            try
            {
                Explode("fail");
                return 2;
            }
            finally { ran++; }
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<int>.Fail(_ => ran == 1));
    }

    [Test]
    public void Bound_finally()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            try
            {
                var x = await Send("x");
                Explode("fail");
                return 2;
            }
            finally { ran++; }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Fail(_ => ran == 1));
    }

    [Test]
    public void Finally_failing_prepare()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            try
            {
                var x = await FailingPrepare("fail", "x");
                return 2;
            }
            finally { ran++; }
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<int>.Fail(_ => ran == 1));
    }

    [Test]
    public void Finally_failing_retrieve()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            try
            {
                var x = await FailingRetrieve("fail", "x");
                return 2;
            }
            finally { ran++; }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Fail(_ => ran == 1));
    }

    [Test]
    public void Using_with_throw()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            using var d = new DisposeRecorder(() => ran++);
            var x = await FailingRetrieve("fail", "x");
            return 2;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Fail(_ => ran == 1));
    }

    [Test]
    public void Using_without_throw()
    {
        int ran = 0;
        async Plan<int> Task()
        {
            using var d = new DisposeRecorder(() => ran++);
            var x = await Send("x");
            return 2;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Ok(2));
        Assert.That(ran, Is.EqualTo(1));
    }

    [Test]
    public void Nested_finally()
    {
        int counter = 0, first = 0, next = 0;
        async Plan<int> Task()
        {
            try
            {
                try
                {
                    var x = await FailingRetrieve("fail", "x");
                    return 2;
                }
                finally { counter++; first = counter; }
            }
            finally { counter++; next = counter; }
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x" } },
            ExpectedResult<int>.Fail(_ => counter == 2 && first == 1 && next == 2));
    }

    [Test]
    public void Concurrent_retrieval_abortion_good_last()
    {
        bool ranFinally = false;
        async Plan<string> Deadly(string query) => await FailingRetrieve("fail", query);
        async Plan<string> Good(string query)
        {
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Deadly("x"), Deadly("y"), Good("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<string>.Fail(_ => ranFinally));
    }

    [Test]
    public void Concurrent_retrieval_abortion_good_first()
    {
        bool started = false;
        bool ranFinally = false;
        async Plan<string> Deadly(string query) => await FailingRetrieve("fail", query);
        async Plan<string> Good(string query)
        {
            started = true;
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Good("x"), Deadly("y"), Deadly("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<string>.Fail(_ => started && ranFinally));
    }

    [Test]
    public void Concurrent_retrieval_abortion_good_middle()
    {
        bool started = false;
        bool ranFinally = false;
        async Plan<string> Deadly(string query) => await FailingRetrieve("fail", query);
        async Plan<string> Good(string query)
        {
            started = true;
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Deadly("x"), Good("y"), Deadly("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<string>.Fail(_ => started && ranFinally));
    }

    [Test]
    public void Concurrent_logic_abortion_good_last()
    {
        bool started = false;
        bool ranFinally = false;
        async Plan<string> Deadly(string query)
        {
            throw new Exception("exn");
#pragma warning disable CS0162 // Unreachable code
            var x = await FailingRetrieve("fail", query);
            return x;
#pragma warning restore CS0162
        }
        async Plan<string> Good(string query)
        {
            started = true;
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Deadly("x"), Deadly("y"), Good("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<string>.Fail(_ => !started && !ranFinally));
    }

    [Test]
    public void Concurrent_logic_abortion_good_first()
    {
        bool ranFinally = false;
        async Plan<string> Deadly(string query)
        {
            throw new Exception("exn");
#pragma warning disable CS0162
            var x = await FailingRetrieve("fail", query);
            return x;
#pragma warning restore CS0162
        }
        async Plan<string> Good(string query)
        {
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Good("x"), Deadly("y"), Deadly("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<string>.Fail(_ => ranFinally));
    }

    [Test]
    public void Concurrent_logic_abortion_good_middle()
    {
        bool ranFinally = false;
        async Plan<string> Deadly(string query)
        {
            throw new Exception("exn");
#pragma warning disable CS0162
            var x = await FailingRetrieve("fail", query);
            return x;
#pragma warning restore CS0162
        }
        async Plan<string> Good(string query)
        {
            try
            {
                var result = await Send(query);
                var next = await Send("jim");
                return result + next;
            }
            finally { ranFinally = true; }
        }
        async Plan<string> Task()
        {
            var (x, y, z) = await Plans.Tuple(Deadly("x"), Good("y"), Deadly("z"));
            return x + y + z;
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<string>.Fail(_ => ranFinally));
    }

    [Test]
    public void Concurrent_loop_retrieval_abortion()
    {
        bool started = false;
        bool ranFinally = false;
        async Plan<int> Deadly(string query) { _ = await FailingRetrieve("fail", query); return 0; }
        async Plan<int> Good(string query)
        {
            started = true;
            try
            {
                _ = await Send(query);
                _ = await Send("jim");
                return 0;
            }
            finally { ranFinally = true; }
        }
        async Plan<int> Task()
        {
            await Plans.ForBatch(new[] { "x", "y", "z" }, async q =>
            {
                if (q == "y") return await Good(q);
                return await Deadly(q);
            });
            return 0;
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<int>.Fail(_ => started && ranFinally));
    }

    [Test]
    public void Concurrent_loop_logic_abortion()
    {
        bool ranFinally = false;
        async Plan<int> Deadly(string query)
        {
            throw new Exception("logic");
#pragma warning disable CS0162
            _ = await FailingRetrieve("fail", query);
            return 0;
#pragma warning restore CS0162
        }
        async Plan<int> Good(string query)
        {
            try
            {
                _ = await Send(query);
                _ = await Send("jim");
                return 0;
            }
            finally { ranFinally = true; }
        }
        async Plan<int> Task()
        {
            await Plans.ForBatch(new[] { "x", "y", "z" }, async q =>
            {
                if (q == "y") return await Good(q);
                return await Deadly(q);
            });
            return 0;
        }
        TestRunner.Run(
            Task,
            System.Array.Empty<string[]>(),
            ExpectedResult<int>.Fail(_ => ranFinally));
    }
}
