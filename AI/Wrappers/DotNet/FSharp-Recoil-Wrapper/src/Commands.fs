/// Command processing and batch execution for RecoilEngine AI
namespace RecoilAI.Core

open System
open System.Numerics
open System.Runtime.InteropServices

/// Native interop placeholder (will be implemented in Interop.fs)
module NativeInterop =
    let executeCommands(commands: NativeCommand array) : int = 
        // Placeholder - will be replaced with actual P/Invoke
        commands.Length

/// Command validation, batching, and execution operations
module Commands =
    
    /// Command validation results
    type ValidationResult = 
        | Valid
        | InvalidUnit of string
        | InvalidPosition of string
        | InvalidTarget of string
        | InsufficientResources of string
    
    /// Command execution context
    type ExecutionContext = {
        WorldState: WorldState
        Resources: Resources
        AllowedUnitTypes: int<unitdef> Set
        MaxCommandsPerBatch: int
    }
    
    /// Command validation operations
    module Validation =
        
        /// Validate that a unit exists and belongs to player
        let validateUnit (worldState: WorldState) (unitId: int) : ValidationResult =
            match worldState.FriendlyUnits |> Array.tryFind (fun u -> u.Id = unitId) with
            | Some unit when unit.Health > 0.0f<hp> -> Valid
            | Some _ -> InvalidUnit $"Unit {unitId} is dead"
            | None -> InvalidUnit $"Unit {unitId} not found or not friendly"
        
        /// Validate position is within map bounds
        let validatePosition (worldState: WorldState) (position: Vector3) : ValidationResult =
            if GameContext.Map.isValidPosition worldState.Map position then
                Valid
            else
                InvalidPosition $"Position ({position.X:F1}, {position.Z:F1}) is outside map bounds"
        
        /// Validate target unit exists
        let validateTarget (worldState: WorldState) (targetId: int) : ValidationResult =
            match worldState.Units |> Array.tryFind (fun u -> u.Id = targetId) with
            | Some target when target.Health > 0.0f<hp> -> Valid
            | Some _ -> InvalidTarget $"Target unit {targetId} is dead"
            | None -> InvalidTarget $"Target unit {targetId} not found"
        
        /// Validate build command requirements
        let validateBuild (context: ExecutionContext) (builderId: int) (unitDefId: int<unitdef>) (position: Vector3) : ValidationResult =
            // Check builder exists
            match validateUnit context.WorldState builderId with
            | Valid -> ()
            | error -> error
            |> ignore
            
            // Check position
            match validatePosition context.WorldState position with
            | Valid -> ()
            | error -> error
            |> ignore
            
            // Check if unit type is allowed
            if not (context.AllowedUnitTypes.Contains(unitDefId)) then
                InvalidUnit $"Unit type {unitDefId} is not allowed"
            else
                Valid
        
        /// Validate complete command
        let validateCommand (context: ExecutionContext) (command: Command) : ValidationResult =
            match command with
            | Move(unitId, position) ->
                match validateUnit context.WorldState unitId with
                | Valid -> validatePosition context.WorldState position
                | error -> error
            
            | Attack(attackerId, targetId) ->
                match validateUnit context.WorldState attackerId with
                | Valid -> validateTarget context.WorldState targetId
                | error -> error
            
            | Build(builderId, unitDefId, position) ->
                validateBuild context builderId unitDefId position
            
            | Stop(unitId) | Guard(unitId, _) | Repair(unitId, _) | Reclaim(unitId, _) ->
                validateUnit context.WorldState unitId
            
            | Patrol(unitId, positions) ->
                match validateUnit context.WorldState unitId with
                | Valid ->
                    positions 
                    |> Array.tryFind (fun pos -> validatePosition context.WorldState pos <> Valid)
                    |> function
                        | Some invalidPos -> InvalidPosition $"Invalid patrol position: {invalidPos}"
                        | None -> Valid
                | error -> error
            
            | SetTarget(unitId, position) ->
                match validateUnit context.WorldState unitId with
                | Valid -> validatePosition context.WorldState position
                | error -> error
    
    /// Command batching operations
    module Batching =
        
        /// Group commands by type for optimal execution
        let groupCommandsByType (commands: Command array) : Map<string, Command array> =
            commands
            |> Array.groupBy (function
                | Move _ -> "Move"
                | Attack _ -> "Attack"
                | Build _ -> "Build"
                | Stop _ -> "Stop"
                | Guard _ -> "Guard"
                | Patrol _ -> "Patrol"
                | Reclaim _ -> "Reclaim"
                | Repair _ -> "Repair"
                | SetTarget _ -> "SetTarget")
            |> Map.ofArray
        
        /// Create batches within size limits
        let createBatches (commands: Command array) (maxBatchSize: int) : Command array array =
            if commands.Length <= maxBatchSize then
                [| commands |]
            else
                commands
                |> Array.chunkBySize maxBatchSize
        
        /// Prioritize commands by urgency
        let prioritizeCommands (commands: Command array) (worldState: WorldState) : Command array =
            let getCommandPriority = function
                | Stop _ -> 0                    // Highest priority - immediate action
                | Attack(attackerId, _) ->       // High priority for damaged units
                    match worldState.FriendlyUnits |> Array.tryFind (fun u -> u.Id = attackerId) with
                    | Some unit when float32 unit.Health / float32 unit.MaxHealth < 0.3f -> 1
                    | _ -> 3
                | Repair _ | Reclaim _ -> 2       // High priority - resource/survival
                | Build _ -> 4                   // Medium priority - expansion
                | Move _ | SetTarget _ -> 5       // Lower priority - positioning
                | Guard _ | Patrol _ -> 6         // Lowest priority - defensive
            
            commands
            |> Array.sortBy getCommandPriority
    
    /// Native command structure for P/Invoke (matches C++ layout)
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type NativeCommand = {
        commandType: int                     // Command type enum
        unitId: int                          // Source unit ID
        targetUnitId: int                    // Target unit ID (if applicable)
        x: float32                           // X coordinate
        y: float32                           // Y coordinate  
        z: float32                           // Z coordinate
        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)>]
        buildUnitName: string               // Unit type name for build commands
        priority: int                        // Command priority
        paramCount: int                      // Number of additional parameters
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)>]
        parameters: float32[]                // Additional parameters array
    }
    
    /// Convert F# command to native format
    module Conversion =
        
        /// Convert single command to native format
        let toNativeCommand (command: Command) : NativeCommand =
            match command with
            | Move(unitId, position) ->
                {
                    commandType = 1; unitId = unitId; targetUnitId = -1
                    x = position.X; y = position.Y; z = position.Z
                    buildUnitName = ""; priority = 5; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | Attack(attackerId, targetId) ->
                {
                    commandType = 2; unitId = attackerId; targetUnitId = targetId
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 3; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | Build(builderId, unitDefId, position) ->
                {
                    commandType = 3; unitId = builderId; targetUnitId = int unitDefId
                    x = position.X; y = position.Y; z = position.Z
                    buildUnitName = $"unitdef_{unitDefId}"; priority = 4; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | Stop(unitId) ->
                {
                    commandType = 4; unitId = unitId; targetUnitId = -1
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 0; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | Guard(unitId, targetId) ->
                {
                    commandType = 5; unitId = unitId; targetUnitId = targetId
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 6; paramCount = 0; parameters = Array.zeroCreate 8
                }
              | Patrol(unitId, positions) ->
                let parameters = Array.zeroCreate 8
                for i = 0 to min (positions.Length - 1) 3 do  // Max 4 patrol points
                    if i * 2 + 1 < parameters.Length then
                        parameters.[i * 2] <- positions.[i].X
                        parameters.[i * 2 + 1] <- positions.[i].Z
                
                {
                    commandType = 6; unitId = unitId; targetUnitId = -1
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 6; paramCount = min (positions.Length * 2) 8; parameters = parameters
                }
            
            | Reclaim(reclaimerId, targetId) ->
                {
                    commandType = 7; unitId = reclaimerId; targetUnitId = targetId
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 2; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | Repair(repairerId, targetId) ->
                {
                    commandType = 8; unitId = repairerId; targetUnitId = targetId
                    x = 0.0f; y = 0.0f; z = 0.0f
                    buildUnitName = ""; priority = 2; paramCount = 0; parameters = Array.zeroCreate 8
                }
            
            | SetTarget(unitId, position) ->
                {
                    commandType = 9; unitId = unitId; targetUnitId = -1
                    x = position.X; y = position.Y; z = position.Z
                    buildUnitName = ""; priority = 5; paramCount = 0; parameters = Array.zeroCreate 8
                }
        
        /// Convert command array to native format
        let toNativeCommandArray (commands: Command array) : NativeCommand array =
            commands |> Array.map toNativeCommand
    
    /// High-level command execution interface
    module Execution =
        
        /// Execute command batch with validation
        let executeValidatedBatch (context: ExecutionContext) (commands: Command array) : CommandBatchResult =
            let startTime = DateTime.UtcNow
            
            // Validate all commands first
            let validationResults = commands |> Array.map (Validation.validateCommand context)
            let validCommands = 
                Array.zip commands validationResults
                |> Array.choose (function (cmd, Valid) -> Some cmd | _ -> None)
            
            let errors = 
                Array.zip commands validationResults
                |> Array.choose (function 
                    | (_, Valid) -> None
                    | (cmd, error) -> Some $"Command {cmd}: {error}")
            
            // Convert to native format and execute
            let nativeCommands = Conversion.toNativeCommandArray validCommands
            let executionResult = NativeInterop.executeCommands nativeCommands
            
            let endTime = DateTime.UtcNow
            let executionTime = (endTime - startTime).TotalMilliseconds
            
            {
                SuccessCount = executionResult
                FailureCount = commands.Length - executionResult
                ExecutionTimeMs = executionTime
                Errors = errors
            }
        
        /// Execute commands with automatic batching and prioritization
        let executeCommands (context: ExecutionContext) (commands: Command array) : CommandBatchResult =
            if commands.Length = 0 then
                { SuccessCount = 0; FailureCount = 0; ExecutionTimeMs = 0.0; Errors = [||] }
            else
                // Prioritize and batch commands
                let prioritizedCommands = Batching.prioritizeCommands commands context.WorldState
                let batches = Batching.createBatches prioritizedCommands context.MaxCommandsPerBatch
                  // Execute all batches and combine results
                let results = batches |> Array.map (executeValidatedBatch context)
                  {
                    SuccessCount = results |> Array.sumBy (fun r -> r.SuccessCount)
                    FailureCount = results |> Array.sumBy (fun r -> r.FailureCount)
                    ExecutionTimeMs = results |> Array.sumBy (fun r -> r.ExecutionTimeMs)
                    Errors = results |> Array.collect (fun r -> r.Errors)
                }
