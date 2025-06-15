/// Simple command processing for RecoilEngine AI (Simplified version for testing)
namespace RecoilAI.Core

open System
open System.Numerics

/// Native command structure for P/Invoke (matches C++ layout)
[<Struct>]
type NativeCommand = {
    commandType: int                     // Command type enum
    unitId: int                          // Source unit ID
    targetUnitId: int                    // Target unit ID (if applicable)
    x: float32                           // X coordinate
    y: float32                           // Y coordinate  
    z: float32                           // Z coordinate
    buildUnitName: string               // Unit type name for build commands
    priority: int                        // Command priority
}

/// Simple native interop module (mock implementation)
module NativeInterop =
    let executeCommands(commands: NativeCommand array) : int = 
        // Mock implementation - returns number of commands processed
        commands.Length

/// Command processing operations
module Commands =
    
    /// Command validation results
    type ValidationResult = 
        | Valid
        | InvalidUnit of string
        | InvalidPosition of string
        | InvalidTarget of string
    
    /// Simple command validation
    let validateCommand (worldState: WorldState) (command: Command) : ValidationResult =
        match command with
        | Move(unitId, position) ->
            if worldState.FriendlyUnits |> Array.exists (fun u -> u.Id = unitId) then
                if GameContext.Map.isValidPosition worldState.Map position then Valid
                else InvalidPosition "Position outside map"
            else InvalidUnit "Unit not found"
        | Attack(attackerId, targetId) ->
            if worldState.FriendlyUnits |> Array.exists (fun u -> u.Id = attackerId) then
                if worldState.Units |> Array.exists (fun u -> u.Id = targetId) then Valid
                else InvalidTarget "Target not found"
            else InvalidUnit "Attacker not found"
        | _ -> Valid  // Simplified validation for other commands
    
    /// Convert F# command to native format
    let toNativeCommand (command: Command) : NativeCommand =
        match command with
        | Move(unitId, position) ->
            { commandType = 1; unitId = unitId; targetUnitId = -1
              x = position.X; y = position.Y; z = position.Z
              buildUnitName = ""; priority = 5 }
        | Attack(attackerId, targetId) ->
            { commandType = 2; unitId = attackerId; targetUnitId = targetId
              x = 0.0f; y = 0.0f; z = 0.0f
              buildUnitName = ""; priority = 3 }
        | Build(builderId, unitDefId, position) ->
            { commandType = 3; unitId = builderId; targetUnitId = int unitDefId
              x = position.X; y = position.Y; z = position.Z
              buildUnitName = $"unitdef_{unitDefId}"; priority = 4 }
        | Stop(unitId) ->
            { commandType = 4; unitId = unitId; targetUnitId = -1
              x = 0.0f; y = 0.0f; z = 0.0f
              buildUnitName = ""; priority = 0 }
        | _ -> // Default for other commands
            { commandType = 0; unitId = 0; targetUnitId = -1
              x = 0.0f; y = 0.0f; z = 0.0f
              buildUnitName = ""; priority = 10 }
    
    /// Execute command batch with validation
    let executeCommands (worldState: WorldState) (commands: Command array) : CommandBatchResult =
        let startTime = DateTime.UtcNow
        
        // Validate commands
        let validationResults = commands |> Array.map (validateCommand worldState)
        let validCommands = 
            Array.zip commands validationResults
            |> Array.choose (function (cmd, Valid) -> Some cmd | _ -> None)
        
        let errors = 
            Array.zip commands validationResults
            |> Array.choose (function 
                | (_, Valid) -> None
                | (cmd, error) -> Some $"Command {cmd}: {error}")
        
        // Convert to native and execute
        let nativeCommands = validCommands |> Array.map toNativeCommand
        let successCount = NativeInterop.executeCommands nativeCommands
        
        let endTime = DateTime.UtcNow
        let executionTime = (endTime - startTime).TotalMilliseconds
        
        {
            SuccessCount = successCount
            FailureCount = commands.Length - successCount
            ExecutionTimeMs = executionTime
            Errors = errors
        }
