#if NETSTANDARD2_0
// `AsyncMethodBuilderAttribute` was added to the BCL in .NET Standard 2.1 / .NET 6.
// Defining it here lets us target netstandard2.0 without the consumer needing
// to reference System.Threading.Tasks.Extensions or similar.
namespace System.Runtime.CompilerServices

open System

[<AttributeUsage
    ( AttributeTargets.Class ||| AttributeTargets.Struct
      ||| AttributeTargets.Interface ||| AttributeTargets.Delegate
      ||| AttributeTargets.Enum ||| AttributeTargets.Method
    , Inherited = false
    , AllowMultiple = false)>]
type internal AsyncMethodBuilderAttribute(builderType : Type) =
    inherit Attribute()
    member _.BuilderType = builderType
#else
namespace Rezoom.Internal
// Placeholder so this file produces something on non-netstandard2.0 targets.
module internal Polyfills = ()
#endif
