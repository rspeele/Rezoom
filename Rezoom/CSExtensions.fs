namespace Rezoom
open System.Runtime.CompilerServices

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member ToDataTask(request : DataRequest<'a>) =
        DataTask.fromDataRequest request
