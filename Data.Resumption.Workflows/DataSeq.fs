/// Provides an F# compatibility layer over the extension methods in Data.Resumption.DataEnumerable.
/// This module is for methods that correspond to methods in the `Seq` module.
module Data.Resumption.DataSeq
open Data.Resumption
open System

let truncate count seq =
    DataEnumerable.Take(seq, count)

let takeWhile (predicate : _ -> bool) seq =
    DataEnumerable.TakeWhile(seq, Func<_, bool>(predicate))

let takeWhileAsync (predicate : _ -> bool datatask) seq =
    DataEnumerable.TakeWhile(seq, Func<_, bool datatask>(predicate))

let map (mapping : _ -> _) seq =
    DataEnumerable.Select(seq, Func<_, _>(mapping))

let concat (seqs : _ dataseq dataseq) =
    DataEnumerable.SelectMany(seqs, id)

let collect (mapping : 'a -> 'b seq) (seqs : 'a dataseq) =
    DataEnumerable.SelectMany(seqs, Func<_, _>(mapping))

let collectAsync (mapping : 'a -> 'b dataseq) (seqs : 'a dataseq) =
    DataEnumerable.SelectMany(seqs, Func<_, _>(mapping))

let filter (predicate : _ -> bool) seq =
    DataEnumerable.Where(seq, Func<_, bool>(predicate))

let filterAsync (predicate : _ -> bool datatask) seq =
    DataEnumerable.Where(seq, Func<_, bool datatask>(predicate))

let fold (folder : 'acc -> 'a -> 'acc) initial seq =
    DataTask.Aggregate(seq, initial, Func<_, _, _>(folder))

let toList (seq : _ dataseq) = DataTask.ToList(seq)
let toFsList (seq : _ dataseq) = seq |> toList |> DataTaskMonad.map Seq.toList
let toArray (seq : _ dataseq) = seq |> toList |> DataTaskMonad.map (fun lst -> lst.ToArray())
