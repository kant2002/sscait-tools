// Learn more about F# at http://fsharp.org

open System
open System.IO
open FSharp.Data
open ChartPrinter

type SSCAIT = JsonProvider<"https://sscaitournament.com/api/games.php?future=false">

let getDataPage page pageSize = 
    async {
        let url = sprintf "https://sscaitournament.com/api/games.php?future=false&page=%d&count=%d" page pageSize;
        let! doc = SSCAIT.AsyncLoad(url)
        return doc
    }


let getResultsPage page pageSize count = 
    async {
        let! doc = getDataPage page pageSize
        return doc |> Seq.take(count)
        }

let getResults count = 
    let pageSize = 1000
    let pagesCount = (count + pageSize - 1) / pageSize
    let f page = 
        if page = pagesCount then
            getResultsPage page pageSize (count % pageSize)
        else
            getResultsPage page pageSize pageSize
    
    [1..pagesCount]
        |> Seq.map f
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Seq.concat


type ModeOption = 
    | ModeGetData
    | ModePrintChart
    | ModePrintPrediction
    | ModeBotStats
    | ModeInvalid

type CommandLineOptions = {
    mode: ModeOption
    gamesProcess: int
    filterThreshold: int
    botName: string
}

// create the "helper" recursive function
let rec parseCommandLineRec args optionsSoFar = 
    match args with 
    // empty list means we're done.
    | [] -> 
        optionsSoFar  

    | "get"::xs ->
        match xs with
        | [] -> 
            optionsSoFar  
        | games::xss ->
            let newOptionsSoFar = { optionsSoFar with gamesProcess = (games|> int); mode=ModeGetData }
            parseCommandLineRec xss newOptionsSoFar 

    | "print"::xs -> 
        let newOptionsSoFar = { optionsSoFar with mode=ModePrintChart}
        parseCommandLineRec xs newOptionsSoFar 

    | "botStats"::xs ->
        match xs with
        | [] -> 
            optionsSoFar  
        | botName::xss ->
            let newOptionsSoFar = { optionsSoFar with botName = botName; mode=ModeBotStats }
            parseCommandLineRec xss newOptionsSoFar 

    | "predict"::xs -> 
        match xs with
        | [] -> 
            optionsSoFar  
        | threshold::xss ->
            let newOptionsSoFar = { optionsSoFar with filterThreshold = (threshold|> int); mode=ModePrintPrediction }
            parseCommandLineRec xss newOptionsSoFar 

    // handle unrecognized option and keep looping
    | x::xs -> 
        printfn "Option '%s' is unrecognized" x
        parseCommandLineRec xs optionsSoFar 

// create the "public" parse function
let parseCommandLine args = 
    // create the defaults
    let defaultOptions = {
        mode = ModeInvalid;
        gamesProcess = 0;
        filterThreshold = 0;
        botName = "<<Not specified>>"
        }

    // call the recursive one with the initial options
    parseCommandLineRec args defaultOptions



[<EntryPoint>]
let main argv = 
    let commandArgs = parseCommandLine (argv |> Seq.toList)
    match commandArgs.mode with
        | ModeGetData ->
            let doc = getResults commandArgs.gamesProcess
            use streamWriter = new StreamWriter("results.txt", false)
            streamWriter.WriteLine "Host,Guest,Result,Replay"
            for record in doc do
                let line = sprintf "%s,%s,%d,%s" record.Host record.Guest record.Result record.Replay
                streamWriter.WriteLine line
        | ModePrintChart ->
            printfn "Printing chart"
            let sourceFile = Path.Combine(Directory.GetCurrentDirectory(), "results.txt")
            printChart sourceFile
        | ModeBotStats ->
            let sourceFile = Path.Combine(Directory.GetCurrentDirectory(), "results.txt")
            printBotStats sourceFile commandArgs.botName
        | ModePrintPrediction ->
            let sourceFile = Path.Combine(Directory.GetCurrentDirectory(), "results.txt")
            printPrediction sourceFile commandArgs.filterThreshold
        | ModeInvalid ->
            printfn "Invalid arguments"
    
    0 // return an integer exit code
