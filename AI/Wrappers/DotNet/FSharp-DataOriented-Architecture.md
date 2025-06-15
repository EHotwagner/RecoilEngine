# F# Data-Oriented AI Architecture for RecoilEngine/Spring

## Overview

This document describes the **F#-only, data-oriented AI architecture** for RecoilEngine/Spring AI development. The design uses a pure data pipeline approach where arrays flow through transformations with no hidden state - everything is visible and calculable from the previous frame's data.

## Core Philosophy: Pure Data Pipeline

### No C# Wrapper Needed
You're absolutely correct - **P/Invoke creates .NET assemblies that F# can use directly**. No C# shim layer is necessary since:
- P/Invoke functions are callable from any .NET language
- F# arrays and value types marshal perfectly to native code
- F# provides superior array processing compared to C#
- Direct F# → Native → F# pipeline is more efficient

### Fixed Conveyor Belt Structure
```fsharp
// Pure data pipeline: Frame N → Frame N+1
// No hidden state, everything flows through visible transformations

let processFrame (previousState: WorldState) (rawEvents: NativeEvent[]) : WorldState * Command[] =
    // Step 1: Update world state from native events
    let currentState = updateWorldState previousState rawEvents
    
    // Step 2: Calculate derived values from current state
    let enrichedState = calculateDerivedValues currentState
    
    // Step 3: Generate commands from enriched state
    let commands = generateCommands enrichedState
    
    (enrichedState, commands)
```

## Data-Oriented Types and Structures

### Core World State (Structure-of-Arrays)
```fsharp
/// Complete world state using Structure-of-Arrays for cache efficiency
type WorldState = {
    // Basic unit data arrays (parallel indexed)
    UnitIds: int[]
    UnitPositions: Vector3[]
    UnitHealth: float32[]
    UnitMaxHealth: float32[]
    UnitDefIds: int16[]         // Compact unit type IDs
    UnitOwners: byte[]          // Player/faction IDs
    UnitStates: byte[]          // Packed boolean flags
    
    // Resource state arrays (by player)
    PlayerMetal: float32[]
    PlayerEnergy: float32[]
    PlayerMetalIncome: float32[]
    PlayerEnergyIncome: float32[]
    
    // Map data arrays
    MapHeightData: float32[]    // Heightmap
    MapMetalData: float32[]     // Metal map
    MapSize: int * int
    
    // Temporal data
    CurrentFrame: int
    FrameDelta: float32
    
    // Derived/calculated values (see below)
    UnitHealthPercentages: float32[]
    UnitsVisibleToMe: int[]
    EnemiesNearBase: int[]
    NumberOfEnemiesSeenLast20Frames: int
    MetalEfficiency: float32
    EnergyEfficiency: float32
    ThreatLevelByArea: float32[]
}
```

### Native Interop Types
```fsharp
/// Raw data from native engine via P/Invoke
[<Struct>]
type NativeUnitData = {
    Id: int
    X: float32
    Y: float32  
    Z: float32
    Health: float32
    MaxHealth: float32
    DefId: int16
    Owner: byte
    StateFlags: byte
}

[<Struct>]
type NativeEvent = {
    EventType: int
    Frame: int
    UnitId: int
    Data1: int
    Data2: int
    FloatData: float32
}

/// P/Invoke functions - no wrapper needed
module NativeInterop =
    [<DllImport("SpringAIWrapper")>]
    extern int GetWorldSnapshot(NativeUnitData* unitData, int maxUnits)
    
    [<DllImport("SpringAIWrapper")>]
    extern int GetFrameEvents(NativeEvent* events, int maxEvents)
    
    [<DllImport("SpringAIWrapper")>]
    extern float GetPlayerMetal(int playerId)
    
    [<DllImport("SpringAIWrapper")>]
    extern int ExecuteCommandBatch(int* commandTypes, float* commandData, int count)
```

### Command Types (Pure Data)
```fsharp
/// Commands as pure data - no objects or methods
[<Struct>]
type Command = {
    CommandType: CommandType
    UnitId: int
    TargetId: int
    X: float32
    Y: float32
    Z: float32
    StringParam: string // Unit def name for builds
    Priority: byte
}

and CommandType =
    | Build = 1
    | Move = 2
    | Attack = 3
    | Stop = 4
    | Guard = 5
    | Patrol = 6
```

## Data Pipeline Implementation

### Frame Processing Pipeline
```fsharp
/// Main update function - pure data transformation
let updateAI (previousState: WorldState) : WorldState * Command[] =
    
    // Step 1: Get raw data from native engine
    let nativeUnits = Array.zeroCreate<NativeUnitData> 5000
    let unitCount = NativeInterop.GetWorldSnapshot(&&nativeUnits.[0], 5000)
    
    let nativeEvents = Array.zeroCreate<NativeEvent> 1000  
    let eventCount = NativeInterop.GetFrameEvents(&&nativeEvents.[0], 1000)
    
    // Step 2: Convert native data to world state
    let baseState = convertNativeToWorldState nativeUnits.[..unitCount-1] nativeEvents.[..eventCount-1]
    
    // Step 3: Calculate all derived values
    let enrichedState = calculateDerivedValues baseState previousState
    
    // Step 4: Run AI systems on enriched state
    let commands = runAISystems enrichedState
    
    (enrichedState, commands)

/// Convert raw native data to structured arrays
let convertNativeToWorldState (units: NativeUnitData[]) (events: NativeEvent[]) : WorldState =
    {
        UnitIds = units |> Array.map (_.Id)
        UnitPositions = units |> Array.map (fun u -> Vector3(u.X, u.Y, u.Z))
        UnitHealth = units |> Array.map (_.Health)
        UnitMaxHealth = units |> Array.map (_.MaxHealth)
        UnitDefIds = units |> Array.map (_.DefId)
        UnitOwners = units |> Array.map (_.Owner)
        UnitStates = units |> Array.map (_.StateFlags)
        
        PlayerMetal = [| for i in 0..7 -> NativeInterop.GetPlayerMetal(i) |]
        PlayerEnergy = [| for i in 0..7 -> NativeInterop.GetPlayerEnergy(i) |]
        PlayerMetalIncome = [| for i in 0..7 -> NativeInterop.GetPlayerMetalIncome(i) |]
        PlayerEnergyIncome = [| for i in 0..7 -> NativeInterop.GetPlayerEnergyIncome(i) |]
        
        MapHeightData = [||] // Get from native if needed
        MapMetalData = [||]  // Get from native if needed
        MapSize = (256, 256)
        
        CurrentFrame = events |> Array.tryLast |> Option.map (_.Frame) |> Option.defaultValue 0
        FrameDelta = 1.0f / 30.0f // 30 FPS
        
        // Derived values calculated below
        UnitHealthPercentages = [||]
        UnitsVisibleToMe = [||]
        EnemiesNearBase = [||]
        NumberOfEnemiesSeenLast20Frames = 0
        MetalEfficiency = 0.0f
        EnergyEfficiency = 0.0f
        ThreatLevelByArea = [||]
    }
```

### Derived Value Calculations
```fsharp
/// Calculate all derived values from base state - pure functions
let calculateDerivedValues (currentState: WorldState) (previousState: WorldState) : WorldState =
    
    // Calculate health percentages for all units
    let healthPercentages = 
        Array.map2 (fun health maxHealth -> 
            if maxHealth > 0.0f then health / maxHealth else 0.0f
        ) currentState.UnitHealth currentState.UnitMaxHealth
    
    // Find units visible to me (player 0)
    let myUnits = 
        Array.zip currentState.UnitIds currentState.UnitOwners
        |> Array.filter (fun (_, owner) -> owner = 0uy)
        |> Array.map fst
    
    // Find enemies near my base
    let myBasePosition = Vector3(100.0f, 0.0f, 100.0f) // Could be calculated
    let enemiesNearBase =
        Array.zip3 currentState.UnitIds currentState.UnitPositions currentState.UnitOwners
        |> Array.filter (fun (_, pos, owner) -> 
            owner <> 0uy && Vector3.Distance(pos, myBasePosition) < 500.0f)
        |> Array.map (fun (id, _, _) -> id)
    
    // Calculate number of enemies seen in last 20 frames
    let enemiesSeenLast20Frames = 
        if currentState.CurrentFrame >= 20 then
            // Count unique enemy units seen in last 20 frames
            // This would use frame history if we maintained it
            enemiesNearBase.Length
        else
            previousState.NumberOfEnemiesSeenLast20Frames
    
    // Calculate resource efficiency
    let metalEfficiency = 
        if currentState.PlayerMetalIncome.[0] > 0.0f then
            currentState.PlayerMetal.[0] / currentState.PlayerMetalIncome.[0]
        else 0.0f
        
    let energyEfficiency =
        if currentState.PlayerEnergyIncome.[0] > 0.0f then
            currentState.PlayerEnergy.[0] / currentState.PlayerEnergyIncome.[0]
        else 0.0f
    
    // Calculate threat levels by map area (simplified grid)
    let threatLevels = calculateThreatGrid currentState
    
    { currentState with
        UnitHealthPercentages = healthPercentages
        UnitsVisibleToMe = myUnits
        EnemiesNearBase = enemiesNearBase
        NumberOfEnemiesSeenLast20Frames = enemiesSeenLast20Frames
        MetalEfficiency = metalEfficiency
        EnergyEfficiency = energyEfficiency
        ThreatLevelByArea = threatLevels }

/// Calculate threat level for each area of the map
let calculateThreatGrid (state: WorldState) : float32[] =
    let gridSize = 16 // 16x16 grid
    let (mapWidth, mapHeight) = state.MapSize
    let cellWidth = float32 mapWidth / float32 gridSize
    let cellHeight = float32 mapHeight / float32 gridSize
    
    let threatGrid = Array.zeroCreate (gridSize * gridSize)
    
    // Calculate threat for each enemy unit
    Array.zip3 state.UnitPositions state.UnitOwners state.UnitHealth
    |> Array.iter (fun (pos, owner, health) ->
        if owner <> 0uy && health > 0.0f then // Enemy unit
            let gridX = int (pos.X / cellWidth) |> max 0 |> min (gridSize - 1)
            let gridZ = int (pos.Z / cellHeight) |> max 0 |> min (gridSize - 1)
            let gridIndex = gridZ * gridSize + gridX
            threatGrid.[gridIndex] <- threatGrid.[gridIndex] + health)
    
    threatGrid
```

### AI Systems (Pure Functions)
```fsharp
/// Run all AI systems on enriched world state
let runAISystems (state: WorldState) : Command[] =
    [|
        yield! runEconomySystem state
        yield! runMilitarySystem state  
        yield! runBuildSystem state
        yield! runScoutSystem state
    |]

/// Economy system - pure function generating economic commands
let runEconomySystem (state: WorldState) : Command[] =
    let myMetal = state.PlayerMetal.[0]
    let myEnergy = state.PlayerEnergy.[0]
    let metalIncome = state.PlayerMetalIncome.[0]
    let energyIncome = state.PlayerEnergyIncome.[0]
    
    // Find idle builders
    let builders = 
        Array.zip3 state.UnitIds state.UnitDefIds state.UnitStates
        |> Array.filter (fun (_, defId, stateFlags) -> 
            defId = 1s && (stateFlags &&& 1uy) = 0uy) // Builder and not busy
        |> Array.map (fun (id, _, _) -> id)
    
    [|
        // Build metal extractors if low on metal
        if metalIncome < 10.0f && myMetal > 50.0f && builders.Length > 0 then
            yield { CommandType = CommandType.Build
                    UnitId = builders.[0]
                    TargetId = 0
                    X = 200.0f; Y = 0.0f; Z = 200.0f
                    StringParam = "armmex"
                    Priority = 100uy }
        
        // Build energy if low on energy
        if energyIncome < 20.0f && myMetal > 60.0f && builders.Length > 1 then
            yield { CommandType = CommandType.Build
                    UnitId = builders.[1]
                    TargetId = 0
                    X = 150.0f; Y = 0.0f; Z = 150.0f
                    StringParam = "armsolar"
                    Priority = 90uy }
    |]

/// Military system - respond to threats
let runMilitarySystem (state: WorldState) : Command[] =
    let enemiesNearBase = state.EnemiesNearBase
    
    if enemiesNearBase.Length > 0 then
        // Find military units and send them to defend
        let militaryUnits =
            Array.zip3 state.UnitIds state.UnitDefIds state.UnitOwners
            |> Array.filter (fun (_, defId, owner) -> 
                owner = 0uy && defId > 10s && defId < 50s) // Military unit IDs
            |> Array.map (fun (id, _, _) -> id)
        
        [|
            for unitId in militaryUnits do
                if enemiesNearBase.Length > 0 then
                    yield { CommandType = CommandType.Attack
                            UnitId = unitId
                            TargetId = enemiesNearBase.[0]
                            X = 0.0f; Y = 0.0f; Z = 0.0f
                            StringParam = ""
                            Priority = 95uy }
        |]
    else
        [||]

/// Build system - construction planning
let runBuildSystem (state: WorldState) : Command[] =
    let myMetal = state.PlayerMetal.[0]
    let myEnergy = state.PlayerEnergy.[0]
    let frame = state.CurrentFrame
    
    // Build factory every 5 minutes if we have resources
    if frame % (30 * 60 * 5) = 0 && myMetal > 200.0f && myEnergy > 100.0f then
        let builders = 
            Array.zip3 state.UnitIds state.UnitDefIds state.UnitStates
            |> Array.filter (fun (_, defId, stateFlags) -> 
                defId = 1s && (stateFlags &&& 1uy) = 0uy)
            |> Array.map (fun (id, _, _) -> id)
        
        if builders.Length > 0 then
            [| { CommandType = CommandType.Build
                 UnitId = builders.[0]
                 TargetId = 0
                 X = 300.0f; Y = 0.0f; Z = 300.0f
                 StringParam = "armlab"
                 Priority = 80uy } |]
        else [||]
    else [||]

/// Scout system - exploration and intelligence
let runScoutSystem (state: WorldState) : Command[] =
    // Send fast units to unexplored areas
    let scoutUnits =
        Array.zip3 state.UnitIds state.UnitDefIds state.UnitStates
        |> Array.filter (fun (_, defId, stateFlags) -> 
            defId = 20s && (stateFlags &&& 2uy) = 0uy) // Fast unit and idle
        |> Array.map (fun (id, _, _) -> id)
    
    [|
        for i, unitId in Array.indexed scoutUnits do
            let angle = float32 i * 2.0f * System.MathF.PI / float32 scoutUnits.Length
            let scoutX = 500.0f * System.MathF.Cos(angle)
            let scoutZ = 500.0f * System.MathF.Sin(angle)
            
            yield { CommandType = CommandType.Move
                    UnitId = unitId
                    TargetId = 0
                    X = scoutX; Y = 0.0f; Z = scoutZ
                    StringParam = ""
                    Priority = 50uy }
    |]
```

### Command Execution
```fsharp
/// Execute commands via P/Invoke batch call
let executeCommands (commands: Command[]) : unit =
    if commands.Length > 0 then
        let commandTypes = commands |> Array.map (fun c -> int c.CommandType)
        let commandData = Array.zeroCreate<float32> (commands.Length * 8) // Max 8 floats per command
        
        // Pack command data into flat array
        commands |> Array.iteri (fun i cmd ->
            let baseIndex = i * 8
            commandData.[baseIndex] <- float32 cmd.UnitId
            commandData.[baseIndex + 1] <- float32 cmd.TargetId
            commandData.[baseIndex + 2] <- cmd.X
            commandData.[baseIndex + 3] <- cmd.Y
            commandData.[baseIndex + 4] <- cmd.Z
            commandData.[baseIndex + 5] <- float32 cmd.Priority
            // String parameters would need separate handling
        )
        
        let result = NativeInterop.ExecuteCommandBatch(&&commandTypes.[0], &&commandData.[0], commands.Length)
        printfn "Executed %d commands, result: %d" commands.Length result
```

## Main AI Loop

### Complete AI Implementation
```fsharp
/// Main AI class - just holds state and runs pipeline
type DataOrientedAI() =
    let mutable currentState = WorldState.empty
    
    /// Called every frame by the game engine
    member this.OnUpdate(frame: int) =
        let (newState, commands) = updateAI currentState
        currentState <- newState
        executeCommands commands
    
    /// Called when game starts
    member this.OnInit(aiId: int) =
        printfn "Data-oriented F# AI initialized for player %d" aiId
        currentState <- WorldState.empty
    
    /// Called when game ends
    member this.OnRelease(reason: int) =
        printfn "AI shutting down, reason: %d" reason

/// Empty world state for initialization
module WorldState =
    let empty = {
        UnitIds = [||]
        UnitPositions = [||]
        UnitHealth = [||]
        UnitMaxHealth = [||]
        UnitDefIds = [||]
        UnitOwners = [||]
        UnitStates = [||]
        PlayerMetal = Array.zeroCreate 8
        PlayerEnergy = Array.zeroCreate 8
        PlayerMetalIncome = Array.zeroCreate 8
        PlayerEnergyIncome = Array.zeroCreate 8
        MapHeightData = [||]
        MapMetalData = [||]
        MapSize = (0, 0)
        CurrentFrame = 0
        FrameDelta = 1.0f / 30.0f
        UnitHealthPercentages = [||]
        UnitsVisibleToMe = [||]
        EnemiesNearBase = [||]
        NumberOfEnemiesSeenLast20Frames = 0
        MetalEfficiency = 0.0f
        EnergyEfficiency = 0.0f
        ThreatLevelByArea = [||]
    }
```

## Performance Characteristics

### Why This Approach Is Fast

1. **Structure-of-Arrays (SOA)** - All unit positions together, all health together = better cache usage
2. **No hidden state** - Everything visible and calculable = no surprises, easy to optimize
3. **Pure functions** - No side effects = compiler can optimize aggressively
4. **Array operations** - F# array functions are highly optimized
5. **Direct P/Invoke** - No C# wrapper overhead
6. **Batch processing** - One native call for all commands

### Memory Layout
```
Traditional (AOS):     Data-Oriented (SOA):
Unit1: [id][pos][hp]   UnitIds:    [id1][id2][id3][id4]...
Unit2: [id][pos][hp]   Positions:  [pos1][pos2][pos3][pos4]...  
Unit3: [id][pos][hp]   Health:     [hp1][hp2][hp3][hp4]...
Unit4: [id][pos][hp]
↑ Cache misses         ↑ Cache friendly
```

## Development Benefits

### Everything Is Visible
```fsharp
// At any point you can see exactly what data exists:
let debugState (state: WorldState) =
    printfn "Frame: %d" state.CurrentFrame
    printfn "Units: %d" state.UnitIds.Length
    printfn "Metal: %.1f" state.PlayerMetal.[0]
    printfn "Enemies seen last 20 frames: %d" state.NumberOfEnemiesSeenLast20Frames
    printfn "Metal efficiency: %.2f" state.MetalEfficiency
    // Every value is directly accessible
```

### Easy Testing
```fsharp
// Test any function in isolation
[<Test>]
let ``Economy system builds metal extractor when low on metal`` () =
    let testState = { WorldState.empty with
        PlayerMetal = [| 100.0f |]
        PlayerMetalIncome = [| 5.0f |] // Low income
        UnitIds = [| 1 |]
        UnitDefIds = [| 1s |] // Builder
        UnitStates = [| 0uy |] } // Idle
    
    let commands = runEconomySystem testState
    commands |> should not' (be Empty)
    commands.[0].StringParam |> should equal "armmex"
```

This architecture provides maximum performance through data-oriented design while maintaining the simplicity and transparency that makes F# ideal for game AI development.
