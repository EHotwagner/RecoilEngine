/// Core types for the F# RecoilEngine wrapper
/// This module defines all fundamental data structures for interfacing with RecoilEngine/Spring
/// Uses Structure-of-Arrays (SOA) design for optimal cache performance
namespace RecoilAI.Core

open System
open System.Numerics

/// Units of measure for type safety and documentation
[<Measure>] type elmo        // RecoilEngine length units (1 elmo ≈ 1 meter)
[<Measure>] type frame       // Game simulation frames (30 Hz)
[<Measure>] type hp          // Health points
[<Measure>] type metal       // Metal resource units
[<Measure>] type energy      // Energy resource units
[<Measure>] type unitdef     // Unit definition ID

/// Core unit states as recognized by RecoilEngine
type UnitState =
    | Idle = 0
    | Moving = 1
    | Building = 2
    | Attacking = 3
    | Patrolling = 4
    | Guarding = 5
    | Reclaiming = 6
    | Repairing = 7
    | Retreating = 8

/// Resource information for economic decisions
type Resources = {
    Metal: float32<metal>
    Energy: float32<energy>
    MetalIncome: float32<metal>
    EnergyIncome: float32<energy>
    MetalStorage: float32<metal>
    EnergyStorage: float32<energy>
    CurrentFrame: int<frame>
}

/// Individual unit data (cache-friendly layout)
type Unit = {
    Id: int                          // Unique unit identifier
    DefId: int<unitdef>              // Unit definition ID
    Position: Vector3                // World position (x, y, z)
    Health: float32<hp>              // Current health
    MaxHealth: float32<hp>           // Maximum health
    TeamId: int                      // Owning team (0 = player, >0 = enemies)
    State: UnitState                 // Current unit state
    Velocity: Vector3                // Current velocity vector
    IsBeingBuilt: bool               // Whether unit is under construction
    BuildProgress: float32           // Construction progress (0.0 to 1.0)
}

/// Map information
type MapInfo = {
    Width: float32<elmo>             // Map width in elmo
    Height: float32<elmo>            // Map height in elmo
    MinHeight: float32<elmo>         // Lowest terrain elevation
    MaxHeight: float32<elmo>         // Highest terrain elevation
    Name: string                     // Map name
}

/// Complete world state snapshot (Structure-of-Arrays design)
type WorldState = {
    Units: Unit array                // All units in SOA layout
    FriendlyUnits: Unit array        // Cached friendly units
    EnemyUnits: Unit array           // Cached enemy units  
    Resources: Resources             // Current resource state
    Map: MapInfo                     // Map information
    CurrentFrame: int<frame>         // Current simulation frame
    DeltaTime: float32               // Time since last frame (seconds)
}

/// Command types for unit control
type Command =
    | Move of unitId: int * position: Vector3
    | Attack of attackerId: int * targetId: int
    | Build of builderId: int * unitDefId: int<unitdef> * position: Vector3
    | Stop of unitId: int
    | Guard of unitId: int * targetId: int
    | Patrol of unitId: int * positions: Vector3 array
    | Reclaim of reclaimerId: int * targetId: int
    | Repair of repairerId: int * targetId: int
    | SetTarget of unitId: int * targetPosition: Vector3

/// Result of command batch execution
type CommandBatchResult = {
    SuccessCount: int                // Number of commands executed successfully
    FailureCount: int                // Number of commands that failed
    ExecutionTimeMs: float           // Time taken to execute batch
    Errors: string array             // Error messages for failed commands
}

/// Spatial query results
type SpatialQueryResult = {
    Units: Unit array                // Units found in query
    Count: int                       // Number of units found
    QueryTimeMs: float               // Time taken for query
}

/// Event types from RecoilEngine
type GameEvent =
    | UnitCreated of unit: Unit
    | UnitDestroyed of unitId: int * teamId: int
    | UnitDamaged of unitId: int * damage: float32<hp> * attackerId: int option
    | UnitIdle of unitId: int
    | UnitFinished of unitId: int
    | EnemySighted of unitId: int * enemyId: int * position: Vector3
    | ResourceFound of resourceType: string * position: Vector3 * amount: float32

/// Configuration for the AI wrapper
type AIConfig = {
    MaxUnitsToTrack: int             // Maximum units to track (performance limit)
    SpatialGridSize: int             // Grid size for spatial queries
    BatchCommandSize: int            // Maximum commands per batch
    EnableProfiling: bool            // Enable performance profiling
    LogLevel: string                 // Logging verbosity
}

/// Performance metrics
type PerformanceMetrics = {
    FrameTime: float                 // Time to process one frame
    CommandTime: float               // Time to execute commands
    QueryTime: float                 // Time for spatial queries
    MemoryUsage: int64               // Memory usage in bytes
    GCCollections: int               // Number of GC collections
}

/// Main AI interface for RecoilEngine integration
type IAI =
    abstract member HandleEvent: GameEvent -> unit
    abstract member PlanActions: WorldState -> Command array
    abstract member Initialize: AIConfig -> unit
    abstract member GetMetrics: unit -> PerformanceMetrics
    abstract member Shutdown: unit -> unit
