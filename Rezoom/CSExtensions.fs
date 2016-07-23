namespace Rezoom
open System.Runtime.CompilerServices

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member ToPlan(request : Errand<'a>) =
        Plan.ofErrand request
