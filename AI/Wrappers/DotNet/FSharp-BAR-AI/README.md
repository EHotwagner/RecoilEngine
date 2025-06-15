# F# BAR AI Implementation

High-performance AI implementation for Beyond All Reason (BAR) using the F# RecoilEngine wrapper. This demonstrates practical usage of the data-oriented architecture for competitive BAR AI.

## Features

- **Data-Oriented Design** - Uses Structure-of-Arrays for optimal performance
- **Custom Analytics** - Calculates derived values like threat assessments, resource efficiency
- **Pure Functions** - No hidden state, everything calculable from world state
- **BAR-Specific Logic** - Tailored for BAR unit types, economy, and combat

## Project Structure

```
├── src/
│   ├── BAR.AI.FSharp.fsproj          # Main project file
│   ├── WorldState.fs                 # BAR-specific world state
│   ├── UnitTypes.fs                  # BAR unit definitions
│   ├── Economy.fs                    # Economic AI system
│   ├── Military.fs                   # Military AI system
│   ├── Building.fs                   # Construction AI system
│   ├── Scouting.fs                   # Reconnaissance AI system
│   └── MainAI.fs                     # Main AI coordinator
├── examples/
│   ├── SimpleEconomyAI.fs            # Basic economic focused AI
│   ├── AggressiveMilitaryAI.fs       # Military focused AI
│   └── BalancedAI.fs                 # Balanced strategy AI
└── tests/
    ├── BAR.AI.FSharp.Tests.fsproj
    └── AISystemTests.fs              # AI system tests
```

## Example AI

```fsharp
open BAR.AI.FSharp
open RecoilEngine.FSharp

type MyBARai() =
    let mutable barState = BARWorldState.empty
    
    interface IRecoilAI with
        member this.OnUpdate(frame) =
            // Get raw world state from wrapper
            let worldState = getCurrentWorldState()
            
            // Convert to BAR-specific enriched state
            let newBARState = updateBARState barState worldState
            barState <- newBARState
            
            // Generate BAR-specific commands
            let commands = [|
                yield! Economy.generateCommands newBARState
                yield! Military.generateCommands newBARState  
                yield! Building.generateCommands newBARState
                yield! Scouting.generateCommands newBARState
            |]
            
            executeCommands commands
```

## Building

```bash
# Build (requires FSharp-Recoil-Wrapper)
cd src
dotnet build BAR.AI.FSharp.fsproj

# Run tests
cd tests  
dotnet test BAR.AI.FSharp.Tests.fsproj
```
