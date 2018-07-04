# What's this?

Rezoom is a library intended to reduce the pain of dealing with data that lives
across a latency boundary.

![Horrors of latency](https://raw.githubusercontent.com/rspeele/Rezoom/master/doc/images/Latency.png)

Common examples of this nightmare include SQL databases, NoSQL databases, and
web APIs.

It tends to be hard to write abstractions over these data sources because the
round-trip time dominates all other performance concerns, and it's impossible to
optimize without breaking your API.

If somebody is calling `GetUserDetails(userId)` in a loop for 500 different
users, you really can't help them other than by convincing them to switch to
`GetMultipleUserDetails(all500userIds)`.

Rezoom lets you write little units of business logic called `Plan`s (like the
aforementioned `GetUserDetails`), which you can then glue together into larger
plans. It'll handle converting the 500 independent `GetUserDetails` calls into
one `GetMultipleUserDetails` call. It also de-duplicates redundant requests: for
example if multiple functions need to query for the current user's permissions,
only one such query will actually be executed.

# Show me an example so I know whether to care

Here's some contrived example code. You can write a Rezoom wrapper library for
any data source, but it comes with one called Rezoom.SQL that statically
typechecks SQL and infers its caching behavior.

```fsharp
type GetPerson = SQL<"select * from People where Id = @id">
type GetCompany = SQL<"select * from Companies where Id = @id">

/// Gets a person and their employer. This implementation takes 2 round trips.
let getPersonAndEmployer (personId : int) : (GetPerson.Row * GetCompany.Row) Plan =
    plan {
        // One round trip.
        let! person = GetPerson.Command(id = personId).ExactlyOne()
        // Another round trip.
        let! employer = GetCompany.Command(id = person.EmployerId).ExactlyOne()
        return (person, employer)
    }

let example =
    plan {
        // Two round-trips: one to get both users, another to get both employers.
        // If the two users have the same employer, the 2nd round-trip will only include one SELECT statement.
        let! (user1, employer1), (user2, employer2) =
            getPersonAndEmployer 1, getPersonAndEmployer 2
        printfn "%s is employed by %, %s by %s"
            user1.Name, employer1.Name
            user2.Name, employer2.Name

        // Two more round trips: again, the user queries all share one, and the employer queries share the next.
        for id in batch [ 3; 4; 5; 6 ] do
            let! user, employer = getPersonAndEmployer id
            printfn "%s is employed by %s" user.Name, employer.Name
    }

```

Pretty neat, huh? How about this?

```fsharp
type UpdateCompanies = SQL<"update Companies set LastUpdatedUtc = SysUtcDateTime() where Id = 27">

let example2 =
    plan {
        // Two round-trips: one for the user, one for the employer.
        let! (user1, employer1) =
            getPersonAndEmployer 1

        // Zero round trips, because both query results are cached.
        let! (user1Again, employer1Again) =
            getPersonAndEmployer 1

        // Invalidates the cache for all queries dependent on the Companies table.
        // This is automatically deduced by the SQL<"query string"> type provider.
        do! UpdateCompanies.Command().Plan()

        // One round-trip: user is still cached, but we have to reload the employer.
        let! (user1Again, employer1Again) =
            getPersonAndEmployer 1

        return 0
    }
```

"Now, hang on a minute," you might be asking your computer screen. "Where's this
stuff being cached? What if some other code updates a table and now the cache is
invalid?"

When you write a `plan` block nothing happens until you execute it with
`Execution.execute(plan)`. The cache is local to that execution, which is also
implicitly wrapped in a transaction. So, the cache is not for keeping
information across, say, multiple requests to your website.

It's to eliminate the temptation to explicitly pass around loaded data,
cluttering your API in an attempt to save queries.

The idea is to let you write very fine-grained APIs that take the bare minimum
of information (like `deleteSlide : SlideId -> unit Plan`) but can be composed
together without creating a performance nightmare.

