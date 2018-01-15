module ChartPrinter

open FSharp.Data
open System.IO
open System.Collections.Generic

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
        | 2 -> { currentResult with lose = currentResult.lose + 1 }
        | other -> currentResult

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

let predictWins playerData=
    playerData.results
    |> Seq.filter (fun x -> x.Value.lose + x.Value.win < 2 && x.Value.win = 1)
    |> Seq.length

let winGames playerData=
    playerData.results
    |> Seq.filter (fun x -> x.Value.win = 2)

let lostGames playerData=
    playerData.results
    |> Seq.filter (fun x -> x.Value.lose = 2)

let drawGames playerData=
    playerData.results
    |> Seq.filter (fun x -> x.Value.lose = 1 && x.Value.win = 1)

let notFullGames playerData=
    playerData.results
    |> Seq.filter (fun x -> x.Value.lose + x.Value.win < 2)

let printPlayerPredictionRow player playerData threshold =
    let predictedWins = predictWins playerData
    let totalGames = playerData.results |> Seq.map (fun x -> x.Value.lose + x.Value.win) |> Seq.sum
    let totalGamesLeft = playerData.results |> Seq.map (fun x -> 2 - (x.Value.lose + x.Value.win)) |> Seq.sum
    
    if (playerData.winGames + predictedWins >= threshold) then
        printfn "%s,%d,%d,%d,%d" player playerData.winGames (playerData.winGames + predictedWins) totalGames totalGamesLeft

let constructInternalState doc =
    let initialState = Map.empty<string,PlayerRow>
    let data = doc |> Seq.fold processRecord initialState
    data

let printStats data =
    let accumulatePlayerGames state (playerData:KeyValuePair<string,PlayerRow>) =
        let playerGames = playerData.Value.results |> Seq.map (fun x -> x.Value.lose + x.Value.win) |> Seq.sum
        state + playerGames
    let totalGames = (data |> Seq.fold accumulatePlayerGames 0) / 2
    printfn "Total games: %d" totalGames

let printBotStats (sourceFile: string) botName =
    let doc = SSCAITResultsFile.Load(sourceFile).Rows
    let data = constructInternalState doc 
    let botStats = data.Item(botName)
    printfn "Stats for bot %s" botName
    printfn "Win games: %d" botStats.winGames
    printfn "Predicted wins: %d" (predictWins botStats)
    printfn "Win games"
    for wg in winGames botStats do
        printfn "\t%s (%d-%d)" wg.Key wg.Value.win wg.Value.lose
    printfn "Lost games"
    for wg in lostGames botStats do
        printfn "\t%s (%d-%d)" wg.Key wg.Value.win wg.Value.lose
    printfn "Draw games"
    for wg in drawGames botStats do
        printfn "\t%s (%d-%d)" wg.Key wg.Value.win wg.Value.lose
    printfn "Not full games"
    for wg in notFullGames botStats do
        printfn "\t%s (%d-%d)" wg.Key wg.Value.win wg.Value.lose

let printChart (sourceFile: string) =
    let doc = SSCAITResultsFile.Load(sourceFile).Rows
    let data = constructInternalState doc 
    for record in data do
        printPlayerRow record.Key record.Value

let printPrediction (sourceFile: string) threshold =
    let doc = SSCAITResultsFile.Load(sourceFile).Rows
    let data = constructInternalState doc
    printfn "Bot,Score,PredictedScore,PlayedGames,GamesLeft"
    for record in data do
        printPlayerPredictionRow record.Key record.Value threshold
    printStats data
