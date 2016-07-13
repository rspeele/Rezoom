namespace Data.Resumption
open System.Runtime.CompilerServices

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member ToStep(request : DataRequest<'a>) =
        DataTaskInternals.fromDataRequest request
    [<Extension>]
    static member ToDataTask(request : DataRequest<'a>) =
        (DataTaskInternals.fromDataRequest request).ToDataTask()
