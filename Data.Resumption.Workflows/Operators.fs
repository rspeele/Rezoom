/// Provides operators that help construct certain types of tasks.

[<AutoOpen>]
module Data.Resumption.Operators
//open System
//open System.Threading.Tasks
//
///// Operator version of DataMonad.bind.
//let (>>=) task continuation = DataTaskMonad.bind task continuation
///// Operator version of DataMonad.map. Use like `function <@> task`.
///// In Haskell, this would be <$>, but that is not a legal operator name in F#.
//let (<@>) mapping task = DataTaskMonad.map mapping task
///// Operator version of DataMonad.apply.
///// Best used in conjunction with `<@>`, like `(fun a b -> ...) <@> taskA <*> taskB`.
//let (<*>) functionTask inputTask = DataTaskMonad.apply functionTask inputTask
//
///// Apply two DataTasks, combining their results into a tuple.
//let datatuple2 taskA taskB = (fun a b -> (a, b)) <@> taskA <*> taskB
///// Apply two DataTasks, combining their results into a tuple.
//let (<..>) taskA taskB = datatuple2 taskA taskB
//
///// Apply three DataTasks, combining their results into a tuple.
//let datatuple3 taskA taskB taskC =
//    (fun a b c -> (a, b, c)) <@> taskA <*> taskB <*> taskC
///// Apply four DataTasks, combining their results into a tuple.
//let datatuple4 taskA taskB taskC taskD =
//    (fun a b c d -> (a, b, c, d)) <@> taskA <*> taskB <*> taskC <*> taskD
//
///// Wraps a datatask or sequence to indicate that it can be mixed together
///// with the following task via `apply`. This is used in overload resolution
///// within `DataTaskBuilder`.
//type Batch<'a> = internal Batch of 'a
//
///// Mark a datatask or sequence to be evaluated mixed together with following
///// tasks if possible. This is used in overload resolution within the `datatask`
///// computation expression builder.
//let batch x = Batch x
//
///// Convert a TPL task to a data task.
//let await (task : Task<'a>) = (Func<_>(fun () -> task)).ToDataTask()