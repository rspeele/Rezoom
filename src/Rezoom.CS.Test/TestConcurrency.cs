using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using Rezoom;
using Rezoom.CS;
using static Rezoom.CS.Test.Helpers;

namespace Rezoom.CS.Test;

[TestFixture]
public class TestConcurrency
{
    [Test]
    public void Strict_pair()
    {
        async Plan<string> Task()
        {
            var q = await Send("q");
            var r = await Send("r");
            return q + r;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "q" },
                new[] { "r" },
            },
            ExpectedResult<string>.Ok("qr"));
    }

    [Test]
    public void Concurrent_pair()
    {
        async Plan<string> Task()
        {
            var (q, r) = await Plans.Tuple(Send("q"), Send("r"));
            return q + r;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "q", "r" },
            },
            ExpectedResult<string>.Ok("qr"));
    }

    [Test]
    public void Chaining_concurrency()
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
            var (x, y, z) = await Plans.Tuple(TestTask("x"), TestTask("y"), TestTask("z"));
            return x + " " + y + " " + z;
        }
        TestRunner.Run(
            Task,
            new[] {
                new[] { "x1", "y1", "z1" },
                new[] { "x2", "y2", "z2" },
                new[] { "x3", "y3", "z3" },
            },
            ExpectedResult<string>.Ok("x1x2x3 y1y2y3 z1z2z3"));
    }

    [Test]
    public void List_concurrency()
    {
        async Plan<FSharpList<string>> Task()
        {
            return await Plans.ConcurrentList(new[] { Send("x"), Send("y"), Send("z") });
        }
        TestRunner.Run(
            Task,
            new[] { new[] { "x", "y", "z" } },
            ExpectedResult<FSharpList<string>>.Ok(ListModule.OfSeq(new[] { "x", "y", "z" })));
    }

    [Test]
    public void Chaining_list_concurrency()
    {
        async Plan<string> A() { _ = await Send("x"); return await Send("y"); }
        async Plan<string> B() { _ = await Send("y"); return await Send("q"); }
        async Plan<string> C() { var z = await Send("z"); return await Send(z + "r"); }
        async Plan<FSharpList<string>> Task() =>
            await Plans.ConcurrentList(new[] { A(), B(), C() });

        TestRunner.Run(
            Task,
            new[] {
                new[] { "x", "y", "z" },
                new[] { "q", "zr" },
            },
            ExpectedResult<FSharpList<string>>.Ok(ListModule.OfSeq(new[] { "y", "q", "zr" })));
    }

    [Test]
    public void Chaining_list_concurrency_with_preceding_cached()
    {
        async Plan<string> A() { _ = await Send("x"); return await Send("q"); }
        async Plan<string> B() { _ = await Send("y"); return await Send("r"); }
        async Plan<string> C() { var z = await Send("z"); return await Send("s"); }

        async Plan<FSharpList<FSharpList<string>>> Task()
        {
            var x = await Send("x");
            return await Plans.ConcurrentList(new[] {
                Plans.ConcurrentList(new[] { A(), B() }),
                Plans.ConcurrentList(new[] { C() }),
            });
        }

        var expected = ListModule.OfSeq(new[] {
            ListModule.OfSeq(new[] { "q", "r" }),
            ListModule.OfSeq(new[] { "s" }),
        });

        TestRunner.Run(
            Task,
            new[] {
                new[] { "x" },
                new[] { "q", "y", "z" },
                new[] { "r", "s" },
            },
            ExpectedResult<FSharpList<FSharpList<string>>>.Ok(expected));
    }
}
