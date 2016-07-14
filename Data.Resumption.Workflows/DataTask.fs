namespace Data.Resumption

type DataResponse =
    | RetrievalSuccess of obj
    | RetrievalException of exn

type Batch<'a> =
    | BatchLeaf of 'a
    | BatchPair of ('a Batch * 'a Batch)
    | BatchList of ('a Batch array)
    | BatchAbort
    member this.MapCS(f : System.Func<'a, 'b>) =
        match this with
        | BatchLeaf x -> BatchLeaf (f.Invoke(x))
        | BatchPair (l, r) -> BatchPair (l.MapCS(f), r.MapCS(f))
        | BatchList arr -> BatchList (arr |> Array.map (fun b -> b.MapCS(f)))
        | BatchAbort -> BatchAbort
    member this.Map(f : 'a -> 'b) =
        match this with
        | BatchLeaf x -> BatchLeaf (f x)
        | BatchPair (l, r) -> BatchPair (l.Map(f), r.Map(f))
        | BatchList arr -> BatchList (arr |> Array.map (fun b -> b.Map(f)))
        | BatchAbort -> BatchAbort

type Requests = DataRequest Batch
type Responses = DataResponse Batch

[<AllowNullLiteral>]
type Step<'result> =
    class
        val public Pending : Requests
        val public Resume : Responses -> DataTask<'result>
        new (pending, resume) = { Pending = pending ; Resume = resume }
        member inline this.ToDataTask() = new DataTask<'result>(this)
    end

and DataTask<'result> =
    struct 
        val public Immediate : 'result
        val public Step : Step<'result>
        new(result : 'result) = { Immediate = result; Step = Unchecked.defaultof<_> }
        new(step : 'result Step) = { Immediate = Unchecked.defaultof<'result>; Step = step }
        member inline this.ToDataTask() = this
    end

type Immediate<'result> =
    struct
        val public Immediate : 'result
        new (result) = { Immediate = result }
        member inline this.ToDataTask() = new DataTask<'result>(this.Immediate)
    end

/// Hint that it is OK to batch the given sequence or task
type BatchHint<'a> = internal | BatchHint of 'a

[<AutoOpen>]
module BatchHints =
    let batch x = BatchHint x