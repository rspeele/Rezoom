# Writing straightforward plans

The simplest possible plan is one that just wraps an already-known value.

You can create this using either a `plan{}` computation expression:

```fsharp
let plan1 = plan { return 1 }
```

...Or with the `Plan.ret` function:

```fsharp
let plan1 = Plan.ret 1
```

The next-simplest is one that applies a synchronous function to transform the
result of another plan. Again, this can be accomplished via the `plan{}`
computation expression builder:

```fsharp
let plan2 =
    plan {
        let! x = plan1
        return x + 1
    }
```

...Or with the `Plan.map` function.

```fsharp
let plan2 = Plan.map (fun x -> x + 1) plan1
```

To chain multiple plans together, it is almost always most readable to use the
computation expression:

```fsharp
let plan3 =
    plan {
        let! x = plan1
        let! y = plan2
        return x + y
    }
```

...But it can also be accomplished with `Plan.bind`. The below code is equivalent to the above:

```fsharp
let plan3 =
    Plan.bind plan1 (fun x ->
        Plan.bind plan2 (fun y ->
            Plan.ret (x + y)))
```

## Ignoring plan results

If you have a `Plan<unit>`, you can use `do!` to run it in a computation expression.
`do! myPlan` is equivalent to `let! () = myPlan`.

If you have another type of plan, such as a `Plan<int>` but don't care about its
result, you can either bind it to a wildcard with `let! _ = myPlan` or map its
result to unit with `do! Plan.map ignore myPlan`.

# Writing batching plans

The easiest way to batch multiple plans together is to bind two or three of them
at once in a computation expression. The computation expression builder
overloads `Bind` for this.

```fsharp
let plan4 =
    plan {
        let! x, y = plan1, plan2
        return x + y
    }
```

This is equivalent to using the `Plan.tuple2` function:

```
let plan4 =
    Plan.bind
        (Plan.tuple2 plan1 plan2)
        (fun (x, y) -> Plan.ret (x + y))
```

If you have a list of plans all returning the same type, you can batch them
together with `Plan.concurrentList`, which turns an `'a Plan list` into an `'a
list Plan`:

```
let getFoos =
    Plan.concurrentList
        [ for fooId in 1..10 ->
            getFoo fooId
        ]
```

If you simply want to run the plans concurrent for their side effects and don't
care about the results, you can run them in a `for ... in batch` loop in a
computation expression:

```
let recycleAllFoos fooIds =
    plan {
        for fooId in batch fooIds do
            do! recycleFoo fooId
    }
```

Note the `batch` function which wraps the list of inputs so that the concurrent
looping overload will be used. Loops over inputs _not_ wrapped in `batch` will
be executed sequentially.

