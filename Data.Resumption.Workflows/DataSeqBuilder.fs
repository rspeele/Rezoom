[<AutoOpen>]
module Data.Resumption.DataSeqBuilder
open Data.Resumption
open System
open System.Threading.Tasks

type DataSeqBuilder() =
    member __.Zero() : _ dataseq  =
        DataSeqMonad.zero ()

    member __.Yield(value) : _ dataseq =
        DataSeqMonad.yieldA value
    member __.YieldFrom(subSeq : _ seq) : _ dataseq =
        DataSeqMonad.yieldM subSeq
    member __.YieldFrom(subSeq : _ dataseq) : _ dataseq =
        subSeq

    member __.Bind(task, continuation) : _ dataseq =
        DataSeqMonad.bindTask task continuation
    member __.Bind((taskA, taskB), continuation) : _ dataseq =
        DataSeqMonad.bindTask (datatuple2 taskA taskB) continuation
    member __.Bind((taskA, taskB, taskC), continuation) : _ dataseq =
        DataSeqMonad.bindTask (datatuple3 taskA taskB taskC) continuation
    member __.Bind((taskA, taskB, taskC, taskD), continuation) : _ dataseq =
        DataSeqMonad.bindTask (datatuple4 taskA taskB taskC taskD) continuation

    member __.Using(disposable : #IDisposable, body) : _ dataseq =
        let dispose () =
            match disposable with
            | null -> ()
            | d -> d.Dispose()
        DataSeqMonad.tryFinally (fun () -> body disposable) dispose

    member __.Combine(dataSeq, continuation) : _ dataseq =
        DataSeqMonad.combine dataSeq continuation

    member __.TryFinally(task : unit -> _ dataseq, onExit) : _ dataseq =
        DataSeqMonad.tryFinally task onExit
    member __.TryWith(task : unit -> _ dataseq, exceptionHandler) : _ dataseq =
        DataSeqMonad.tryWith task exceptionHandler

    member __.For(dataSequence : _ dataseq, continuation) : _ dataseq =
        DataSeqMonad.bind dataSequence continuation
    member __.For(sequence, continuation) : _ dataseq =
        DataSeqMonad.bind (DataSeqMonad.yieldM sequence) continuation

    member __.While(condition, iteration) =
        DataSeqMonad.loop condition iteration

    member __.Delay(f : unit -> _ dataseq) = f
    member __.Run(f : unit -> _ dataseq) = f()

let dataseq = new DataSeqBuilder()
