#if DELIBERATELY_BROKEN
// Uncomment this conditional to confirm the cross-contamination guard fires.
// When DELIBERATELY_BROKEN is defined, this file should fail to compile with
// a constraint error on `await Task.Delay(...)`: `TaskAwaiter` does not
// satisfy the `IPlanAwaiter` constraint on `PlanMethodBuilder<T>.AwaitUnsafeOnCompleted`.
using System.Threading.Tasks;
using Rezoom;

namespace Rezoom.CS.Test;

internal static class CrossContaminationProbe
{
    public static async Plan<int> ShouldNotCompile()
    {
        await Task.Delay(1);
        return 1;
    }
}
#endif
