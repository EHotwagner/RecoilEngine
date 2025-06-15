/// Native interop for data-oriented Spring AI interface
namespace SpringAI.Core

open System
open System.Runtime.InteropServices
open DataOrientedTypes

/// Native function declarations for Spring AI interface
module NativeInterop =
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnitCount()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnits([<Out>] int[] unitIds, int maxCount)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnitPositions([<In>] int[] unitIds, [<Out>] float[] positions, int count)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetUnitHealth([<In>] int[] unitIds, [<Out>] float[] health, [<Out>] float[] maxHealth, int count)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int ExecuteCommands([<In>] int[] commandTypes, [<In>] IntPtr[] commandData, int count)
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float GetMetal()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern float GetEnergy()
    
    [<DllImport("SpringAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetCurrentFrame()

/// High-level interop functions for data-oriented access
module DataOrientedInterop =
    
    /// Get complete world state from native code efficiently
    let getWorldState () : WorldState =
        let unitCount = NativeInterop.GetUnitCount()
        
        if unitCount = 0 then
            {
                UnitIds = [||]
                UnitPositions = [||]
                UnitHealth = [||]
                UnitMaxHealth = [||]
                UnitDefIds = [||]
                UnitFactions = [||]
                UnitStates = [||]
                PlayerMetal = [||]
                PlayerEnergy = [||]
                PlayerMetalIncome = [||]
                PlayerEnergyIncome = [||]
                MapHeightData = [||]
                MapMetalData = [||]
                MapSize = (0, 0)
                CurrentFrame = 0<frame>
                EventBatch = [||]
            }
        else
            let unitIds = Array.zeroCreate<int> unitCount
            let actualCount = NativeInterop.GetUnits(unitIds, unitCount)
            
            // Get positions for all units in batch
            let positions = Array.zeroCreate<float> (actualCount * 3)
            let _ = NativeInterop.GetUnitPositions(unitIds, positions, actualCount)
            let unitPositions = 
                Array.chunkBySize 3 positions
                |> Array.map (fun chunk -> System.Numerics.Vector3(chunk.[0], chunk.[1], chunk.[2]))
            
            // Get health data in batch
            let health = Array.zeroCreate<float> actualCount
            let maxHealth = Array.zeroCreate<float> actualCount
            let _ = NativeInterop.GetUnitHealth(unitIds, health, maxHealth, actualCount)
            
            {
                UnitIds = Array.take actualCount unitIds
                UnitPositions = unitPositions
                UnitHealth = health
                UnitMaxHealth = maxHealth
                UnitDefIds = Array.create actualCount 0s // Placeholder
                UnitFactions = Array.create actualCount BARFaction.Unknown
                UnitStates = Array.create actualCount 0uy
                PlayerMetal = [| NativeInterop.GetMetal() |]
                PlayerEnergy = [| NativeInterop.GetEnergy() |]
                PlayerMetalIncome = [| 0.0f |] // Placeholder
                PlayerEnergyIncome = [| 0.0f |] // Placeholder
                MapHeightData = [||] // Placeholder
                MapMetalData = [||] // Placeholder
                MapSize = (256, 256) // Placeholder
                CurrentFrame = NativeInterop.GetCurrentFrame() * 1<frame>
                EventBatch = [||] // Placeholder
            }
    
    /// Execute command batch efficiently
    let executeCommandBatch (commandBatch: CommandBatch) : BatchResult<CommandResult> =
        let startTime = DateTime.UtcNow
        let commandTypes = Array.zeroCreate<int> commandBatch.CommandCount
        let commandData = Array.zeroCreate<IntPtr> commandBatch.CommandCount
        
        // Convert F# commands to native format
        for i in 0 .. commandBatch.CommandCount - 1 do
            match commandBatch.Commands.[i] with
            | Build(builderId, unitDefName, position) ->
                commandTypes.[i] <- 1 // BUILD_COMMAND
                // Marshal command data (simplified)
                commandData.[i] <- IntPtr.Zero
            | Move(unitId, destination) ->
                commandTypes.[i] <- 2 // MOVE_COMMAND
                commandData.[i] <- IntPtr.Zero
            | Attack(attackerId, targetId) ->
                commandTypes.[i] <- 3 // ATTACK_COMMAND
                commandData.[i] <- IntPtr.Zero
            | Stop(unitId) ->
                commandTypes.[i] <- 4 // STOP_COMMAND
                commandData.[i] <- IntPtr.Zero
            | Guard(unitId, targetId) ->
                commandTypes.[i] <- 5 // GUARD_COMMAND
                commandData.[i] <- IntPtr.Zero
            | Patrol(unitId, positions) ->
                commandTypes.[i] <- 6 // PATROL_COMMAND
                commandData.[i] <- IntPtr.Zero
        
        let result = NativeInterop.ExecuteCommands(commandTypes, commandData, commandBatch.CommandCount)
        
        {
            Results = Array.create commandBatch.CommandCount (Ok "Command executed")
            SuccessCount = result
            ErrorCount = commandBatch.CommandCount - result
            ExecutionTime = DateTime.UtcNow - startTime
        }
    
    /// Memory pool for reducing allocations
    type ArrayPool<'T>() =
        let pools = System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentQueue<'T array>>()
        
        member this.GetArray(size: int) : 'T array =
            let queue = pools.GetOrAdd(size, fun _ -> System.Collections.Concurrent.ConcurrentQueue<'T array>())
            let mutable array = Unchecked.defaultof<'T array>
            if queue.TryDequeue(&array) then
                Array.Clear(array, 0, array.Length)
                array
            else
                Array.zeroCreate<'T> size
        
        member this.ReturnArray(array: 'T array) : unit =
            let size = array.Length
            let queue = pools.GetOrAdd(size, fun _ -> System.Collections.Concurrent.ConcurrentQueue<'T array>())
            queue.Enqueue(array)

/// Global array pools for common types
module ArrayPools =
    let intPool = ArrayPool<int>()
    let floatPool = ArrayPool<float32>()
    let vector3Pool = ArrayPool<System.Numerics.Vector3>()
    let commandPool = ArrayPool<Command>()
