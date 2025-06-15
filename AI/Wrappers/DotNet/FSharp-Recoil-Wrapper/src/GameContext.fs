/// Game context and spatial query operations for RecoilEngine AI
namespace RecoilAI.Core

open System
open System.Numerics
open System.Runtime.InteropServices

/// High-level interface for accessing game world state
module GameContext =
    
    /// Interface for querying game world state
    type IGameContext =
        abstract member GetWorldState: unit -> WorldState
        abstract member GetResources: unit -> Resources
        abstract member GetMapInfo: unit -> MapInfo
        abstract member GetCurrentFrame: unit -> int<frame>
        abstract member GetDeltaTime: unit -> float32

    /// Spatial partitioning and query operations
    module Spatial =
        
        /// Grid-based spatial partitioning for efficient unit queries
        type SpatialGrid = {
            GridSize: int                    // Grid cells per side
            CellSize: float32<elmo>          // Size of each grid cell
            UnitGrid: int list array array   // Grid of unit IDs
        }
        
        /// Create spatial grid from world state
        let createSpatialGrid (worldState: WorldState) (gridSize: int) : SpatialGrid =
            let cellSize = max worldState.Map.Width worldState.Map.Height / float32 gridSize
            let grid = Array.init gridSize (fun _ -> Array.init gridSize (fun _ -> []))
            
            // Populate grid with unit IDs
            for unit in worldState.Units do
                let cellX = int (unit.Position.X / float32 cellSize) |> min (gridSize - 1) |> max 0
                let cellY = int (unit.Position.Z / float32 cellSize) |> min (gridSize - 1) |> max 0
                grid.[cellX].[cellY] <- unit.Id :: grid.[cellX].[cellY]
            
            { GridSize = gridSize; CellSize = cellSize; UnitGrid = grid }
        
        /// Get units within radius using spatial grid (optimized)
        let getUnitsInRadius (worldState: WorldState) (center: Vector3) (radius: float32<elmo>) : SpatialQueryResult =
            let startTime = DateTime.UtcNow
            
            let radiusSquared = (float32 radius) * (float32 radius)
            let unitsInRadius = 
                worldState.Units
                |> Array.filter (fun unit ->
                    let distance = Vector3.DistanceSquared(center, unit.Position)
                    distance <= radiusSquared)
            
            let endTime = DateTime.UtcNow
            let queryTime = (endTime - startTime).TotalMilliseconds
            
            {
                Units = unitsInRadius
                Count = unitsInRadius.Length
                QueryTimeMs = queryTime
            }
        
        /// Get nearest unit to a position (optionally filtered by team)
        let getNearestUnit (worldState: WorldState) (position: Vector3) (teamFilter: int option) : Unit option =
            let candidateUnits = 
                match teamFilter with
                | Some teamId -> worldState.Units |> Array.filter (fun u -> u.TeamId = teamId)
                | None -> worldState.Units
            
            if candidateUnits.Length = 0 then
                None
            else
                candidateUnits
                |> Array.minBy (fun unit -> Vector3.DistanceSquared(position, unit.Position))
                |> Some
        
        /// Get all units in rectangular area
        let getUnitsInArea (worldState: WorldState) (minCorner: Vector3) (maxCorner: Vector3) : SpatialQueryResult =
            let startTime = DateTime.UtcNow
            
            let unitsInArea = 
                worldState.Units
                |> Array.filter (fun unit ->
                    unit.Position.X >= minCorner.X && unit.Position.X <= maxCorner.X &&
                    unit.Position.Z >= minCorner.Z && unit.Position.Z <= maxCorner.Z)
            
            let endTime = DateTime.UtcNow
            let queryTime = (endTime - startTime).TotalMilliseconds
            
            {
                Units = unitsInArea
                Count = unitsInArea.Length
                QueryTimeMs = queryTime
            }
        
        /// Get units of specific type within radius
        let getUnitsOfTypeInRadius (worldState: WorldState) (center: Vector3) (radius: float32<elmo>) (unitDefId: int<unitdef>) : Unit array =
            (getUnitsInRadius worldState center radius).Units
            |> Array.filter (fun unit -> unit.DefId = unitDefId)
        
        /// Get enemy units within threat range
        let getThreateningEnemies (worldState: WorldState) (position: Vector3) (threatRadius: float32<elmo>) : Unit array =
            worldState.EnemyUnits
            |> Array.filter (fun enemy ->
                let distance = Vector3.Distance(position, enemy.Position)
                distance <= float32 threatRadius && 
                (enemy.State = UnitState.Attacking || enemy.State = UnitState.Patrolling))

    /// Economic analysis operations
    module Economy =
        
        /// Calculate resource efficiency metrics
        let calculateResourceEfficiency (resources: Resources) : float32 =
            let metalEfficiency = 
                if resources.MetalIncome > 0.0f<metal> then
                    min 1.0f (float32 resources.Metal / float32 resources.MetalIncome / 30.0f) // 30 frames = 1 second
                else 0.0f
            
            let energyEfficiency = 
                if resources.EnergyIncome > 0.0f<energy> then
                    min 1.0f (float32 resources.Energy / float32 resources.EnergyIncome / 30.0f)
                else 0.0f
            
            (metalEfficiency + energyEfficiency) / 2.0f
        
        /// Check if we can afford a unit build
        let canAffordUnit (resources: Resources) (metalCost: float32<metal>) (energyCost: float32<energy>) : bool =
            resources.Metal >= metalCost && resources.Energy >= energyCost
        
        /// Predict resources after N frames
        let predictResources (resources: Resources) (frames: int<frame>) : Resources =
            let frameCount = float32 frames
            {
                resources with
                    Metal = min resources.MetalStorage (resources.Metal + resources.MetalIncome * frameCount)
                    Energy = min resources.EnergyStorage (resources.Energy + resources.EnergyIncome * frameCount)
                    CurrentFrame = resources.CurrentFrame + frames
            }

    /// Unit analysis operations
    module Units =
        
        /// Get units by state
        let getUnitsByState (worldState: WorldState) (state: UnitState) : Unit array =
            worldState.Units |> Array.filter (fun unit -> unit.State = state)
        
        /// Get damaged units (below certain health percentage)
        let getDamagedUnits (worldState: WorldState) (healthThreshold: float32) : Unit array =
            worldState.Units
            |> Array.filter (fun unit ->
                let healthPercentage = float32 unit.Health / float32 unit.MaxHealth
                healthPercentage < healthThreshold)
        
        /// Get idle military units
        let getIdleMilitaryUnits (worldState: WorldState) (militaryDefIds: int<unitdef> array) : Unit array =
            worldState.FriendlyUnits
            |> Array.filter (fun unit ->
                unit.State = UnitState.Idle && 
                Array.contains unit.DefId militaryDefIds)
        
        /// Get builders that are not currently building
        let getIdleBuilders (worldState: WorldState) (builderDefIds: int<unitdef> array) : Unit array =
            worldState.FriendlyUnits
            |> Array.filter (fun unit ->
                Array.contains unit.DefId builderDefIds &&
                unit.State <> UnitState.Building)
        
        /// Calculate army strength in area
        let calculateArmyStrength (units: Unit array) : float32 =
            units
            |> Array.sumBy (fun unit -> float32 unit.MaxHealth)

    /// Map analysis operations  
    module Map =
        
        /// Check if position is valid on map
        let isValidPosition (mapInfo: MapInfo) (position: Vector3) : bool =
            position.X >= 0.0f && position.X <= float32 mapInfo.Width &&
            position.Z >= 0.0f && position.Z <= float32 mapInfo.Height
        
        /// Clamp position to map boundaries
        let clampToMap (mapInfo: MapInfo) (position: Vector3) : Vector3 =
            Vector3(
                Math.Max(0.0f, Math.Min(float32 mapInfo.Width, position.X)),
                position.Y,
                Math.Max(0.0f, Math.Min(float32 mapInfo.Height, position.Z))
            )
        
        /// Find safe position away from enemies
        let findSafePosition (worldState: WorldState) (referencePos: Vector3) (safeDistance: float32<elmo>) : Vector3 option =
            let mapCenter = Vector3(float32 worldState.Map.Width / 2.0f, 0.0f, float32 worldState.Map.Height / 2.0f)            // Simple approach: move towards map center away from enemies
            let safeDist = float32 safeDistance
            let searchRadius = safeDist * 2.0f
            let nearbyEnemies = (Spatial.getUnitsInRadius worldState referencePos (searchRadius * 1.0f<elmo>)).Units
            if nearbyEnemies.Length = 0 then
                Some referencePos
            else
                let escapeDirection = Vector3.Normalize(mapCenter - referencePos)
                let safePos = referencePos + escapeDirection * safeDist
                Some (clampToMap worldState.Map safePos)
