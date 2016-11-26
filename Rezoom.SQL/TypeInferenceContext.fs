namespace Rezoom.SQL
open System
open System.Collections.Generic
open Rezoom.SQL
open Rezoom.SQL.InferredTypes

type private NVariable(variableId : TypeVariableId) =
    let mutable currentNullable = InfNullable.Nope
    let infNull = InfNullable.Of(variableId)
    member __.VariableId = variableId
    member __.Type = infNull
    member __.Unify(source : SourceInfo, cla : InfNullable) =
        match cla with
        | Nullable true as definitely ->
            currentNullable <- definitely
        | Nullable false -> ()
        | NullableDueToOuterJoin wrapped -> ()
        | NullableIfVar of TypeVariableId
        | NullableIfBoth of InfNullable * InfNullable
        | NullableIfEither of InfNullable * InfNullable

type private TVariable(variableId) =
    let infType = InfVariable variableId
    let mutable hasCurrentClass = false
    let mutable currentClass = AnyClass
    let mutable aliasFor = None : TVariable option
    member __.VariableId =
        match aliasFor with
        | Some tvar -> tvar.VariableId
        | None -> variableId
    member __.Type =
        match aliasFor with
        | Some tvar -> tvar.Type
        | None -> infType
    member this.Unify(source : SourceInfo, cla : TypeClass) =
        match aliasFor with
        | Some tvar -> tvar.Unify(source, cla)
        | None ->
            if hasCurrentClass then
                match currentClass.Unify(cla) with
                | None ->
                    failAt source <| sprintf "The type variable cannot be unified with ``%O``" cla
                | Some unified ->
                    currentClass <- unified
            else
                hasCurrentClass <- true
                currentClass <- cla
            this.Type
    member private this.CurrentClass =
        match aliasFor with
        | Some tvar -> tvar.CurrentClass
        | None -> currentClass
    member private this.BecomeAliasFor(other) =
        aliasFor <- Some other
    member this.Unify(source : SourceInfo, tvar : TVariable) =
        let unified = this.Unify(source, tvar.CurrentClass)
        tvar.BecomeAliasFor(this)
        this.Type

type VariableLookup<'a, 'b>(create, get : 'a -> 'b) =
    let variablesByParameter = Dictionary<_, 'a>()
    let variablesById = Dictionary<_, 'a>()
    let mutable nextVariableId = 0
    let nextVar () =
        let tvar = create nextVariableId
        variablesById.[nextVariableId] <- tvar
        nextVariableId <- nextVariableId + 1
        tvar
    let getVar id =
        let succ, inferred = variablesById.TryGetValue(id)
        if not succ then bug "Type variable not found"
        else inferred
    member __.AnonymousVariable() = get(nextVar())
    member __.Variable(parameter : BindParameter) =
        let succ, found = variablesByParameter.TryGetValue(parameter)
        if succ then get found else
        let var = nextVar()
        variablesByParameter.[parameter] <- var
        get var

type private TypeInferenceContext() =
    let nvars = VariableLookup(NVariable, fun x -> x.Type)
    let tvars = VariableLookup(TVariable, fun x -> x.Type)
    interface ITypeInferenceContext with
        member this.AnonymousTypeVariable() = tvars.AnonymousVariable()
        member this.TypeVariable(parameter) = tvars.Variable(parameter)
        member this.AnonymousNullableVariable() = nvars.AnonymousVariable()
        member this.NullableVariable(parameter) = nvars.Variable(parameter)
        member this.UnifyTypes(source, left, right) =
            match left, right with
            | InfClass lc, InfClass rc ->
                match lc.Unify(rc) with
                | None -> failAt source <| sprintf "The types ``%O`` and ``%O`` cannot be unified" lc rc
                | Some unified -> InfClass unified
            | InfClass cla, InfVariable vid
            | InfVariable vid, InfClass cla ->
                variablesById.[vid].Unify(source, cla).InfType
            | InfVariable lid, InfVariable rid ->
                variablesById.[lid].Unify(source, variablesById.[rid]).InfType

        member this.InfectNullable(source, left, right) =
            match left, right with
            | Nullable _, Nullable _ -> ()
            | NullableIfVar lvar, NullableIfVar rvar ->
                ()
            | 
                

        member this.Concrete(inferred) = failwith "not impl"
        member __.Parameters = variablesByParameter.Keys :> _ seq

//    abstract member AnonymousVariable : unit -> InferredType
//    abstract member Variable : BindParameter -> InferredType
//    abstract member UnifyTypes : SourceInfo * CoreInfType * CoreInfType -> CoreInfType
//    abstract member InfectNullable : SourceInfo * InfNullable * InfNullable -> unit
//    abstract member Concrete : InferredType -> ColumnType
//    abstract member Parameters : BindParameter seq