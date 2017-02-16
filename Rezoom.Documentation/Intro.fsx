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

Rezoom is a library intended to help you deal with data that lives somewhere else.

Between database servers, web APIs, and specialized protocols like SNMP, data has an annoying habit of
hanging out on other machines and waiting for us to request it. Unfortunately, while programmers can
afford to be inefficient with memory and CPU, we still have to count network round trips on our fingers.

*)
