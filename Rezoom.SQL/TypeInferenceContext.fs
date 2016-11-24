namespace Rezoom.SQL
open System
open System.Collections.Generic
open Rezoom.SQL
open Rezoom.SQL.InferredTypes

//type private TypeInferenceContext() =
//    let variablesByParameter = Dictionary<BindParameter, InferredType>()
//    let variablesById = Dictionary<TypeVariableId, InferredType>()
//    let mutable nextVariableId = 0
//    let getVar id =
//        let succ, inferred = variablesById.TryGetValue(id)
//        if not succ then bug "Type variable not found"
//        else inferred
//    interface ITypeInferenceContext with
//        member this.AnonymousVariable() = failwith "not impl"
//        member this.Variable(parameter) = failwith "not impl"
//        member this.Unify(source, left, right) = failwith "not impl"
//        member this.Concrete(inferred) = failwith "not impl"
//        member __.Parameters = variablesByParameter.Keys :> _ seq
// 
//[<AutoOpen>]
//module private TypeInferenceExtensions =
//    type ITypeInferenceContext with
//        member typeInference.Unify(inferredType, coreType : CoreColumnType) =
//            typeInference.Unify(inferredType, InferredType.Dependent(inferredType, coreType))
//        member typeInference.Unify(inferredType, resultType : Result<InferredType, string>) =
//            match resultType with
//            | Ok t -> typeInference.Unify(inferredType, t)
//            | Error _ as e -> e
//        member typeInference.Unify(types : InferredType seq) =
//            types
//            |> Seq.fold
//                (function | Ok s -> (fun t -> typeInference.Unify(s, t)) | Error _ as e -> (fun _ -> e))
//                (Ok InferredType.Any)
//        member typeInference.Concrete(inferred) = typeInference.Concrete(inferred)
//        member typeInference.Binary(op, left, right) =
//            match op with
//            | Concatenate -> typeInference.Unify([ left; right; InferredType.String ])
//            | Multiply
//            | Divide
//            | Add
//            | Subtract -> typeInference.Unify([ left; right; InferredType.Number ])
//            | Modulo
//            | BitShiftLeft
//            | BitShiftRight
//            | BitAnd
//            | BitOr -> typeInference.Unify([ left; right; InferredType.Integer ])
//            | LessThan
//            | LessThanOrEqual
//            | GreaterThan
//            | GreaterThanOrEqual
//            | Equal
//            | NotEqual
//            | Is
//            | IsNot ->
//                result {
//                    let! operandType = typeInference.Unify(left, right)
//                    return InferredType.Dependent(operandType, BooleanType)
//                }
//            | And
//            | Or -> typeInference.Unify([ left; right; InferredType.Boolean ])
//        member typeInference.Unary(op, operandType) =
//            match op with
//            | Negative
//            | BitNot -> typeInference.Unify(operandType, InferredType.Number)
//            | Not -> typeInference.Unify(operandType, InferredType.Boolean)
//            | IsNull
//            | NotNull -> result { return InferredType.Boolean }
//        member typeInference.AnonymousQueryInfo(columnNames) =
//            {   Columns =
//                    seq {
//                        for { WithSource.Source = source; Value = name } in columnNames ->
//                            {   ColumnName = name
//                                FromAlias = None
//                                Expr =
//                                    {   Value = ColumnNameExpr { Table = None; ColumnName = name }
//                                        Source = source
//                                        Info = ExprInfo.OfType(typeInference.AnonymousVariable())
//                                    }
//                            }
//                    } |> toReadOnlyList
//            }
//
//    let inline implicitAlias column =
//        match column with
//        | _, (Some _ as a) -> a
//        | ColumnNameExpr c, None -> Some c.ColumnName
//        | _ -> None
//
