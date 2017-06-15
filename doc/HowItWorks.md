# How does Rezoom work?

Rezoom uses (and gets its name from) something called a resumption monad.

Facebook's [Haxl](https://github.com/facebook/Haxl) uses the same concept, but
is geared more towards reading data, while Rezoom is designed to work well with
code that updates and creates data, too. Also, Haxl is for the few people who
use Haskell, while Rezoom could potentially be used by dozens of F# developers.

## The quick version

A Rezoom `Plan` can be one of two things:

* A completed result.

* A set of `Errand`s that need to be executed and a function that takes their
  results and returns the next `Plan` to execute.

An `Errand` is generally some latency-inducing work needing to be done, such as
a SQL command. Errands can be combined to execute in a single batch.

Execution is achieved by executing any pending errands and stepping to the next
plan until reaching a completed result.

Batching is achieved by making a new plan which combines the pending errands of
its sub-plans at each step.

Caching is achieved by given each errand an ID and keeping a dictionary of
results by errand ID.

Cache invalidation is achieved by giving each errand a category ID, dependency
bitmask, and invalidation bitmask. When an errand runs, all errands with the
same category ID whose dependency bitmasks overlapped with the executed errand's
invalidation bitmask (i.e. `b1 &&& b2 <> 0`) are purged from the cache.

## A worked example

Here's a minimal version of the `Plan` type called `TrivialPlan`. To keep things
simple, `TrivialPlan` is specialized to only work with SQL queries.

```fsharp

// For this example, our queries are just SQL strings
type Query = string

// ... and our query results are sequences of untyped rows (obj arrays).
type QueryResponse = array<obj> seq

// A plan can that will eventually yield a result of type 'a can be one of two things:
type TrivialPlan<'a> =
    // Either it's already completed and carries a result...
    | Done of result : 'a
    // Or it's paused, waiting for a round-trip to run.
    | Pending of RoundTrip<'a>

    // In the latter case we have two things:
and RoundTrip<'a> =
    {   // The queries to run in the round-trip...
        Queries : Query array
        // And a function to get the next step in the plan, once we have the responses to our queries.
        Resume : QueryResponse array -> TrivialPlan<'a>
    }

```

You can probably imagine how you might write a function to execute a plan like this. Assuming you have a library
function to run SQL queries in a batch, it's tiny:

```fsharp
let rec exec (plan : 'a TrivialPlan) =
    match plan with
    | Done x -> x
    | Pending trip ->
        let responses = runBatchOfQueries trip.Queries
        exec (trip.Resume responses)
```

As an optimization you could add a cache for queries, so you don't run the same one more than once.

```fsharp
let rec execWithCache (cache : IDictionary<Query, QueryResult>) (plan : 'a TrivialPlan) =
    match plan with
    | Done x -> x
    | Pending trip ->
        let pending = trip.Queries |> Array.filter (not << cache.ContainsKey)
        let responses = runBatchOfQueries pending |> Array.zip Pending
        for query, response in responses do
            cache.[query] <- response
        let allResponses = trip.Queries |> Array.map (fun q -> cache.[q])
        execWithCache cache (trip.Resume allResponses)
```

Ok, so that covers how plan execution and caching could work.

In Rezoom, it's a little fancier. Commands can specify that they aren't
cacheable or even that running them will invalidate the cached results of other
commands. But the basic idea is there.

Now, how could you write the `TrivialPlan`s to feed into your `exec` function?
Well, it's easy to write one that just returns a value:

```fsharp
let ret (value : 'a) : 'a TrivialPlan = Done value
```

How about one that wraps a query?

```fsharp
let query (sql : Query) : QueryResult TrivialPlan =
    {   // We just have the one query to run.
        Queries = [| sql |]
        // Given our expected single query result, say we don't have a next step and we're done here.
        Resume = fun [| result |] -> Done result
    } |> Pending
```

To write real code though, we need to run multiple queries. We can sequence
together actions by "binding" the result of one plan as the input to generate
the next plan.

```fsharp
let rec bind (first : 'a TrivialPlan) (next : 'a -> 'b TrivialPlan) =
    match first with
    // If the first plan is done, we can use its result to move onto the next.
    | Done x -> next x
    // Otherwise, we have to wrap the first plan's pending step, so that upon resuming execution,
    // we can check again to possibly proceed to the next step.
    | Pending step ->
        {   Queries = step.Queries
            Resume = fun responses -> bind (step.Resume responses) next
        } |> Pending
```

Now we can write fancy business logic like this:

```fsharp
bind (query "select * from Users where Id = 1")
    (fun users ->
        bind (query "select * from Groups where Id = 1")
            (fun groups ->
                printfn "Users: %A; Groups: %A" users groups
                ret (users, groups)
            )
    )
```

This ugly chain of nested functions is roughly what F# computation expressions translate to.
This means we could rewrite the above like so -- much more readable!

```fsharp
trivialplan {
    let! users = query "select * from Users where Id = 1"
    let! groups = query "select * from Groups where Id = 1"
    printfn "Users: %A; Groups: %A" users groups
    return (users, groups)
}
```

Now, so far, we don't have a way to share round-trips between plans. However,
since a plan can carry an array of queries, not just one, we can combine any two
plans into one, which'll execute both side-by-side.

```fsharp
let rec sideBySide (a : 'a TrivialPlan) (b : 'b TrivialPlan) : ('a * 'b) TrivialPlan =
    match a, b with
    // If they're both done, tuple up the results.
    | Done gotA, Done gotB -> Done (gotA, gotB)
    // If only one is done, just keep working on the next one, but remember to
    // include the first result when the other finishes.
    | Done gotA, b -> bind b (fun gotB -> ret (gotA, gotB))
    | a, Done gotB -> bind a (fun gotA -> get (gotA, gotB))
    // If they're both pending...
    | Pending pendA, Pending pendB ->
        {   // Include the queries for both in a single round-trip.
            Queries = Array.append pendA.Queries pendB.Queries
            // When we get responses, resume both plans and keep on running them side-by-side.
            Resume =
                fun responses ->
                    let responsesA = Array.sub responses 0 pendA.Queries.Length
                    let responsesB = Array.sub pendA.Queries.Length pendB.Queries.Length
                    sideBySide (pendA.Resume responsesA) (pendB.Resume responsesB)
        } |> Pending
```

Notice that if the two plans have multiple steps, they'll take turns stepping
forward, so the code of the second one will be executing interleaved with the
code of the first one. This means this approach is only suitable when the two
plans don't need to be run in any particular order.

With Rezoom, something like the above is used when you bind two variables at the
same time, like in `let! x, y = getX, getY`.

Everything gets more complicated in real life because we also have to support
user-defined exception handlers, `finally` blocks, loops, and beyond. But the
simplified plan functions described here are close enough to use as a mental
model. Most importantly, they take the magic out of `plan` blocks and help you
understand what they're capable of.