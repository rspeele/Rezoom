module Data.Resumption.DataTask
open System
open System.Collections.Generic

let internal abort() = raise (DataTaskAbortException "Task aborted")

let internal abortSteps (steps : 'a Step seq) (reason : exn) : 'b =
    let exns = new ResizeArray<_>()
    exns.Add(reason)
    for _, resume in steps do
        try
            ignore <| resume BatchAbort // this should fail with a DataTaskAbortException
        with
        | DataTaskAbortException _ -> ()
        | exn -> exns.Add(exn)
    if exns.Count > 1 then raise (aggregate exns)
    else dispatchRaise reason

let internal abortTask (task : 'a DataTask) (reason : exn) : 'b =
    match task with
    | Step (_, resume) ->
        try
            ignore <| resume BatchAbort
            dispatchRaise reason
        with
        | DataTaskAbortException _ -> dispatchRaise reason
        | exn -> raise (new AggregateException(reason, exn))
    | _ ->
        dispatchRaise reason

let inline ret (result : 'a) = Result result
let zero = ret ()

let fromDataRequest (request : DataRequest<'a>) : Step<'a> =
    BatchLeaf (request :> DataRequest), function
        | BatchLeaf (RetrievalSuccess suc) -> Result (Unchecked.unbox suc : 'a)
        | BatchLeaf (RetrievalException exn) -> dispatchRaise exn
        | BatchAbort -> abort()
        | BatchPair _
        | BatchMany _ -> logicFault "Incorrect response shape for data request"

// Map

let inline mapTI mapS f task =
    match task with
    | Result r -> Result (f r)
    | Step s -> Step (mapS f s)

let rec mapS (f : 'a -> 'b) ((pending, resume) : 'a Step) : 'b Step =
    pending, fun responses -> mapTI mapS f (resume responses)
and inline mapT (f : 'a -> 'b) (task : 'a DataTask): 'b DataTask =
    mapTI mapS f task

// Bind

let inline bindTI bindTI task cont =
    match task with
    | Result r -> cont r
    | Step (pending, resume) ->
        Step (pending, fun responses -> bindTI (resume responses) cont)

let rec bindTF task cont =
    bindTI bindTF task cont
        
let inline bindTT (task : 'a DataTask) (cont : 'a -> 'b DataTask) : 'b DataTask =
    bindTI (bindTI bindTF) task cont

// Apply

let rec apply (taskF : DataTask<'a -> 'b>) (taskA : DataTask<'a>) : DataTask<'b> =
    match taskF, taskA with
    | Result f, Result a ->
        Result (f a)
    | Result f, step ->
        mapT (fun a -> f a) step
    | step, Result a ->
        mapT (fun f -> f a) step
    | Step (pendingF, resumeF), Step (pendingA, resumeA) ->
        let pending = BatchPair (pendingF, pendingA)
        let onResponses =
            function
            | BatchPair (rspF, rspA) ->
                let mutable exnF : exn = null
                let mutable exnA : exn = null
                let mutable resF : DataTask<'a -> 'b> = Unchecked.defaultof<_>
                let mutable resA : DataTask<'a> = Unchecked.defaultof<_>
                try
                    resF <- resumeF rspF
                with
                | exn ->
                    exnF <- exn
                try
                    resA <- resumeA rspA
                with
                | exn -> exnA <- exn
                if isNull exnF && isNull exnA then
                    apply resF resA
                else if not (isNull exnF) && not (isNull exnA) then
                    raise (new AggregateException(exnF, exnA))
                else if isNull exnF then
                    abortTask resF exnA
                else
                    abortTask resA exnF
            | BatchAbort -> abort()
            | BatchLeaf _
            | BatchMany _ -> logicFault "Incorrect response shape for applied pair"
        Step (pending, onResponses)

let tuple2 (taskA : 'a DataTask) (taskB : 'b DataTask) : ('a * 'b) DataTask =
    apply
        (mapT (fun a b -> a, b) taskA)
        taskB

let tuple3 (taskA : 'a DataTask) (taskB : 'b DataTask) (taskC : 'c DataTask) : ('a * 'b * 'c) DataTask =
    apply
        (apply
            (mapT (fun a b c -> a, b, c) taskA)
            taskB)
        taskC

let tuple4
    (taskA : 'a DataTask)
    (taskB : 'b DataTask)
    (taskC : 'c DataTask)
    (taskD : 'd DataTask)
    : ('a * 'b * 'c * 'd) DataTask =
    apply
        (apply
            (apply
                (mapT (fun a b c d -> a, b, c, d) taskA)
                taskB)
            taskC)
        taskD

// Exception handling

let rec catchTT (wrapped : unit -> 'a DataTask) (catcher : exn -> 'a DataTask) =
    try
        match wrapped() with
        | Result _ as result -> result
        | Step (pending, resume) ->
            let onResponses (responses : Responses) =
                catchTT (fun () -> resume responses) catcher
            Step (pending, onResponses)
    with
    | DataTaskAbortException _ -> reraise() // don't let them catch these
    | ex -> catcher(ex)

let rec finallyTT (wrapped : unit -> 'a DataTask) (onExit : unit -> unit) =
    let mutable cleanExit = false
    let task =
        try
            match wrapped() with
            | Result _ as result ->
                cleanExit <- true
                result
            | Step (pending, resume) ->
                let onResponses (responses : Responses) =
                    finallyTT (fun () -> resume responses) onExit
                Step (pending, onResponses)
        with
        | ex ->
            try
                onExit()
            with
            | inner ->
                raise (aggregate [|ex; inner|])
            reraise()
    if cleanExit then
        // run outside of the try/catch so we don't risk recursion
        onExit()
    task

// Looping

let rec private forIterator (enumerator : 'a IEnumerator) (iteration : 'a -> unit DataTask) =
    if not <| enumerator.MoveNext() then zero else
    bindTT (iteration enumerator.Current) (fun () -> forIterator enumerator iteration)

let forM (sequence : 'a seq) (iteration : 'a -> unit DataTask) =
    let enumerator = sequence.GetEnumerator()
    finallyTT
        (fun () -> forIterator enumerator iteration)
        (fun () -> enumerator.Dispose())

let rec private forAs (tasks : (unit -> unit DataTask) seq) : unit DataTask =
    let steps =
        let steps = new ResizeArray<_>()
        let exns = new ResizeArray<_>()
        for task in tasks do
            try
                match task() with
                | Step step -> steps.Add(step)
                | Result _ -> ()
            with
            | exn -> exns.Add(exn)
        if exns.Count > 0 then abortSteps steps (aggregate exns)
        else steps
    if steps.Count <= 0 then zero
    else
        let pending =
            let arr = Array.zeroCreate steps.Count
            for i = 0 to steps.Count - 1 do
                arr.[i] <- fst steps.[i]
            BatchMany arr
        let onResponses =
            function
            | BatchMany responses ->
                responses
                |> Seq.mapi (fun i rsp () -> snd steps.[i] rsp)
                |> forAs
            | BatchAbort -> abort()
            | BatchPair _
            | BatchLeaf _ -> logicFault "Incorrect response shape for applicative batch"
        Step (pending, onResponses)

let forA (sequence : 'a seq) (iteration : 'a -> unit DataTask) =
    forAs (sequence |> Seq.map (fun element () -> iteration element))
