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

    [<TestMethod>]
    member __.TestInfiniteTakeN() =
        let infinite =
            dataseq {
                for i in Seq.initInfinite id do
                    let! echo = send (string i)
                    yield echo
            }
        {
            Task = fun () ->
                datatask {
                    let! xs = infinite |> DataSeq.map int |> DataSeq.truncate 5 |> DataSeq.toList
                    return Set.ofSeq xs
                }
            Batches =
                [
                    ["0"]
                    ["1"]
                    ["2"]
                    ["3"]
                    ["4"]
                ]
            Result = Good (Set.ofList [0..4])
        } |> test

    [<TestMethod>]
    member __.TestInfiniteTakeWhile() =
        let infinite =
            dataseq {
                for i in Seq.initInfinite id do
                    let! echo = send (string i)
                    yield echo
            }
        {
            Task = fun () ->
                datatask {
                    let! xs = infinite |> DataSeq.map int |> DataSeq.takeWhile (fun x -> x < 4) |> DataSeq.toList
                    return Set.ofSeq xs
                }
            Batches =
                [
                    ["0"]
                    ["1"]
                    ["2"]
                    ["3"]
                    ["4"] // we have to load it to know that it ends the sequence
                ]
            Result = Good (Set.ofList [0..3])
        } |> test
