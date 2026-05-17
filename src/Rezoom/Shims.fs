#if NETSTANDARD2_0
// `AsyncMethodBuilderAttribute` was added to the BCL in .NET Standard 2.1 / .NET 6.
// Defining it here lets us target netstandard2.0 without the consumer needing
// to reference System.Threading.Tasks.Extensions or similar.
// Roslyn resolves the attribute by name so this still works for the netstandard2.0
// consumer as long as their compiler version is up to date.
// On net8.0+ this file compiles to nothing and the BCL's
// real attribute is used; the two never coexist in the same assembly.
namespace System.Runtime.CompilerServices

open System

[<AttributeUsage
    ( AttributeTargets.Class ||| AttributeTargets.Struct
      ||| AttributeTargets.Interface ||| AttributeTargets.Delegate
      ||| AttributeTargets.Enum ||| AttributeTargets.Method
    , Inherited = false
    , AllowMultiple = false)>]
type AsyncMethodBuilderAttribute(builderType : Type) =
    inherit Attribute()
    member _.BuilderType = builderType
#else
namespace Rezoom.Internal
// Placeholder so this file produces something on non-netstandard2.0 targets.
module internal Shims = ()
#endif
