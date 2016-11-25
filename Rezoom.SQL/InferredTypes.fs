module Rezoom.SQL.InferredTypes
open System
open System.Collections.Generic
open Rezoom.SQL

type InfExprType = ExprType<InferredType ObjectInfo, InferredType ExprInfo>
type InfExpr = Expr<InferredType ObjectInfo, InferredType ExprInfo>
type InfInExpr = InExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfCollationExpr = CollationExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfBetweenExpr = BetweenExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfSimilarityExpr = SimilarityExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfBinaryExpr = BinaryExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfUnaryExpr = UnaryExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfObjectName = ObjectName<InferredType ObjectInfo>
type InfColumnName = ColumnName<InferredType ObjectInfo>
type InfInSet = InSet<InferredType ObjectInfo, InferredType ExprInfo>
type InfCaseExpr = CaseExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfCastExpr = CastExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfFunctionInvocationExpr = FunctionInvocationExpr<InferredType ObjectInfo, InferredType ExprInfo>   
type InfWithClause = WithClause<InferredType ObjectInfo, InferredType ExprInfo>
type InfCommonTableExpression = CommonTableExpression<InferredType ObjectInfo, InferredType ExprInfo>
type InfCompoundExprCore = CompoundExprCore<InferredType ObjectInfo, InferredType ExprInfo>
type InfCompoundExpr = CompoundExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfCompoundTermCore = CompoundTermCore<InferredType ObjectInfo, InferredType ExprInfo>
type InfCompoundTerm = CompoundTerm<InferredType ObjectInfo, InferredType ExprInfo>
type InfCreateTableDefinition = CreateTableDefinition<InferredType ObjectInfo, InferredType ExprInfo>
type InfCreateTableStmt = CreateTableStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfSelectCore = SelectCore<InferredType ObjectInfo, InferredType ExprInfo>
type InfJoinConstraint = JoinConstraint<InferredType ObjectInfo, InferredType ExprInfo>
type InfJoin = Join<InferredType ObjectInfo, InferredType ExprInfo>
type InfLimit = Limit<InferredType ObjectInfo, InferredType ExprInfo>
type InfGroupBy = GroupBy<InferredType ObjectInfo, InferredType ExprInfo>
type InfOrderingTerm = OrderingTerm<InferredType ObjectInfo, InferredType ExprInfo>
type InfResultColumn = ResultColumn<InferredType ObjectInfo, InferredType ExprInfo>
type InfResultColumns = ResultColumns<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableOrSubquery = TableOrSubquery<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableExprCore = TableExprCore<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableExpr = TableExpr<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableInvocation = TableInvocation<InferredType ObjectInfo, InferredType ExprInfo>
type InfSelectStmt = SelectStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfColumnConstraint = ColumnConstraint<InferredType ObjectInfo, InferredType ExprInfo>
type InfColumnDef = ColumnDef<InferredType ObjectInfo, InferredType ExprInfo>
type InfAlterTableStmt = AlterTableStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfAlterTableAlteration = AlterTableAlteration<InferredType ObjectInfo, InferredType ExprInfo>
type InfCreateIndexStmt = CreateIndexStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableIndexConstraintClause = TableIndexConstraintClause<InferredType ObjectInfo, InferredType ExprInfo>
type InfTableConstraint = TableConstraint<InferredType ObjectInfo, InferredType ExprInfo>
type InfCreateViewStmt = CreateViewStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfQualifiedTableName = QualifiedTableName<InferredType ObjectInfo>
type InfDeleteStmt = DeleteStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfDropObjectStmt = DropObjectStmt<InferredType ObjectInfo>
type InfUpdateStmt = UpdateStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfInsertStmt = InsertStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfStmt = Stmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfVendorStmt = VendorStmt<InferredType ObjectInfo, InferredType ExprInfo>
type InfTotalStmt = TotalStmt<InferredType ObjectInfo, InferredType ExprInfo>

let ofClass clas =
    {   InfType = InfClass clas
        InfNullable = InfNullable.Nope
    }

let ofCoreType (coreType : CoreColumnType) = ofClass (ExactClass coreType)

let any = ofClass AnyClass

let numeric = ofClass NumericClass

let inty = ofClass IntegeryClass

let stringy = ofClass StringyClass

let string = ofCoreType StringType

let boolean = ofCoreType BooleanType

let ofNull =
    {   InfType = InfClass AnyClass
        InfNullable = InfNullable.Yep
    }

let ofColumnType (columnType : ColumnType) =
    {   InfType = InfClass (ExactClass columnType.Type)
        InfNullable = Nullable columnType.Nullable
    }

let ofTypeName (typeName : TypeName) =
    ofClass (ExactClass (CoreColumnType.OfTypeName(typeName)))

let nullIf (ifTy : InferredType) thenTy =
    { thenTy with InfNullable = ifTy.InfNullable }

let ofLiteral (literal : Literal) =
    match literal with
    | NullLiteral -> ofNull
    | BooleanLiteral _ -> ofCoreType BooleanType
    | StringLiteral _ -> ofCoreType StringType
    | BlobLiteral _ -> ofCoreType BinaryType
    | DateTimeLiteral _ -> ofCoreType DateTimeType
    | DateTimeOffsetLiteral _ -> ofCoreType DateTimeOffsetType
    | NumericLiteral lit ->
        let cla =
            match lit with
            | IntegerLiteral _ -> IntegeryClass
            | FloatLiteral _ -> FloatyClass
        {   InfType = InfClass cla
            InfNullable = InfNullable.Nope
        }

type ITypeInferenceContext with
    member this.UnifyTypes(source : SourceInfo, types : CoreInfType seq) =
        let mutable unified = InfClass AnyClass
        for ty in types do
            unified <- this.UnifyTypes(source, unified, ty)
        unified
    member this.UnifyWithConstraint(source : SourceInfo, inputType, constr : TypeClass) =
        {   InfType = this.UnifyTypes(source, inputType.InfType, InfClass constr)
            InfNullable = inputType.InfNullable
        }
    member this.UnifyEitherNull(source : SourceInfo, left, right) =
        {   InfType = this.UnifyTypes(source, left.InfType, right.InfType)
            InfNullable = left.InfNullable.Or(right.InfNullable)
        }
    member this.UnifyEitherNull(source : SourceInfo, left, right, knownType : CoreInfType) =
        {   InfType =
                this.UnifyTypes(source, this.UnifyTypes(source, left.InfType, right.InfType), knownType)
            InfNullable = left.InfNullable.Or(right.InfNullable)
        }

let binary
    (source : SourceInfo)
    (op : BinaryOperator)
    (left : InferredType)
    (right : InferredType)
    (cxt : ITypeInferenceContext) =
    match op with
    | Concatenate -> cxt.UnifyEitherNull(source, left, right, string.InfType)
    | Multiply
    | Divide
    | Add
    | Subtract -> cxt.UnifyEitherNull(source, left, right, numeric.InfType)
    | Modulo
    | BitShiftLeft
    | BitShiftRight
    | BitAnd
    | BitOr -> cxt.UnifyEitherNull(source, left, right, inty.InfType)
    | LessThan
    | LessThanOrEqual
    | GreaterThan
    | GreaterThanOrEqual
    | Equal
    | NotEqual ->
        let unified = cxt.UnifyEitherNull(source, left, right)
        {   InfType = boolean.InfType
            InfNullable = unified.InfNullable
        }
    | Is
    | IsNot ->
        let unified = cxt.UnifyEitherNull(source, left, right)
        cxt.InfectNullable(source, left.InfNullable, right.InfNullable) // IS operators push back nullability
        unified
    | And
    | Or -> cxt.UnifyEitherNull(source, left, right, boolean.InfType)

let unary
    (source : SourceInfo)
    (op : UnaryOperator)
    (operand : InferredType)
    (cxt : ITypeInferenceContext) =
    match op with
    | Negative
    | BitNot -> cxt.UnifyWithConstraint(source, operand, NumericClass)
    | Not -> cxt.UnifyWithConstraint(source, operand, ExactClass BooleanType)
    | IsNull
    | NotNull ->
        cxt.InfectNullable(source, operand.InfNullable, InfNullable.Yep)
        {   InfType = boolean.InfType
            InfNullable = InfNullable.Yep
        }

let private funcArgTypes (func : FunctionType) =
    seq {
        for arg in func.FixedArguments ->
            arg
        match func.VariableArgument with
        | None -> ()
        | Some varArg ->
            while true do
                yield varArg.Type
    }

let func
    (source : SourceInfo)
    (func : FunctionType)
    (invoc : InfFunctionInvocationExpr)
    (cxt : ITypeInferenceContext) =
    let aggregate =
        match invoc.Arguments with
        | ArgumentWildcard -> func.Aggregate None
        | ArgumentList (_, args) -> func.Aggregate (Some args.Length)
    let byName = Dictionary()
    let infFrom (source : SourceInfo) (cla : TypeClass) (tvar : Name option) =
        match tvar with
        | None ->
            InfClass cla
        | Some tvar ->
            let existing =
                let succ, existing = byName.TryGetValue(tvar)
                if succ then existing else
                let tid = cxt.AnonymousVariable().InfType
                byName.[tvar] <- tid
                tid
            cxt.UnifyTypes(source, InfClass cla, existing)
    match invoc.Arguments, aggregate with
    | ArgumentWildcard, None ->
        failAt source "Non-aggregate function cannot permit wildcard"
    | ArgumentWildcard, Some agg ->
        if not agg.AllowWildcard then
            failAt source <| sprintf "Aggregate function ``%O`` does not permit wildcard" func.FunctionName
        else
            let out = func.Output
            {   InfType = infFrom source out.Class out.TypeVariable
                InfNullable = out.Nullable Seq.empty
            }
    | ArgumentList (distinct, args), aggregate ->
        match distinct, aggregate with
        | None, _ -> ()
        | Some _, None ->
            failAt source <| sprintf "Non-aggregate function cannot permit DISTINCT keyword"
        | Some _, Some aggregate ->
            if not aggregate.AllowDistinct then
                failAt source <| sprintf "Aggregate function ``%O`` does not permit DISTINCT keyword" func.FunctionName
        if args.Length < func.FixedArguments.Length then
            failAt ((Array.last args).Source) <|
                sprintf "The function ``%O`` requires at least %d arguments, but was given only %d"
                    func.FunctionName func.FixedArguments.Length args.Length
        let maxArguments =
            match func.VariableArgument with
            | None -> Some func.FixedArguments.Length
            | Some { MaxCount = Some max } -> Some (func.FixedArguments.Length + max)
            | Some _ -> None
        match maxArguments with
        | Some maxArgs when maxArgs < args.Length ->
            failAt ((Array.last args).Source) <|
                sprintf "The function ``%O`` accepts at most %d arguments, but was given %d"
                    func.FunctionName maxArgs args.Length
        | _ -> ()
        let infTypeOf source (inputTy : InfInputType) =
            let core = infFrom source inputTy.Class inputTy.TypeVariable
            {   InfType = core
                InfNullable = inputTy.Nullable
            }
        for argType, arg in Seq.zip (funcArgTypes func) args do
            let passedType = arg.Info.Type
            let expectedType = infTypeOf arg.Source argType
            ignore <| cxt.InfectNullable(arg.Source, expectedType.InfNullable, passedType.InfNullable)
            ignore <| cxt.UnifyTypes(arg.Source, expectedType.InfType, passedType.InfType)
        let out = func.Output
        {   InfType = infFrom source out.Class out.TypeVariable
            InfNullable = out.Nullable (args |> Seq.map (fun a -> a.Info.Type.InfNullable))
        }   

type InferredQueryColumn() =
    static member OfColumn(fromAlias : Name option, column : SchemaColumn) =
        {   Expr =
                {   Source = SourceInfo.Invalid
                    Info = { ExprInfo<_>.OfType(ofColumnType column.ColumnType) with Column = Some column }
                    Value = ColumnNameExpr { ColumnName = column.ColumnName; Table = None }
                }
            ColumnName = column.ColumnName
            FromAlias = fromAlias
        }

let foundAt source nameResolution =
    match nameResolution with
    | Found x -> x
    | NotFound err
    | Ambiguous err -> failAt source err

let inferredOfTable (table : SchemaTable) =
    {   Columns =
            table.Columns
            |> Seq.map (function KeyValue(_, c) -> InferredQueryColumn.OfColumn(Some table.TableName, c))
            |> toReadOnlyList
    }

let inferredOfView (view : SchemaView) =
    let concreteQuery = view.Definition.Value.Info.Query
    concreteQuery.Map(ofColumnType)

type InferredFromClause =
    {   /// The tables named in the "from" clause of the query, if any.
        /// These are keyed on the alias of the table, if any, or the table name.
        FromVariables : IReadOnlyDictionary<Name, InferredType ObjectInfo>
        /// All the objects involved in the from clause in order.
        FromObjects : (Name * InferredType ObjectInfo) seq
    }
    static member FromSingleObject(tableName : InfObjectName) =
        {   FromVariables = Dictionary() :> IReadOnlyDictionary<_, _>
            FromObjects = (Name(""), tableName.Info) |> Seq.singleton
        }
    member this.ResolveTable(tableName : ObjectName) =
        match tableName.SchemaName with
        // We don't currently support referencing columns like "main.users.id". Use table aliases instead!
        | Some schemaName -> Ambiguous <| sprintf "Unsupported schema name in column reference: ``%O``" tableName
        | None ->
            let succ, query = this.FromVariables.TryGetValue(tableName.ObjectName)
            if succ then Found query
            else NotFound <| sprintf "No such table in FROM clause: ``%O``" tableName.ObjectName
    member this.ResolveColumnReference(name : ColumnName) =
        match name.Table with
        | None ->
            let matches =
                seq {
                    for tableAlias, objectInfo in this.FromObjects do
                        let table = objectInfo.Table
                        match table.Query.ColumnByName(name.ColumnName) with
                        | Found column ->
                            yield Ok ((if tableAlias.Value = "" then None else Some tableAlias), table, column)
                        | NotFound _ -> ()
                        | Ambiguous err -> yield Error err
                } |> toReadOnlyList
            if matches.Count = 1 then
                match matches.[0] with
                | Ok triple -> Found triple
                | Error e -> Ambiguous e
            elif matches.Count <= 0 then
                NotFound <| sprintf "No such column in FROM clause: ``%O``" name
            else
                Ambiguous <| sprintf "Ambiguous column: ``%O``" name
        | Some tableName ->
            match this.ResolveTable(tableName) with
            | Found objectInfo ->
                let table = objectInfo.Table
                match table.Query.ColumnByName(name.ColumnName) with
                | Found column -> Found (Some tableName.ObjectName, table, column)
                | NotFound err -> NotFound err
                | Ambiguous err -> Ambiguous err
            | NotFound err -> NotFound err
            | Ambiguous err -> Ambiguous err

and InferredSelectScope =
    {   /// If this scope is that of a subquery, the parent query's scope can also be used
        /// to resolve column and CTE names.
        ParentScope : InferredSelectScope option
        /// The model this select is running against.
        /// This includes tables and views that are part of the database, and may be used to resolve
        /// table names in the "from" clause of the query.
        Model : Model
        /// Any CTEs defined by the query.
        /// These may be referenced in the "from" clause of the query.
        CTEVariables : Map<Name, InferredType QueryExprInfo>
        FromClause : InferredFromClause option
        SelectClause : InferredType QueryExprInfo option
    }

    static member Root(model) =
        {   ParentScope = None
            Model = model
            CTEVariables = Map.empty
            FromClause = None
            SelectClause = None
        }

    member private this.ResolveObjectReferenceBySchema(schema : Schema, name : Name) =
        match schema.Objects |> Map.tryFind name with
        | Some (SchemaTable tbl) ->
            { Table = TableReference tbl; Query = inferredOfTable(tbl) } |> TableLike |> Found
        | Some (SchemaView view) ->
            { Table = ViewReference view; Query = inferredOfView(view) } |> TableLike |> Found
        | None -> NotFound <| sprintf "No such table in schema %O: ``%O``" schema.SchemaName name

    /// Resolve a reference to a table which may occur as part of a TableExpr.
    /// This will resolve against the database model and CTEs, but not table aliases defined in the FROM clause.
    member this.ResolveObjectReference(name : ObjectName) =
        match name.SchemaName with
        | None ->
            match this.CTEVariables.TryFind(name.ObjectName) with
            | Some cte -> { Table = CTEReference name.ObjectName; Query = cte } |> TableLike |> Found
            | None ->
                match this.ParentScope with
                | Some parent ->
                    parent.ResolveObjectReference(name)
                | None ->
                    let schema = this.Model.Schemas.[this.Model.DefaultSchema]
                    this.ResolveObjectReferenceBySchema(schema, name.ObjectName)
        | Some schema ->
            let schema = this.Model.Schemas.[schema]
            this.ResolveObjectReferenceBySchema(schema, name.ObjectName)

    /// Resolve a column reference, which may be qualified with a table alias.
    /// This resolves against the tables referenced in the FROM clause, and the columns explicitly named
    /// in the SELECT clause, if any.
    member this.ResolveColumnReference(name : ColumnName) =
        let findFrom() =
            let thisLevel =
                match this.FromClause with
                | None ->
                    NotFound <| sprintf "Cannot reference column name ``%O`` in query without a FROM clause" name
                | Some fromClause ->
                    fromClause.ResolveColumnReference(name)
            match this.ParentScope, thisLevel with
            | Some parent, NotFound _ ->
                parent.ResolveColumnReference(name)
            | _ -> thisLevel
        match name.Table, this.SelectClause with
        | None, Some selected ->
            match selected.ColumnByName(name.ColumnName) with
            | Found column ->
                Found (None, { Table = SelectClauseReference name.ColumnName; Query = selected }, column)
            | Ambiguous reason -> Ambiguous reason
            | NotFound _ -> findFrom()
        | _ -> findFrom()

let concreteMapping (inference : ITypeInferenceContext) =
    ASTMapping<InferredType ObjectInfo, InferredType ExprInfo, _, _>
        ((fun t -> t.Map(inference.Concrete)), fun e -> e.Map(inference.Concrete))