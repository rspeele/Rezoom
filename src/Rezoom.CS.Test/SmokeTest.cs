using NUnit.Framework;
using Rezoom;

namespace Rezoom.CS.Test;

[TestFixture]
public class SmokeTest
{
    static async Plan<int> Trivial()
    {
        return 42;
    }

    static async Plan<int> TwoReturns()
    {
        var a = await Trivial();
        var b = await Trivial();
        return a + b;
    }

    [Test]
    public void Async_Plan_Of_T_compiles_and_runs()
    {
        var result = Execution.execute(Execution.ExecutionConfig.Default, Trivial()).Result;
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Sequential_await_chain_works()
    {
        var result = Execution.execute(Execution.ExecutionConfig.Default, TwoReturns()).Result;
        Assert.That(result, Is.EqualTo(84));
    }
}
