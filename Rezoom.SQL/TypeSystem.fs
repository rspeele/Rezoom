namespace Rezoom.SQL
open System

type CoreColumnType =
    | ObjectType
    | BooleanType
    | StringType
    | IntegerType of IntegerSize
    | FloatType of FloatSize
    | DecimalType
    | BinaryType
    | DateTimeType
    | DateTimeOffsetType
    override this.ToString() =
        match this with
        | ObjectType -> "OBJECT"
        | BooleanType -> "BOOL"
        | StringType -> "STRING"
        | IntegerType Integer8 -> "INT8"
        | IntegerType Integer16 -> "INT16"
        | IntegerType Integer32 -> "INT"
        | IntegerType Integer64 -> "INT64"
        | FloatType Float32 -> "FLOAT32"
        | FloatType Float64 -> "FLOAT64"
        | DecimalType -> "DECIMAL"
        | BinaryType -> "BINARY"
        | DateTimeType -> "DATETIME"
        | DateTimeOffsetType -> "DATETIMEOFFSET"
    static member OfTypeName(typeName : TypeName) =
        match typeName with
        | StringTypeName _ -> StringType
        | BinaryTypeName _ -> BinaryType
        | IntegerTypeName sz -> IntegerType sz
        | FloatTypeName sz -> FloatType sz
        | DecimalTypeName -> DecimalType
        | BooleanTypeName -> BooleanType
        | DateTimeTypeName -> DateTimeType
        | DateTimeOffsetTypeName -> DateTimeOffsetType

type ColumnType =
    {   Type : CoreColumnType
        Nullable : bool
    }
    static member OfTypeName(typeName : TypeName, nullable) =
        {   Type = CoreColumnType.OfTypeName(typeName)
            Nullable = nullable
        }
    member ty.CLRType =
        match ty.Type with
        | IntegerType Integer8 -> if ty.Nullable then typeof<Nullable<sbyte>> else typeof<sbyte>
        | IntegerType Integer16 -> if ty.Nullable then typeof<Nullable<int16>> else typeof<int16>
        | IntegerType Integer32 -> if ty.Nullable then typeof<Nullable<int32>> else typeof<int32>
        | IntegerType Integer64 -> if ty.Nullable then typeof<Nullable<int64>> else typeof<int64>
        | FloatType Float32 -> if ty.Nullable then typeof<Nullable<single>> else typeof<single>
        | FloatType Float64 -> if ty.Nullable then typeof<Nullable<double>> else typeof<double>
        | BooleanType -> if ty.Nullable then typeof<Nullable<bool>> else typeof<bool>
        | DecimalType -> if ty.Nullable then typeof<Nullable<decimal>> else typeof<decimal>
        | DateTimeType -> if ty.Nullable then typeof<Nullable<DateTime>> else typeof<DateTime>
        | DateTimeOffsetType -> if ty.Nullable then typeof<Nullable<DateTimeOffset>> else typeof<DateTimeOffset>
        | StringType -> typeof<string>
        | BinaryType -> typeof<byte array>
        | ObjectType -> typeof<obj>

type TypeClass =
    | AnyClass // base class for everything except lists
    | NumericClass // ints and floats
    | IntegeryClass // int sizes
    | FloatyClass // float sizes, decimal
    | StringyClass // varchar and varbinary
    | ListClass of element : TypeClass // paremeter lists e.g. WHERE u.Id in @legal_ids
    | ExactClass of columnType : CoreColumnType // a specific column type
    member this.HasParentClass = this.ParentClass <> this
    member this.ParentClass =
        match this with
        | AnyClass
        | StringyClass
        | NumericClass -> AnyClass
        | IntegeryClass
        | FloatyClass -> NumericClass
        | ListClass element -> ListClass (element.ParentClass)
        | ExactClass columnType ->
            match columnType with
            | BinaryType
            | StringType -> StringyClass
            | DecimalType -> FloatyClass
            | ObjectType
            | BooleanType
            | DateTimeType
            | DateTimeOffsetType -> AnyClass
            | IntegerType Integer8 -> ExactClass (IntegerType Integer16)
            | IntegerType Integer16 -> ExactClass (IntegerType Integer32)
            | IntegerType Integer32 -> ExactClass (IntegerType Integer64)
            | IntegerType Integer64 -> IntegeryClass
            | FloatType Float32 -> ExactClass (FloatType Float64)
            | FloatType Float64 -> FloatyClass
    member this.ParentClasses =
        seq {
            let mutable previous = this
            let mutable parent = this.ParentClass
            while previous <> parent do
                yield parent
                previous <- parent
                parent <- parent.ParentClass
        }
    member private this.CalculateDepth(acc) =
        match this with
        | AnyClass
        | ListClass AnyClass -> acc
        | other -> other.ParentClass.CalculateDepth(acc + 1)
    member private this.Depth = this.CalculateDepth(0)
    /// If this is a base class of other or vice versa, return Some (whichever one was the base).
    /// Otherwise return None.
    member this.Unify(other : TypeClass) =
        if this = other then
            Some this
        else
            if this.ParentClasses |> Seq.contains other then Some other
            elif other.ParentClasses |> Seq.contains this then Some this
            else None

type TypeVariableId = int

type CoreInfType =
    | InfClass of TypeClass
    | InfVariable of TypeVariableId

type InfNullable =
    | UnknownNullable
    | KnownNullable of bool
    | NullableIfNullable of TypeVariableId
    | NullableIfBoth of InfNullable * InfNullable
    | NullableIfEither of InfNullable * InfNullable

type InferredType =
    {   InfType : CoreInfType
        InfNullable : InfNullable
    }

type ITypeInferenceContext =
    abstract member AnonymousVariable : unit -> InferredType
    abstract member Variable : BindParameter -> InferredType
    /// Unify the two types (ensure they are compatible and add constraints)
    /// and produce the most specific type.
    abstract member Unify : InferredType * InferredType -> Result<InferredType, string>
    abstract member Concrete : InferredType -> ColumnType
    abstract member Parameters : BindParameter seq

type InfInputType =
    {   TypeVariable : Name option
        Class : TypeClass
        Nullable : InfNullable
    }

type InfOutputType =
    {   TypeVariable : Name option
        Class : TypeClass
        Nullable : InfNullable seq -> InfNullable // Determine nullability given that of inputs.
    }

type InfVariableArgument =
    {   /// Maximum # of times this argument can be supplied (no limit if None).
        MaxCount : int option
        Type : InfInputType
    }

type AggregateType =
    {   AllowWildcard : bool
        AllowDistinct : bool
    }

type FunctionType =
    {   FunctionName : Name
        FixedArguments : InfInputType array
        VariableArgument : InfVariableArgument option
        Output : InfOutputType
        Aggregate : FunctionArguments<unit, unit> -> AggregateType option
        Idempotent : bool
    }


