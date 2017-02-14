(*** hide ***)

#r "../Rezoom.SQL.Provider/bin/Debug/Rezoom.dll"
#r "../Rezoom.SQL.Provider/bin/Debug/LicenseToCIL.dll"
#r "../Rezoom.SQL.Provider/bin/Debug/Rezoom.SQL.Compiler.dll"
#r "../Rezoom.SQL.Provider/bin/Debug/Rezoom.SQL.Mapping.dll"
#r "../Rezoom.SQL.Provider/bin/Debug/Rezoom.SQL.Provider.dll"
#nowarn "193"
open System
open Rezoom
open Rezoom.SQL
open Rezoom.SQL.Provider

(**

# Rezoom: a resumption monad for .NET data access.

Rezoom is a library intended to help you code an API for manipulating your application's data.
An API that doesn't fall down when you try to compose its functions.

*)
