namespace Rezoom
open System.Runtime.CompilerServices

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member ToDataTask(request : Errand<'a>) =
        DataTask.fromDataRequest request
