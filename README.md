# sscait-tools
Analysis scripts for the SSCAIT

## Prerequiresites
Install .NET Core SDK 2.1.4

## How to build

	dotnet restore
    dotnet build

## How to run

- Collect data

	Collect data from SSCAIT website. This should be first step in the analysis of results.

    `dotnet run get <games>`

	*games* - Specify how much games take from the server. When during tournament it better take actual count of games played so far.
	For example: `dotnet run get 6006`

- Predict results

    `dotnet run predict <threshold>`

	*threshold* - specify which minimum predicted level of win games return. For SSCAIT 2017 make sense take 109 as threshold

- Print bot stats

	Prints bot statistics and list bots agains which he wins, lose or has a draw
 
	`dotnet run botStats "Andrey Kurdiumov"`

- Print lost game replays

	Prints URLs to lost game replays
 
	`dotnet run lostGames "Andrey Kurdiumov"`
