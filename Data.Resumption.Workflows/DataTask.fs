namespace Data.Resumption

type DataResponse =
    | RetrievalSuccess of obj
    | RetrievalException of exn

type Batch<'a> =
    | BatchLeaf of 'a
    | BatchPair of ('a Batch * 'a Batch)
    | BatchList of ('a Batch array)
    | BatchAbort

type Requests = DataRequest Batch
type Responses = DataResponse Batch

type RequestsPending<'result>() =
    class end

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
    end

type Immediate<'result> =
    struct
        val public Immediate : 'result
        new (result) = { Immediate = result }
        member inline this.ToDataTask() = new DataTask<'result>(this.Immediate)
    end

/// Hint that it is OK to batch the given sequence or task
type BatchHint<'a> = | BatchHint of 'a