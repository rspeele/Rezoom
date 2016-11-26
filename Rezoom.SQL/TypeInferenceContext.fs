namespace Rezoom.SQL
open System
open System.Collections.Generic
open Rezoom.SQL
open Rezoom.SQL.InferredTypes

type private TypeInferenceVariable(variableId) =
    // uhoh gotta make nulls separate so when we unify @x and @y at the type level we don't force them to be the same nullability
    let infType =
        {   InfType = InfVariable variableId
            InfNullable = InfNullable.Of(variableId)
        }
    let mutable hasCurrentClass = false
    let mutable currentClass = AnyClass
    let mutable currentNullable = InfNullable.Nope
    let mutable aliasFor = None : TypeInferenceVariable option
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
    member this.Unify(source : SourceInfo, tvar : TypeInferenceVariable) =
        let unified = this.Unify(source, tvar.CurrentClass)
        tvar.BecomeAliasFor(this)
        this.Type

type private TypeInferenceContext() =
    let variablesByParameter = Dictionary<_, TypeInferenceVariable>()
    let variablesById = Dictionary<_, TypeInferenceVariable>()
    let mutable nextVariableId = 0
    let nextVar () =
        let tvar = TypeInferenceVariable(nextVariableId)
        variablesById.[nextVariableId] <- tvar
        nextVariableId <- nextVariableId + 1
        tvar
    let getVar id =
        let succ, inferred = variablesById.TryGetValue(id)
        if not succ then bug "Type variable not found"
        else inferred
    interface ITypeInferenceContext with
        member this.AnonymousVariable() = nextVar().Type
        member this.Variable(parameter) =
            let succ, found = variablesByParameter.TryGetValue(parameter)
            if succ then found.Type else
            let var = nextVar()
            variablesByParameter.[parameter] <- var
            var.Type
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