/// F# type definitions for BAR AI development
/// This module provides F#-friendly types and domain modeling for BAR
module BAR.AI.FSharp.Types

open System

/// Units of measure for type safety
[<Measure>] type frame
[<Measure>] type metal  
[<Measure>] type energy
[<Measure>] type elmo    // BAR distance units
[<Measure>] type dps     // Damage per second
[<Measure>] type hp      // Hit points

/// F#-friendly resource state
type ResourceState = {
    Metal: float32<metal>
    Energy: float32<energy>
    MetalIncome: float32<metal>
    EnergyIncome: float32<energy>
    Frame: int<frame>
}

/// Game phases based on time
type GamePhase = 
    | EarlyGame of frame: int<frame>
    | MidGame of frame: int<frame>
    | LateGame of frame: int<frame>

/// AI strategies
type Strategy =
    | EconomicExpansion
    | MilitaryBuildup  
    | TechAdvancement
    | DefensivePosition
    | AttackMode of target: Vector3

/// Unit classification for F# pattern matching
type UnitClassification =
    | Commander of health: float32<hp> * maxHealth: float32<hp>
    | Builder of buildSpeed: float32 * metalCost: float32<metal>
    | Factory of productionType: string * efficiency: float32
    | CombatUnit of damage: float32<dps> * range: float32<elmo>
    | EconomicUnit of resourceType: ResourceType * efficiency: float32
    | Unknown

and ResourceType = 
    | MetalExtractor 
    | EnergyGenerator
    | Storage

/// Position information with type safety
[<Struct>]
type Position = {
    X: float32<elmo>
    Y: float32<elmo> 
    Z: float32<elmo>
}

/// Vector operations for positions
module Position =
    let zero = { X = 0.0f<elmo>; Y = 0.0f<elmo>; Z = 0.0f<elmo> }
    
    let distance pos1 pos2 =
        let dx = pos1.X - pos2.X
        let dy = pos1.Y - pos2.Y
        let dz = pos1.Z - pos2.Z
        sqrt(float32(dx*dx + dy*dy + dz*dz)) * 1.0f<elmo>
    
    let fromVector3 (v: Vector3) : Position =
        { X = v.X * 1.0f<elmo>; Y = v.Y * 1.0f<elmo>; Z = v.Z * 1.0f<elmo> }
    
    let toVector3 (pos: Position) : Vector3 =
        Vector3(float32 pos.X, float32 pos.Y, float32 pos.Z)

/// F#-friendly unit information
type UnitInfo = {
    Id: int
    DefId: int
    DefName: string
    Classification: UnitClassification
    Position: Position
    Health: float32<hp>
    MaxHealth: float32<hp>
    Categories: string list
    Team: int
}

/// Command results for railway-oriented programming
type CommandResult<'T> = 
    | Success of 'T
    | Failure of string

/// Command execution context
type CommandContext = {
    Callback: SpringAI.IGameCallback
    CurrentFrame: int<frame>
    Resources: ResourceState
}

/// Build order step definitions
type BuildCondition =
    | MetalReaches of float32<metal>
    | EnergyReaches of float32<energy>
    | FrameReaches of int<frame>
    | UnitsOfType of unitDefName: string * count: int
    | CustomCondition of (CommandContext -> bool)

type BuildStep =
    | BuildUnit of unitDefName: string * count: int * priority: int
    | WaitFor of BuildCondition
    | Parallel of BuildStep list
    | Sequential of BuildStep list
    | Conditional of condition: (CommandContext -> bool) * ifTrue: BuildStep * ifFalse: BuildStep option

/// AI decision result
type Decision<'T> = {
    Action: 'T
    Priority: int
    Reason: string
    RequiredResources: ResourceState option
}

/// Threat assessment
type ThreatLevel = Low | Medium | High | Critical

type ThreatInfo = {
    Level: ThreatLevel
    Source: Position
    Distance: float32<elmo>
    EstimatedDamage: float32<dps>
    UnitCount: int
}
