module Data.Resumption.LoopInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open Data.Resumption.BindInternals
open System
open System.Collections.Generic

let rec private forIterator (enumerator : 'a IEnumerator) (iteration : 'a -> unit DataTask) =
    if not <| enumerator.MoveNext() then zero else
    bindTT (iteration enumerator.Current) (fun () -> forIterator enumerator iteration)

let forM (sequence : 'a seq) (iteration : 'a -> unit DataTask) =
    let enumerator = sequence.GetEnumerator()
    ExceptionInternals.finallyTT
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
