/// Provides operators that help construct certain types of tasks.

[<AutoOpen>]
module Data.Resumption.Operators
    /// Operator version of DataMonad.bind.
    let (>>=) task continuation = DataMonad.bind task continuation
    /// Operator version of DataMonad.map. Use like `function <@> task`.
    /// In Haskell, this would be <$>, but that is not a legal operator name in F#.
    let (<@>) mapping task = DataMonad.map mapping task
    /// Operator version of DataMonad.apply.
    /// Best used in conjunction with `<@>`, like `(fun a b -> ...) <@> taskA <*> taskB`.
    let (<*>) functionTask inputTask = DataMonad.apply functionTask inputTask

    /// Apply two DataTasks, combining their results into a tuple.
    let datatuple2 taskA taskB = (fun a b -> (a, b)) <@> taskA <*> taskB
    /// Apply two DataTasks, combining their results into a tuple.
    let (<..>) taskA taskB = datatuple2 taskA taskB

    let datatuple3 taskA taskB taskC =
        (fun a b c -> (a, b, c)) <@> taskA <*> taskB <*> taskC
    let datatuple4 taskA taskB taskC taskD =
        (fun a b c d -> (a, b, c, d)) <@> taskA <*> taskB <*> taskC <*> taskD