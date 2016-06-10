namespace Data.Resumption.Test
open Data.Resumption
open System
open System.Text
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type TestDataSeq() =
    [<TestMethod>]
    member __.TestSimpleIteration() =
        let sequence =
            dataseq {
                yield "x"
                yield "y"
                yield "z"
            }
        {
            Task = fun () ->
                datatask {
                    for query in sequence do
                        let! q = send query
                        printfn "%s" q
                    return () // we have to have this return because for over dataseqs is strict
                }
            Batches =
                [
                    ["x"]
                    ["y"]
                    ["z"]
                ]
            Result = Good ()
        } |> test

    [<TestMethod>]
    member __.TestQueryIteration() =
        let sequence =
            dataseq {
                let! x = send "x"
                yield x
                let! y = send "y"
                yield y
                let! z = send "z"
                yield z
            }
        {
            Task = fun () ->
                datatask {
                    let builder = new StringBuilder()
                    for result in sequence do
                        ignore <| builder.Append(result)
                    return builder.ToString()
                }
            Batches =
                [
                    ["x"]
                    ["y"]
                    ["z"]
                ]
            Result = Good "xyz"
        } |> test
