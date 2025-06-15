/// F#-first game context interface with data-oriented operations
namespace SpringAI.Core

open System
open System.Collections.Generic
open DataOrientedTypes

/// Primary game context interface designed for F# with data-oriented approach
type IGameContext =
    /// Get current resource state with units of measure
    abstract member GetResources: unit -> ResourceState
    
    /// Get complete world state as arrays for batch processing
    abstract member GetWorldState: unit -> WorldState
    
    /// Get unit information (returns None if unit doesn't exist)
    abstract member GetUnit: int -> UnitInfo option
    
    /// Get all friendly units as array for efficient processing
    abstract member GetFriendlyUnitsArray: unit -> CompactUnit array
    
    /// Get enemy units in range as array
    abstract member GetEnemyUnitsArray: float32<elmo> -> CompactUnit array
    
    /// Execute a command and get result
    abstract member ExecuteCommand: Command -> CommandResult
    
    /// Execute command batch efficiently
    abstract member ExecuteCommandBatch: CommandBatch -> BatchResult<CommandResult>
    
    /// Spatial queries for efficient unit finding
    abstract member QueryUnitsInRadius: Vector3 * float32<elmo> -> SpatialQueryResult
    abstract member QueryUnitsInArea: Vector3 * Vector3 -> SpatialQueryResult
    
    /// Get unit definition by name
    abstract member GetUnitDef: string -> UnitDefinition option
    
    /// Check if building is possible at location
    abstract member CanBuildAt: Vector3 * string -> bool
    
    /// Batch building validation
    abstract member CanBuildAtBatch: (Vector3 * string) array -> bool array
    
    /// Get map dimensions
    abstract member GetMapSize: unit -> int * int
    
    /// Get current frame with type safety
    abstract member GetCurrentFrame: unit -> int<frame>
    
    /// Get performance metrics for optimization
    abstract member GetPerformanceMetrics: unit -> PerformanceMetrics

/// Unit definition information
and UnitDefinition = {
    Id: int
    Name: string
    DefName: string
    MetalCost: float32<metal>
    EnergyCost: float32<energy>
    BuildTime: int<frame>
    MaxHealth: float32<hp>
    Categories: string list
    Faction: BARFaction
    CanBuild: string list  // Units this can build
}

/// F# module for working with game context using data-oriented patterns
module GameContext =
    
    /// Safe unit retrieval with detailed error information
    let tryGetUnitWithReason (context: IGameContext) (unitId: int) : Result<UnitInfo, string> =
        match context.GetUnit(unitId) with
        | Some unit when unit.IsAlive -> Ok unit
        | Some unit -> Error $"Unit {unitId} exists but is dead"
        | None -> Error $"Unit {unitId} not found"
    
    /// Get units by faction using efficient array operations
    let getUnitsByFaction (context: IGameContext) (faction: BARFaction) : CompactUnit array =
        context.GetFriendlyUnitsArray()
        |> Array.filter (fun u -> u.Faction = faction)
    
    /// Get units by category using unit definitions
    let getUnitsByCategory (context: IGameContext) (category: string) : UnitInfo list =
        context.GetFriendlyUnits()
        |> List.filter (fun u -> u.Categories |> List.contains category)
    
    /// Batch processing for unit queries
    let queryMultipleAreas (context: IGameContext) (areas: (Vector3 * Vector3) array) : SpatialQueryResult array =
        areas |> Array.map (fun (min, max) -> context.QueryUnitsInArea(min, max))
    
    /// Efficient nearest neighbor search using spatial grid
    let findNearestUnits (context: IGameContext) (position: Vector3) (count: int) : CompactUnit array =
        let result = context.QueryUnitsInRadius(position, 1000.0f<elmo>)
        let units = context.GetFriendlyUnitsArray()
        
        result.UnitIds
        |> Array.zip result.Distances
        |> Array.sortBy fst
        |> Array.take (min count result.Count)
        |> Array.map (fun (_, unitId) -> 
            units |> Array.find (fun u -> u.Id = unitId))
    
    /// Calculate total resource investment using array operations
    let calculateResourceInvestment (context: IGameContext) : float32<metal> * float32<energy> =
        let worldState = context.GetWorldState()
        let myFaction = ARM // Get from AI context
        
        let myUnits = 
            Array.zip worldState.UnitIds worldState.UnitFactions
            |> Array.filter (fun (_, faction) -> faction = myFaction)
            |> Array.map fst
        
        let totalMetal = 
            myUnits 
            |> Array.sumBy (fun unitId -> 
                let defId = worldState.UnitDefIds.[Array.findIndex ((=) unitId) worldState.UnitIds]
                // Look up cost from unit definition
                100.0f) * 1.0f<metal>  // Placeholder
        
        let totalEnergy = 
            myUnits 
            |> Array.sumBy (fun unitId -> 200.0f) * 1.0f<energy>  // Placeholder
        
        (totalMetal, totalEnergy)
    
    /// Check resource affordability
    let canAfford (context: IGameContext) (metalCost: float32<metal>) (energyCost: float32<energy>) : bool =
        let resources = context.GetResources()
        resources.Metal >= metalCost && resources.Energy >= energyCost
    
    /// Batch command validation for efficiency
    let validateCommandsBatch (context: IGameContext) (commands: Command array) : bool array =
        // Implementation would validate all commands in batch
        Array.map (fun _ -> true) commands  // Placeholder
        
/// Data-oriented processing module for high-performance operations
module DataOrientedProcessing =
    
    /// SIMD-optimized distance calculations
    let calculateDistancesSIMD (positions1: Vector3 array) (positions2: Vector3 array) : float32 array =
        Array.map2 (fun p1 p2 -> Vector3.Distance(p1, p2)) positions1 positions2
    
    /// Batch health percentage calculation
    let calculateHealthPercentages (worldState: WorldState) : float32 array =
        Array.map2 (fun health maxHealth -> 
            if maxHealth > 0.0f then health / maxHealth else 0.0f
        ) worldState.UnitHealth worldState.UnitMaxHealth
    
    /// Spatial partitioning for efficient queries
    let buildSpatialGrid (units: CompactUnit array) (cellSize: float32<elmo>) : SpatialGrid =
        let bounds = 
            if Array.isEmpty units then (Vector3.Zero, Vector3.Zero)
            else
                let minX = units |> Array.map (fun u -> u.X) |> Array.min
                let maxX = units |> Array.map (fun u -> u.X) |> Array.max
                let minZ = units |> Array.map (fun u -> u.Z) |> Array.min
                let maxZ = units |> Array.map (fun u -> u.Z) |> Array.max
                (Vector3(minX, 0.0f, minZ), Vector3(maxX, 0.0f, maxZ))
        
        let gridSize = int (Vector3.Distance(fst bounds, snd bounds) / float32 cellSize) + 1
        let cells = Array.init (gridSize * gridSize) (fun _ -> Array.empty<int>)
        
        // Populate grid cells
        units |> Array.iter (fun unit ->
            let cellX = int (unit.X / float32 cellSize)
            let cellZ = int (unit.Z / float32 cellSize)
            let cellIndex = cellZ * gridSize + cellX
            if cellIndex >= 0 && cellIndex < cells.Length then
                cells.[cellIndex] <- Array.append cells.[cellIndex] [| unit.Id |]
        )
        
        {
            GridSize = gridSize
            CellSize = cellSize
            UnitCells = cells
            MapBounds = bounds
        }
    
    /// Memory-efficient event processing
    let processEventBatch (events: GameEvent array) (processor: GameEvent -> unit) : unit =
        // Process events in chunks to avoid memory pressure
        let chunkSize = 1000
        let chunks = Array.chunkBySize chunkSize events
        chunks |> Array.iter (Array.iter processor)
    
    /// Command batching for reduced API calls
    let batchCommands (commands: Command array) (maxBatchSize: int) : CommandBatch array =
        let chunks = Array.chunkBySize maxBatchSize commands
        chunks |> Array.mapi (fun i chunk ->
            {
                Commands = chunk
                CommandCount = chunk.Length
                Priority = Array.create chunk.Length 1
                FrameToExecute = Array.create chunk.Length 0<frame>
            })
    
    /// Efficient unit filtering using bit operations
    let filterUnitsByFlags (worldState: WorldState) (flagMask: byte) : int array =
        worldState.UnitIds
        |> Array.zip worldState.UnitStates
        |> Array.filter (fun (state, _) -> (state &&& flagMask) <> 0uy)
        |> Array.map snd

/// Active patterns for F# pattern matching
module ActivePatterns =
    
    /// Pattern match on game phase
    let (|EarlyGame|MidGame|LateGame|) (frame: int<frame>) =
        let frameCount = int frame
        match frameCount with
        | f when f < 1800 -> EarlyGame frame     // First 60 seconds at 30 FPS
        | f when f < 9000 -> MidGame frame       // 1-5 minutes
        | _ -> LateGame frame                    // 5+ minutes
    
    /// Pattern match on resource state
    let (|ResourcePoor|ResourceModerate|ResourceRich|) (resources: ResourceState) =
        let metal = float32 resources.Metal
        let energy = float32 resources.Energy
        match metal, energy with
        | m, e when m < 200.0f || e < 500.0f -> ResourcePoor
        | m, e when m < 1000.0f || e < 2000.0f -> ResourceModerate
        | _ -> ResourceRich
    
    /// Pattern match on unit health
    let (|Critical|Damaged|Healthy|) (unit: UnitInfo) =
        let healthPercent = float32 unit.Health / float32 unit.MaxHealth
        match healthPercent with
        | h when h < 0.2f -> Critical
        | h when h < 0.6f -> Damaged
        | _ -> Healthy
    
    /// Pattern match on unit type
    let (|Commander|Builder|Factory|Combat|Economic|Other|) (unit: UnitInfo) =
        let categories = Set.ofList unit.Categories
        if categories.Contains("COMMANDER") then Commander
        elif categories.Contains("BUILDER") then Builder
        elif categories.Contains("FACTORY") then Factory
        elif categories.Contains("WEAPON") then Combat
        elif categories.Contains("ENERGY") || categories.Contains("METAL") then Economic
        else Other

/// F# computation expression for game commands
type GameCommandBuilder(context: IGameContext) =
    member _.Bind(result: CommandResult, f: string -> CommandResult) : CommandResult =
        match result with
        | Ok value -> f value
        | Error error -> Error error
    
    member _.Return(value: string) : CommandResult =
        Ok value
    
    member _.ReturnFrom(result: CommandResult) : CommandResult =
        result
    
    member _.Zero() : CommandResult =
        Ok "No operation"

/// Async computation for non-blocking operations
module AsyncGameContext =
    
    /// Async resource monitoring
    let getResourcesAsync (context: IGameContext) : Async<ResourceState> =
        async { return context.GetResources() }
    
    /// Async unit operations
    let getUnitsAsync (context: IGameContext) : Async<UnitInfo list> =
        async { return context.GetFriendlyUnits() }
    
    /// Async command execution
    let executeCommandAsync (context: IGameContext) (command: Command) : Async<CommandResult> =
        async { return context.ExecuteCommand(command) }
