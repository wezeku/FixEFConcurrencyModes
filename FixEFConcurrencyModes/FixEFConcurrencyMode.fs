module FixEFConcurrencyMode

// When using Entity Framework, the ConcurrencyMode attribute for properties
// mapped to rowversion (a.k.a. timestamp) database columns, should be set to
// ConcurrencyMode.Fixed, to enable optimistic concurrency.
//
// Unfortunately, due to a bug in the Entity Framework tools, that property is 
// set to ConcurrencyMode.None when you generate an edmx file by reverse 
// engineering a database.
//
// This utility corrects the faulty ConcurrencyMode settings in your edmx files.

open System
open System.Linq
open System.Xml.Linq

open CommandLine
open EFConcurrencyModeTest

// ------------------------------------------------------------------------------

type CliOptions() =
    [<Option('i', "input", Required = true, HelpText = "-i filename   Edmx input file.")>]
    member val InputFile = "" with get, set

    [<Option('o', "output", HelpText = "-o filename   Output file. Default: overwrite input file.")>]
    member val OutputFile = "" with get, set

    [<OptionArray('n', "name-patterns", 
                  HelpText = "-n pattern1 pattern2 ...   Regex patterns for column names used for concurrency control.")>]
    member val ConcurrencyColumnNamePatterns : string[] = [||] with get, set

    [<OptionArray('t', "types", 
                  HelpText = "-t type1 type2 ...   Types for concurrency control. Default: rowversion timestamp")>]
    member val RowVersionTypes = [| "rowversion"; "timestamp" |] with get, set

    [<Option('p', "preview", HelpText = "Preview. Changes are shown but nothing is written to file.")>]
    member val Preview = false with get, set

    [<Option('q', "quiet", HelpText = "Quiet mode.")>]
    member val Quiet = false with get, set

    [<HelpOption>]
    member o.GetUsage () =
        let help = Text.HelpText(AddDashesToOption = true)
    
        help.AddOptions o

        "FixEFConcurrencyModes version 1.01 April 2014\n\
         Sets ConcurrencyMode to Fixed on columns used for optimistic concurrency control.\n\n\
         Usage: FixEFConcurrencyModes [OPTIONS]" +
        help.ToString() +
        "\nExamples\n\
         --------\n\n\
         The following sets ConcurrencyMode on all columns with names that include the substring \"VersionNo\" \
         or end with \"Revision\":\n\
         FixEFConcurrencyModes -i myfile.edmx -n VersionNo Revision$\n\n\
         The following sets ConcurrencyMode on all columns of type thistype or thattype:\n\
         FixEFConcurrencyModes -i myfile.edmx -t thistype thattype\n"

// ------------------------------------------------------------------------------

let printBadConcurrencyModes (entProps : EntityProperty seq) =
    printfn "The concurrency mode of these columns will be changed:\n"

    let maxEntityWidth = max 13 (entProps |> Seq.map (fun i -> i.CsdlEntity.Length) |> Seq.max)
    let maxPropertyWidth = max 13 (entProps |> Seq.map (fun i -> i.CsdlProperty.Length) |> Seq.max)
    let maxTableWidth = max 13 (entProps |> Seq.map (fun i -> i.SsdlEntity.Length) |> Seq.max)
    let maxColumnWidth = max 13 (entProps |> Seq.map (fun i -> i.SsdlProperty.Length) |> Seq.max)
    let pad maxWidth (s : string) = s.PadRight(maxWidth, ' ')

    printfn "%s  %s  %s  %s" 
            (pad maxEntityWidth "CSDL Entity") (pad maxPropertyWidth "CSDL Property")
            (pad maxTableWidth "Table") (pad maxColumnWidth "Column")
    printfn "%s  %s  %s  %s" 
            (String('-', maxEntityWidth)) (String('-', maxPropertyWidth))
            (String('-', maxTableWidth)) (String('-', maxColumnWidth))
    for entProp in entProps do
        printfn "%s  %s  %s  %s" 
                (pad maxEntityWidth entProp.CsdlEntity) (pad maxPropertyWidth entProp.CsdlProperty)
                (pad maxTableWidth entProp.SsdlEntity) (pad maxColumnWidth entProp.SsdlProperty)
    printfn ""

// ------------------------------------------------------------------------------

let fixCsdlConcurrencyModes (csdl : XElement) (entProps : EntityProperty seq) =
    // entities = all Edmx/Runtime/ConceptualModels/Schema/EntityType entries.
    let entities = csdl.Elements(XName.Get("EntityType", ConcurrencyModeTester.CsdlNs))

    let setConcurrencyModeFixed entProp =
        let entity = entities.First(fun e -> e.Attribute(XName.Get("Name")).Value = entProp.CsdlEntity)
        let property = entity.Elements(XName.Get("Property", ConcurrencyModeTester.CsdlNs))
                             .FirstOrDefault(fun p -> p.Attribute(XName.Get("Name")).Value = entProp.CsdlProperty)
        property.SetAttributeValue(XName.Get("ConcurrencyMode"), "Fixed")

    for entProp in entProps do
        setConcurrencyModeFixed entProp

// ------------------------------------------------------------------------------

let getCsdlSsdlMsl (xdoc : XDocument) =
    let runtimeElement = xdoc.Root.Element(XName.Get("Runtime", ConcurrencyModeTester.EdmxNs))
    let conceptualModelsElement = runtimeElement.Element(XName.Get("ConceptualModels", ConcurrencyModeTester.EdmxNs))
    let storageModelsElement = runtimeElement.Element(XName.Get("StorageModels", ConcurrencyModeTester.EdmxNs))
    let mappingsElement = runtimeElement.Element(XName.Get("Mappings", ConcurrencyModeTester.EdmxNs))

    let csdl = conceptualModelsElement.Element(XName.Get("Schema", ConcurrencyModeTester.CsdlNs))
    let ssdl = storageModelsElement.Element(XName.Get("Schema", ConcurrencyModeTester.SsdlNs))
    let msl = mappingsElement.Element(XName.Get("Mapping", ConcurrencyModeTester.MslNs))
    (csdl, ssdl, msl)


let fixEdmxConcurrencyModes (options : CliOptions) =
    let xdoc = XDocument.Load options.InputFile
    let (csdl, ssdl, msl) = getCsdlSsdlMsl xdoc

    let cmt = ConcurrencyModeTester(
                  ConcurrencyColumnNamePatterns = options.ConcurrencyColumnNamePatterns,
                  RowVersionTypes = options.RowVersionTypes)

    let badConcurrencyModes = cmt.BadConcurrencyModes(csdl, ssdl, msl)

    if badConcurrencyModes.Length > 0 then
        fixCsdlConcurrencyModes csdl badConcurrencyModes
        if not options.Quiet then 
            printBadConcurrencyModes badConcurrencyModes

        let newBadConcurrencyModes = cmt.BadConcurrencyModes(csdl, ssdl, msl)
        if newBadConcurrencyModes.Length <> 0 then 
            failwith "Failed to fix the concurrency modes!"

        if not options.Preview then
            printfn "Writing %s." options.OutputFile
            xdoc.Save options.OutputFile

    elif not options.Quiet then
        printfn "No changes needed."

// ------------------------------------------------------------------------------

[<EntryPoint>]
let main argv = 
    let options = CliOptions()
    if Parser.Default.ParseArguments(argv, options) then
        if options.OutputFile = "" then
            options.OutputFile <- options.InputFile

        try
            fixEdmxConcurrencyModes options
            0
        with
            | :? System.IO.IOException as e ->
                printfn "Error: %s" e.Message
                -1
            | _ -> reraise()
    else
        -1

// ------------------------------------------------------------------------------
