# Writing your own errands

In Rezoom, an *errand* represents some work which can be batched or cached, such
as a SQL query or request to a web API. A single errand represents a single
logical piece of work like "load data for the user with ID = 37", but multiple
errands that are pending simultaneously can be executed together like "load data
for users 37, 38, and 39".

You can define your own errands to wrap whatever API calls you want.

# Subclassing `Errand`

To define your own errand, you should inherit either `Rezoom.AsynchronousErrand`
or `Rezoom.SynchronousErrand`. Most tasks are best implemented using the former,
but we'll start with the latter since it's simpler.

This is the general pattern for a custom synchronous errand:

```fsharp
open Rezoom

// Example of the batch API we're wrapping.
type MyBatchAPI =
    static member this.GetStringsByInts(ints : int array) : string array =
        // just an example, actual implementation could be anything.
        printfn "running batch %A" ints
        ints |> Array.map string


// A different instance of this type will be instantiated
// for each per execution "step", i.e., point at which
// execution of a plan is paused pending completion of
// 1 or more errands.
type MyStepLocalBatch() =
    let batchInts = ResizeArray<int>()
    let batchResults =
        lazy
            MyBatchAPI.GetStringsByInts(batchInts.ToArray())

    // Add an int to the batch. Return a closure that can be evaluted
    // to force execution of the whole batch then return the result
    // for the int we just added.
    member this.AddToBatch(i) =
        let index = batchInts.Count
        batchInts.Add(i)
        fun () -> batchResults.Value.[index]

let myErrandCacheInfo =
    // Any equality-comparable object; a type in the assembly
    // is usually a good choice. Errands with the same category
    // share the same cache.
    let category = box typeof<MyStepLocalBatch>
    // Any equality-comparable object. Identifies the type of
    // errand we're caching, within the category.
    let identity = box "myErrand"
    { new CacheInfo() with
        override this.Category = category
        override this.Identity = identity
        override this.Cacheable = true
        // Result can be cached without any dependencies.
        override this.DependencyMask = BitMask.Zero
        // Running this does not invalidate any dependencies.
        override this.InvalidationMask = BitMask.Zero
    }

type MyErrand(i : int) =
    inherit SynchronousErrand<string>()

    // Most of the cache info is the same for any `MyErrand`.
    override this.CacheInfo = myErrandCacheInfo
    // Additional piece of cache identity so we don't pull
    // MyErrand(1)'s result for MyErrand(2)
    override this.CacheArgument = box i

    // This is where we define the work to be done.
    // When called, this method is supposed to:
    // 1. add this errand's work to the step-local batch.
    // 2. return a function that can be evaluated to execute the batch
    //    and extract this errand's result.
    override this.Prepare(context : ServiceContext) : unit -> string =
        // Get the batch for this execution step.
        let batch = context.GetService<StepLocal<MyStepLocalBatch>>()
        // Add ourselves to the batch, return the result getter.
        batch.AddToBatch(i)

let batchStringify (i : int) : string Plan =
    MyErrand(i) |> Plan.ofErrand
```

