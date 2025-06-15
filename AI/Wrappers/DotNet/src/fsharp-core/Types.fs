/// Core F# types for SpringAI wrapper
/// Designed F#-first with C# compatibility as secondary concern
namespace SpringAI.Core

open System
open System.Numerics

/// Units of measure for type safety
[<Measure>] type metal
[<Measure>] type energy  
[<Measure>] type frame
[<Measure>] type elmo    // BAR distance units
[<Measure>] type hp      // Hit points
[<Measure>] type dps     // Damage per second

/// BAR faction enumeration
type BARFaction = 
    | ARM = 1
    | COR = 2
    | Unknown = 0

/// Core game events - primary F# API
type GameEvent =
    | GameStarted of aiId: int * savedGame: bool
    | FrameUpdate of frame: int<frame>
    | UnitCreated of unitId: int * builderId: int * frame: int<frame>
    | UnitDamaged of unitId: int * attackerId: int * damage: float32 * frame: int<frame>
    | UnitDestroyed of unitId: int * attackerId: int * frame: int<frame>
    | GameEnded of reason: int

/// Resource state with units of measure
type ResourceState = {
    Metal: float32<metal>
    Energy: float32<energy>
    MetalIncome: float32<metal>
    EnergyIncome: float32<energy>
    CurrentFrame: int<frame>
}

/// Unit information with F# types
type UnitInfo = {
    Id: int
    DefId: int
    DefName: string
    Position: Vector3
    Health: float32<hp>
    MaxHealth: float32<hp>
    Faction: BARFaction
    Categories: string list
    IsAlive: bool
}

/// Command types designed for F# pattern matching
type Command =
    | Build of builderId: int * unitDefName: string * position: Vector3
    | Move of unitId: int * destination: Vector3
    | Attack of attackerId: int * targetId: int
    | Stop of unitId: int
    | Guard of unitId: int * targetId: int
    | Patrol of unitId: int * positions: Vector3 list

/// Command execution result
type CommandResult = Result<string, string>

/// Strategy types for AI decision making
type Strategy =
    | EconomicExpansion
    | MilitaryBuildup
    | TechAdvancement 
    | DefensivePosition
    | AttackMode of target: Vector3

/// Game phase classification
type GamePhase =
    | EarlyGame of frame: int<frame>
    | MidGame of frame: int<frame>
    | LateGame of frame: int<frame>

/// Threat assessment
type ThreatLevel = 
    | None = 0
    | Low = 1
    | Medium = 2
    | High = 3
    | Critical = 4

type ThreatAssessment = {
    Level: ThreatLevel
    Source: Vector3 option
    Distance: float32<elmo>
    EstimatedDamage: float32<dps>
    UnitCount: int
}

/// Build order DSL types
type BuildCondition =
    | MetalReaches of float32<metal>
    | EnergyReaches of float32<energy>
    | FrameReaches of int<frame>
    | UnitsOfType of unitDefName: string * count: int
    | CustomCondition of (ResourceState -> bool)

type BuildStep =
    | BuildUnit of unitDefName: string * count: int * priority: int
    | WaitFor of BuildCondition
    | Parallel of BuildStep list
    | Sequential of BuildStep list
    | Conditional of condition: (ResourceState -> bool) * ifTrue: BuildStep * ifFalse: BuildStep option

/// Decision result for AI planning
type Decision<'T> = {
    Action: 'T
    Priority: int
    Reason: string
    RequiredResources: ResourceState option
    EstimatedDuration: int<frame> option
}
