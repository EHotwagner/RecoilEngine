/// F#-first game context interface
namespace SpringAI.Core

open System
open System.Collections.Generic

/// Primary game context interface designed for F#
type IGameContext =
    /// Get current resource state with units of measure
    abstract member GetResources: unit -> ResourceState
    
    /// Get unit information (returns None if unit doesn't exist)
    abstract member GetUnit: int -> UnitInfo option
    
    /// Get all friendly units
    abstract member GetFriendlyUnits: unit -> UnitInfo list
    
    /// Get enemy units in range
    abstract member GetEnemyUnits: unit -> UnitInfo list
    
    /// Execute a command and get result
    abstract member ExecuteCommand: Command -> CommandResult
    
    /// Get unit definition by name
    abstract member GetUnitDef: string -> UnitDefinition option
    
    /// Check if building is possible at location
    abstract member CanBuildAt: Vector3 * string -> bool
    
    /// Get map dimensions
    abstract member GetMapSize: unit -> int * int
    
    /// Get current frame with type safety
    abstract member GetCurrentFrame: unit -> int<frame>

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

/// F# module for working with game context
module GameContext =
    
    /// Safe unit retrieval with detailed error information
    let tryGetUnitWithReason (context: IGameContext) (unitId: int) : Result<UnitInfo, string> =
        match context.GetUnit(unitId) with
        | Some unit when unit.IsAlive -> Ok unit
        | Some unit -> Error $"Unit {unitId} exists but is dead"
        | None -> Error $"Unit {unitId} not found"
    
    /// Get units by faction
    let getUnitsByFaction (context: IGameContext) (faction: BARFaction) : UnitInfo list =
        context.GetFriendlyUnits()
        |> List.filter (fun u -> u.Faction = faction)
    
    /// Get units by category
    let getUnitsByCategory (context: IGameContext) (category: string) : UnitInfo list =
        context.GetFriendlyUnits()
        |> List.filter (fun u -> u.Categories |> List.contains category)
    
    /// Calculate total resource investment
    let calculateResourceInvestment (context: IGameContext) : float32<metal> * float32<energy> =
        let units = context.GetFriendlyUnits()
        let totalMetal = 
            units 
            |> List.sumBy (fun u -> 
                match context.GetUnitDef(u.DefName) with
                | Some def -> float32 def.MetalCost
                | None -> 0.0f) * 1.0f<metal>
        
        let totalEnergy = 
            units 
            |> List.sumBy (fun u -> 
                match context.GetUnitDef(u.DefName) with
                | Some def -> float32 def.EnergyCost
                | None -> 0.0f) * 1.0f<energy>
        
        (totalMetal, totalEnergy)
    
    /// Find closest unit to position
    let findClosestUnit (context: IGameContext) (position: Vector3) : UnitInfo option =
        context.GetFriendlyUnits()
        |> List.sortBy (fun u -> Vector3.Distance(u.Position, position))
        |> List.tryHead
    
    /// Check resource affordability
    let canAfford (context: IGameContext) (metalCost: float32<metal>) (energyCost: float32<energy>) : bool =
        let resources = context.GetResources()
        resources.Metal >= metalCost && resources.Energy >= energyCost

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
