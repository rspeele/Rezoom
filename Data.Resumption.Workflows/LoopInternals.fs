module Data.Resumption.LoopInternals
open Data.Resumption
open Data.Resumption.DataTaskInternals
open Data.Resumption.BindInternals
open System
open System.Collections.Generic

let rec private forIteratorTT (enumerator : 'a IEnumerator) (iteration : 'a -> unit DataTask) =
    if not <| enumerator.MoveNext() then zero else
    bindTT (iteration enumerator.Current) (fun () -> forIteratorTT enumerator iteration)

let forTT (sequence : 'a seq) (iteration : 'a -> unit DataTask) =
    let enumerator = sequence.GetEnumerator()
    ExceptionInternals.finallyTT
        (fun () -> forIteratorTT enumerator iteration)
        (fun () -> enumerator.Dispose())