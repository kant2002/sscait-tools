module ChartPrinter

open FSharp.Data
open System.IO

type SSCAITResultsFile = CsvProvider<"results.txt">
type SSCAITResultsFileRow = SSCAITResultsFile.Row

type GameResults =
    {
        win: int;
        lose: int
    }

type PlayerRow =
    {
        name: string;
        winGames: int;
        results: Map<string,GameResults>
    }

type TournamentInformation = Map<string,PlayerRow>

let registerResult currentResult result =
    match result with
        | 1 -> { currentResult with win = currentResult.win + 1 }
        | other -> { currentResult with lose = currentResult.lose + 1 }

let registerPlayerResult player againstPlayer result =
    let currentPlayerData =
        match Map.tryFind againstPlayer player.results with
        | Some player -> player
        | None -> { win = 0; lose = 0 }
    let newplayer = registerResult currentPlayerData result
    let winGames = if result = 1 then player.winGames + 1 else player.winGames
    { player with winGames = winGames; results = Map.add againstPlayer newplayer player.results }

let registerTournamentPlayerResult tournament player againstPlayer result =
    let currentPlayer =
        match Map.tryFind player tournament with
        | Some playerRow -> playerRow
        | None -> { name = player; winGames = 0; results = Map.empty<string,GameResults> }
    let updatedPlayer = registerPlayerResult currentPlayer againstPlayer result
    Map.add player updatedPlayer tournament

let processRecord tournament (record: SSCAITResultsFileRow) =
    let intermediate = registerTournamentPlayerResult tournament record.Host record.Guest record.Result
    let updatedResult = 3 - record.Result
    registerTournamentPlayerResult intermediate record.Guest record.Host updatedResult

let printPlayerRow player playerData =
    printf "%s,%d||" player playerData.winGames
    let dataToPrint = playerData.results
    for pair in dataToPrint do
        let y = pair.Value
        printf ",%d-%d" y.win y.lose
    
    printfn ""

let printPlayerPredictionRow player playerData =
    let dataToPrint = playerData.results |> Seq.filter (fun x -> x.Value.lose + x.Value.win < 2 && x.Value.win = 1) |> Seq.length
    //for pair in dataToPrint do
    //    let y = pair.Value
    //    printf ",%d-%d" y.win y.lose
    
    if (playerData.winGames + dataToPrint >= 108) then
        printf "%s,%d||" player playerData.winGames
        printfn "%d" (playerData.winGames + dataToPrint)

let constructInternalState doc =
    let initialState = Map.empty<string,PlayerRow>
    let data = doc |> Seq.fold processRecord initialState
    data

let printChart (sourceFile: string) =
    let doc = SSCAITResultsFile.Load(sourceFile).Rows
    let data = constructInternalState doc 
    for record in data do
        printPlayerRow record.Key record.Value

let printPrediction (sourceFile: string) =
    let doc = SSCAITResultsFile.Load(sourceFile).Rows
    let data = constructInternalState doc 
    for record in data do
        printPlayerPredictionRow record.Key record.Value
