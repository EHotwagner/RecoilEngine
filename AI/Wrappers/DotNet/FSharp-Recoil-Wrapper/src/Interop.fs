/// Native interop for data-oriented Spring AI interface
/// P/Invoke layer that calls the Native C++ Stub Library functions
namespace SpringAI.Core

open System
open System.Runtime.InteropServices
open System.Numerics

/// Native data structures that match the C++ definitions exactly
/// These must have the same memory layout as the C++ structs

[<Struct; StructLayout(LayoutKind.Sequential)>]
type NativeUnit = {
    id: int
    defId: int
    x: float32
    y: float32
    z: float32
    health: float32
    maxHealth: float32
    teamId: int
    state: int
}

[<Struct; StructLayout(LayoutKind.Sequential)>]
type NativeResourceState = {
    metal: float32
    energy: float32
    metalStorage: float32
    energyStorage: float32
    metalIncome: float32
    energyIncome: float32
    currentFrame: int
}

[<Struct; StructLayout(LayoutKind.Sequential)>]
type NativeCommand = {
    commandType: int
    unitId: int
    targetUnitId: int
    x: float32
    y: float32
    z: float32
    buildUnitName: [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)>] string
    priority: int
}

/// Direct P/Invoke declarations to the native C++ stub library
module NativeInterop =
    
    // Core array filling functions - these are the main interface points
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int FillUnitArray(NativeUnit[] units, int maxCount)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int FillResourceState(NativeResourceState& resources)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int ExecuteCommandBatch(NativeCommand[] commands, int commandCount)
    
    // Basic information queries
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnitCount()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float32 GetMetal()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float32 GetEnergy()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetCurrentFrame()
    
    // Spatial queries for efficient AI processing
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnitsInRadius(NativeUnit[] allUnits, int unitCount, 
                               float32 centerX, float32 centerY, float32 centerZ, 
                               float32 radius, int[] resultIds, int maxResults)
    
    // Map information
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float32 GetMapWidth()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float32 GetMapHeight()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern bool IsPositionValid(float32 x, float32 y, float32 z)

/// High-level F# data-oriented interface that wraps the P/Invoke calls
/// Converts between native structs and F# data structures efficiently
module DataOrientedInterop =
    
    /// Convert native unit to F# types with proper units of measure
    let private convertNativeUnit (nativeUnit: NativeUnit) : Unit = {
        Id = nativeUnit.id
        DefId = nativeUnit.defId
        Position = Vector3(nativeUnit.x, nativeUnit.y, nativeUnit.z)
        Health = nativeUnit.health * 1.0f<hp>
        MaxHealth = nativeUnit.maxHealth * 1.0f<hp>
        TeamId = nativeUnit.teamId
        State = enum<UnitState> nativeUnit.state
        Faction = if nativeUnit.teamId = 0 then BARFaction.ARM else BARFaction.COR
    }
    
    /// Convert F# command to native command structure
    let private convertToNativeCommand (command: Command) : NativeCommand =
        match command with
        | Build(builderId, unitDefName, position) ->
            { commandType = 1; unitId = builderId; targetUnitId = -1
              x = position.X; y = position.Y; z = position.Z
              buildUnitName = unitDefName; priority = 1 }
        | Move(unitId, destination) ->
            { commandType = 2; unitId = unitId; targetUnitId = -1
              x = destination.X; y = destination.Y; z = destination.Z
              buildUnitName = ""; priority = 1 }
        | Attack(attackerId, targetId) ->
            { commandType = 3; unitId = attackerId; targetUnitId = targetId
              x = 0.0f; y = 0.0f; z = 0.0f; buildUnitName = ""; priority = 2 }
        | Stop(unitId) ->
            { commandType = 4; unitId = unitId; targetUnitId = -1
              x = 0.0f; y = 0.0f; z = 0.0f; buildUnitName = ""; priority = 1 }
        | Guard(unitId, targetId) ->
            { commandType = 5; unitId = unitId; targetUnitId = targetId
              x = 0.0f; y = 0.0f; z = 0.0f; buildUnitName = ""; priority = 1 }
        | Patrol(unitId, positions) ->
            // For patrol, just use first position for now
            let pos = if positions.Length > 0 then positions.[0] else Vector3.Zero
            { commandType = 6; unitId = unitId; targetUnitId = -1
              x = pos.X; y = pos.Y; z = pos.Z; buildUnitName = ""; priority = 1 }
    
    /// Get complete world state from native code efficiently using direct array filling
    let getWorldState () : WorldState =
        try
            let unitCount = NativeInterop.GetUnitCount()
            
            if unitCount <= 0 then
                // Return empty world state
                {
                    Units = [||]
                    Resources = { Metal = 0.0f<metal>; Energy = 0.0f<energy>
                                 MetalIncome = 0.0f<metal>; EnergyIncome = 0.0f<energy>
                                 CurrentFrame = 0<frame> }
                    MapWidth = 0.0f<elmo>
                    MapHeight = 0.0f<elmo>
                    CurrentFrame = 0<frame>
                }
            else
                // Fill native unit array directly from C++
                let nativeUnits = Array.zeroCreate<NativeUnit> unitCount
                let actualCount = NativeInterop.FillUnitArray(nativeUnits, unitCount)
                
                // Convert to F# units efficiently
                let units = Array.sub nativeUnits 0 actualCount |> Array.map convertNativeUnit
                
                // Get resource state
                let mutable nativeResources = Unchecked.defaultof<NativeResourceState>
                let resourceResult = NativeInterop.FillResourceState(&nativeResources)
                
                let resources = 
                    if resourceResult > 0 then
                        { Metal = nativeResources.metal * 1.0f<metal>
                          Energy = nativeResources.energy * 1.0f<energy>
                          MetalIncome = nativeResources.metalIncome * 1.0f<metal>
                          EnergyIncome = nativeResources.energyIncome * 1.0f<energy>
                          CurrentFrame = nativeResources.currentFrame * 1<frame> }
                    else
                        { Metal = 0.0f<metal>; Energy = 0.0f<energy>
                          MetalIncome = 0.0f<metal>; EnergyIncome = 0.0f<energy>
                          CurrentFrame = 0<frame> }
                
                // Get map information
                let mapWidth = NativeInterop.GetMapWidth() * 1.0f<elmo>
                let mapHeight = NativeInterop.GetMapHeight() * 1.0f<elmo>
                let currentFrame = NativeInterop.GetCurrentFrame() * 1<frame>
                
                {
                    Units = units
                    Resources = resources
                    MapWidth = mapWidth
                    MapHeight = mapHeight
                    CurrentFrame = currentFrame
                }
        with
        | :? DllNotFoundException as ex ->
            failwith $"Native library not found: {ex.Message}. Make sure SpringAIWrapper.dll is in the correct location."
        | ex ->
            failwith $"Error calling native functions: {ex.Message}"
    
    /// Execute command batch efficiently using direct array passing
    let executeCommandBatch (commands: Command array) : CommandBatchResult =
        try
            if commands.Length = 0 then
                { SuccessCount = 0; FailureCount = 0; ExecutionTimeMs = 0.0 }
            else
                let startTime = DateTime.UtcNow
                
                // Convert F# commands to native format
                let nativeCommands = commands |> Array.map convertToNativeCommand
                
                // Execute batch via native call
                let successCount = NativeInterop.ExecuteCommandBatch(nativeCommands, nativeCommands.Length)
                let failureCount = nativeCommands.Length - successCount
                
                let executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
                
                { SuccessCount = successCount; FailureCount = failureCount; ExecutionTimeMs = executionTime }
        with
        | ex ->
            failwith $"Error executing command batch: {ex.Message}"
    
    /// Efficient spatial query using native implementation
    let getUnitsInRadius (worldState: WorldState) (center: Vector3) (radius: float32<elmo>) : Unit array =
        try
            if worldState.Units.Length = 0 then
                [||]
            else
                // Convert F# units back to native format for spatial query
                let nativeUnits = worldState.Units |> Array.map (fun unit ->
                    { id = unit.Id; defId = unit.DefId
                      x = unit.Position.X; y = unit.Position.Y; z = unit.Position.Z
                      health = float32 unit.Health; maxHealth = float32 unit.MaxHealth
                      teamId = unit.TeamId; state = int unit.State })
                
                let maxResults = worldState.Units.Length
                let resultIds = Array.zeroCreate<int> maxResults
                
                let foundCount = NativeInterop.GetUnitsInRadius(
                    nativeUnits, nativeUnits.Length,
                    center.X, center.Y, center.Z, float32 radius,
                    resultIds, maxResults)
                
                // Return the units that were found
                let foundIds = Set.ofArray (Array.sub resultIds 0 foundCount)
                worldState.Units |> Array.filter (fun unit -> foundIds.Contains unit.Id)
        with
        | ex ->
            failwith $"Error in spatial query: {ex.Message}"
    
    /// Validate position using native map bounds check
    let isPositionValid (position: Vector3) : bool =
        try
            NativeInterop.IsPositionValid(position.X, position.Y, position.Z)
        with
        | ex ->
            printfn $"Warning: Error validating position: {ex.Message}"
            false
    
    /// Get basic resource information quickly
    let getResourceSnapshot () : ResourceState =
        try
            { Metal = NativeInterop.GetMetal() * 1.0f<metal>
              Energy = NativeInterop.GetEnergy() * 1.0f<energy>
              MetalIncome = 0.0f<metal>  // Not available in basic query
              EnergyIncome = 0.0f<energy>  // Not available in basic query
              CurrentFrame = NativeInterop.GetCurrentFrame() * 1<frame> }
        with
        | ex ->
            failwith $"Error getting resource snapshot: {ex.Message}"

/// Memory-efficient array pools for reducing allocations during frequent operations
module ArrayPools =
    let private intPool = System.Collections.Concurrent.ConcurrentBag<int array>()
    let private unitPool = System.Collections.Concurrent.ConcurrentBag<Unit array>()
    let private commandPool = System.Collections.Concurrent.ConcurrentBag<Command array>()
    
    /// Get a reusable int array (for spatial queries, etc.)
    let getIntArray (size: int) : int array =
        let mutable array = Unchecked.defaultof<int array>
        if intPool.TryTake(&array) && array.Length >= size then
            Array.Clear(array, 0, array.Length)
            array
        else
            Array.zeroCreate<int> size
    
    /// Return an int array to the pool
    let returnIntArray (array: int array) : unit =
        intPool.Add(array)
    
    /// Get a reusable unit array
    let getUnitArray (size: int) : Unit array =
        let mutable array = Unchecked.defaultof<Unit array>
        if unitPool.TryTake(&array) && array.Length >= size then
            array
        else
            Array.zeroCreate<Unit> size
    
    /// Return a unit array to the pool
    let returnUnitArray (array: Unit array) : unit =
        unitPool.Add(array)
