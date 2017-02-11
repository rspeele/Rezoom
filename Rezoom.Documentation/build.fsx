#load "../packages/FSharp.Formatting.2.14.4/FSharp.Formatting.fsx"
open FSharp.Literate
open System.IO

let source = __SOURCE_DIRECTORY__
let template = Path.Combine(source, "template.html")

let files =
    [   "Intro.fsx"
    ]

let replacements =
    [   "project-name", "Rezoom"
        "github-link", "https://github.com/rspeele/Rezoom"
    ]

for file in files do
    Literate.ProcessScriptFile
        ( Path.Combine(source, file), template, lineNumbers = false
        , replacements = replacements
        )