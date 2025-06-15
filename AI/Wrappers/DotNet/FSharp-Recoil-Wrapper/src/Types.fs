/// Core F# types for SpringAI wrapper
/// Designed F#-first with data-oriented, array-based approach for high-performance AI
namespace SpringAI.Core

open System
open System.Numerics
open System.Runtime.InteropServices

/// Units of measure for type safety
[<Measure>] type metal
[<Measure>] type energy  
[<Measure>] type frame
[<Measure>] type elmo    // BAR distance units
[<Measure>] type hp      // Hit points
[<Measure>] type dps     // Damage per second

/// Array-based data structures for high-performance AI processing
/// These align with data-oriented design principles for efficient cache usage

/// BAR faction enumeration
type BARFaction = 
    | ARM = 1
    | COR = 2
    | Unknown = 0

/// World state arrays for data-oriented processing
/// These represent the complete game state in array form for efficient batch operations
[<Struct>]
type WorldState = {
    /// All units in SOA (Structure of Arrays) format for cache efficiency
    UnitIds: int array
    UnitPositions: Vector3 array
    UnitHealth: float32 array
    UnitMaxHealth: float32 array
    UnitDefIds: int array
    UnitFactions: BARFaction array
    UnitStates: byte array  // Packed state flags (alive, moving, attacking, etc.)
    
    /// Resource state arrays for all players
    PlayerMetal: float32 array
    PlayerEnergy: float32 array
    PlayerMetalIncome: float32 array
    PlayerEnergyIncome: float32 array
    
    /// Map data arrays
    MapHeightData: float32 array
    MapMetalData: float32 array
    MapSize: int * int
    
    /// Current simulation frame
    CurrentFrame: int<frame>
    
    /// Event batches for this frame
    EventBatch: GameEvent array
}

/// Event batch for efficient processing
[<Struct>]
type EventBatch = {
    Events: GameEvent array
    EventCount: int
    FrameNumber: int<frame>
    Timestamp: DateTimeOffset
}

/// Core game events - optimized for batch processing
type GameEvent =
    | GameStarted of aiId: int * savedGame: bool
    | FrameUpdate of frame: int<frame>
    | UnitCreated of unitId: int * builderId: int * frame: int<frame>
    | UnitDamaged of unitId: int * attackerId: int * damage: float32 * frame: int<frame>
    | UnitDestroyed of unitId: int * attackerId: int * frame: int<frame>
    | GameEnded of reason: int

/// Command batch for efficient command execution
[<Struct>]
type CommandBatch = {
    Commands: Command array
    CommandCount: int
    Priority: int array
    FrameToExecute: int<frame> array
}

/// Spatial partitioning data for efficient queries
[<Struct>]
type SpatialGrid = {
    GridSize: int
    CellSize: float32<elmo>
    UnitCells: int array array  // Array of unit IDs per grid cell
    MapBounds: Vector3 * Vector3
}

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
/// Build order DSL types with array support
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

/// Decision result for AI planning with batch support
type Decision<'T> = {
    Action: 'T
    Priority: int
    Reason: string
    RequiredResources: ResourceState option
    EstimatedDuration: int<frame> option
}

/// High-performance batch processing types
module DataOrientedTypes =
    
    /// Compact unit representation for array processing
    [<Struct>]
    type CompactUnit = {
        Id: int
        DefId: int16
        X: float32
        Y: float32
        Z: float32
        Health: float32
        MaxHealth: float32
        Faction: BARFaction
        StateFlags: byte  // Packed boolean states
    }
    
    /// Efficient spatial query results
    [<Struct>]
    type SpatialQueryResult = {
        UnitIds: int array
        Distances: float32 array
        Count: int
    }
    
    /// Batch operation results
    [<Struct>]
    type BatchResult<'T> = {
        Results: 'T array
        SuccessCount: int
        ErrorCount: int
        ExecutionTime: TimeSpan
    }
    
    /// Memory pool for reducing allocations
    type ArrayPool<'T> = {
        GetArray: int -> 'T array
        ReturnArray: 'T array -> unit
    }

/// Performance monitoring types
type PerformanceMetrics = {
    FrameTime: TimeSpan
    EventProcessingTime: TimeSpan
    CommandGenerationTime: TimeSpan
    BatchSize: int
    AllocatedMemory: int64
}

/// Unit state enumeration for efficient state tracking
type UnitState =
    | Idle = 0
    | Moving = 1
    | Building = 2
    | Attacking = 3
    | Repairing = 4
    | Reclaiming = 5
    | Patrolling = 6
    | Guarding = 7

/// Individual unit representation for data-oriented processing
[<Struct>]
type Unit = {
    Id: int
    DefId: int
    Position: Vector3
    Health: float32<hp>
    MaxHealth: float32<hp>
    TeamId: int
    State: UnitState
    Faction: BARFaction
}

/// Simplified world state for data-oriented processing
[<Struct>]
type WorldState = {
    Units: Unit array
    Resources: ResourceState
    MapWidth: float32<elmo>
    MapHeight: float32<elmo>
    CurrentFrame: int<frame>
}

/// Command batch execution result
[<Struct>]
type CommandBatchResult = {
    SuccessCount: int
    FailureCount: int
    ExecutionTimeMs: float
}
