module Rezoom.Plan
open System
open System.Collections.Generic

////////////////////////////////////////////////////////////
// Internal guts we use throughout the module.
////////////////////////////////////////////////////////////

let internal abort() = raise (PlanAbortException "Task aborted")

let internal abortSteps (steps : 'a Step seq) (reason : exn) : 'b =
    let exns = new ResizeArray<_>()
    exns.Add(reason)
    for _, resume in steps do
        try
            // this should fail with a DataTaskAbortException
            ignore <| resume BatchAbort
        with
        | PlanAbortException _ -> ()
        | exn -> exns.Add(exn)
    if exns.Count > 1 then raise (aggregate exns)
    else dispatchRaise reason

let internal abortTask (task : 'a Plan) (reason : exn) : 'b =
    match task with
    | Step (_, resume) ->
        try
            ignore <| resume BatchAbort
            dispatchRaise reason
        with
        | PlanAbortException _ -> dispatchRaise reason
        | exn -> raise (new AggregateException(reason, exn))
    | _ ->
        dispatchRaise reason

////////////////////////////////////////////////////////////
// Fundamentals:
// Tasks that immediately return and tasks that encapsulate a single step.
////////////////////////////////////////////////////////////

/// Monadic return for `DataTask`s.
/// Creates a `DataTask` with no steps, whose immediate result is `result`.
let inline ret (result : 'a) = Result result

/// Monoidal identity for `DataTask`.
/// Equivalent to `ret ()`.
let zero = ret ()

/// Convert a `DataRequest<'a>` to a `DataTask<'a>`.
let ofErrand (request : Errand<'a>) : Plan<'a> =
    let onResponse =
        function
        | BatchLeaf (RetrievalSuccess suc) -> ret (Unchecked.unbox suc : 'a)
        | BatchLeaf (RetrievalException exn) -> dispatchRaise exn
        | BatchAbort -> abort()
        | BatchPair _
        | BatchMany _ -> logicFault "Incorrect response shape for data request"
    Step (BatchLeaf (request :> Errand), onResponse)

////////////////////////////////////////////////////////////
// Mapping of plain-old functions over `DataTask`s.
//
// This lets you transform the eventual values produced by the task.
////////////////////////////////////////////////////////////

let inline _mapInline map f task =
    match task with
    | Result r -> Result (f r)
    | Step s -> Step (map f s)
let rec _mapRecursive (f : 'a -> 'b) ((pending, resume) : 'a Step) : 'b Step =
    pending, fun responses -> _mapInline _mapRecursive f (resume responses)
/// Map a function over the result of a `DataTask<'a>`, producing a new `DataTask<'b>`.
and inline map (f : 'a -> 'b) (task : 'a Plan): 'b Plan =
    _mapInline _mapRecursive f task

////////////////////////////////////////////////////////////
// Monadic `bind`.
//
// This lets you sequence together tasks where the result of the
// first task is necessary to decide what to do as the next task.
////////////////////////////////////////////////////////////

let inline _bindInline bind task cont =
    match task with
    | Result r -> cont r
    | Step (pending, resume) ->
        Step (pending, fun responses -> bind (resume responses) cont)
let rec _bindRecursive task cont =
    _bindInline _bindRecursive task cont
/// Chain a continuation `DataTask` onto an existing `DataTask` to
/// get a new `DataTask`.
/// The continuation can be dependent on the result of the first task.
let inline bind (task : 'a Plan) (cont : 'a -> 'b Plan) : 'b Plan =
    _bindInline (_bindInline _bindRecursive) task cont

////////////////////////////////////////////////////////////
// Applicative functor `apply`.
//
// This lets you combine the results of multiple independent
// tasks into a single value, while allowing them to execute
// concurrently and share batchable resources.
////////////////////////////////////////////////////////////

/// Create a task that will eventually apply the function produced by
/// `taskF` to the value produced by `taskA` to obtain its result.
/// The two tasks are independent, so they will execute concurrently and
/// share batchable resources.
let rec apply (taskF : Plan<'a -> 'b>) (taskA : Plan<'a>) : Plan<'b> =
    match taskF, taskA with
    | Result f, Result a ->
        Result (f a)
    | Result f, step ->
        map ((<|) f) step
    | step, Result a ->
        map ((|>) a) step
    | Step (pendingF, resumeF), Step (pendingA, resumeA) ->
        let pending = BatchPair (pendingF, pendingA)
        let onResponses =
            function
            | BatchPair (rspF, rspA) ->
                let mutable exnF : exn = null
                let mutable exnA : exn = null
                let mutable resF : Plan<'a -> 'b> = Unchecked.defaultof<_>
                let mutable resA : Plan<'a> = Unchecked.defaultof<_>
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

/// Create a task that runs `taskA` and `taskB` concurrently and combines their results into a tuple.
let tuple2 (taskA : 'a Plan) (taskB : 'b Plan) : ('a * 'b) Plan =
    apply
        (map (fun a b -> a, b) taskA)
        taskB

/// Create a task that runs `taskA`, `taskB`, and `taskC` concurrently and combines their results into a tuple.
let tuple3 (taskA : 'a Plan) (taskB : 'b Plan) (taskC : 'c Plan) : ('a * 'b * 'c) Plan =
    apply
        (apply
            (map (fun a b c -> a, b, c) taskA)
            taskB)
        taskC

/// Create a task that runs `taskA`, `taskB`, `taskC`, and `taskD` concurrently
/// and combines their results into a tuple.
let tuple4
    (taskA : 'a Plan)
    (taskB : 'b Plan)
    (taskC : 'c Plan)
    (taskD : 'd Plan)
    : ('a * 'b * 'c * 'd) Plan =
    apply
        (apply
            (apply
                (map (fun a b c d -> a, b, c, d) taskA)
                taskB)
            taskC)
        taskD

////////////////////////////////////////////////////////////
// Exception handling.
////////////////////////////////////////////////////////////

/// Wrap a `DataTask<'a>` with an exception handler.
/// The exception handler `catcher` will be called if an exception is thrown
/// during execution of `wrapped`, whether it's in creating the `DataTask`
/// to be run or in executing any step of the resulting task.
/// The exception handler may rethrow the exception.
let rec tryCatch (wrapped : unit -> 'a Plan) (catcher : exn -> 'a Plan) =
    try
        match wrapped() with
        | Result _ as result -> result
        | Step (pending, resume) ->
            let onResponses (responses : Responses) =
                tryCatch (fun () -> resume responses) catcher
            Step (pending, onResponses)
    with
    | PlanAbortException _ -> reraise() // don't let them catch these
    | ex -> catcher(ex)

/// Wrap a `DataTask<'a>` with a block that must execute.
/// When the task is executed, the function `onExit` will be called
/// after `wrapped` completes, regardless of whether the task
/// succeeded, failed to be created, or failed while partially executed.
let rec tryFinally (wrapped : unit -> 'a Plan) (onExit : unit -> unit) =
    let mutable cleanExit = false
    let task =
        try
            match wrapped() with
            | Result _ as result ->
                cleanExit <- true
                result
            | Step (pending, resume) ->
                let onResponses (responses : Responses) =
                    tryFinally (fun () -> resume responses) onExit
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

////////////////////////////////////////////////////////////
// Looping.
//
// There are two ways to loop over a sequence of inputs.
//
// One is to lazily enumerate, chaining together the tasks handling each step with `bind`.
//
// The other is to enumerate immediately and generate tasks for all the sequence's elements,
// then combine them with `apply` so that they can execute concurrently.
// Rather than actually using `apply`, we treat this as a special case for efficiency's sake.
////////////////////////////////////////////////////////////

let rec private forIterator (enumerator : 'a IEnumerator) (iteration : 'a -> unit Plan) =
    if not <| enumerator.MoveNext() then zero else
    bind (iteration enumerator.Current) (fun () -> forIterator enumerator iteration)

/// Monadic iteration.
/// Create a task that lazily iterates a sequence, executing `iteration` for each element.
let forM (sequence : 'a seq) (iteration : 'a -> unit Plan) =
    let enumerator = sequence.GetEnumerator()
    tryFinally
        (fun () -> forIterator enumerator iteration)
        (fun () -> enumerator.Dispose())

let rec private forAs (tasks : (unit -> unit Plan) seq) : unit Plan =
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

/// Applicative iteration.
/// Create a task that strictly iterates a sequence, creating a `DataTask` for each element
/// using the given `iteration` function, then runs those tasks concurrently.
let forA (sequence : 'a seq) (iteration : 'a -> unit Plan) =
    forAs (sequence |> Seq.map (fun element () -> iteration element))
