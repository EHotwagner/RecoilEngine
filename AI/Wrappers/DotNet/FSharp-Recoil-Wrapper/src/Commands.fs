/// F# command definitions and execution with data-oriented support
namespace SpringAI.Core

open System
open System.Numerics
open DataOrientedTypes

/// Command execution result with detailed information
type CommandResult = Result<string, string>

/// Command batch execution for high performance
module CommandExecution =
    
    /// Execute a single command through game context
    let executeCommand (context: IGameContext) (command: Command) : CommandResult =
        context.ExecuteCommand command
    
    /// Execute command batch efficiently
    let executeCommandBatch (context: IGameContext) (commandBatch: CommandBatch) : BatchResult<CommandResult> =
        let startTime = DateTime.UtcNow
        let results = ResizeArray<CommandResult>()
        let mutable successCount = 0
        let mutable errorCount = 0
        
        for command in commandBatch.Commands do
            match context.ExecuteCommand command with
            | Ok msg ->
                results.Add(Ok msg)
                successCount <- successCount + 1
            | Error err ->
                results.Add(Error err)
                errorCount <- errorCount + 1
        
        {
            Results = results.ToArray()
            SuccessCount = successCount
            ErrorCount = errorCount
            ExecutionTime = DateTime.UtcNow - startTime
        }
    
    /// Validate commands before execution
    let validateCommands (context: IGameContext) (commands: Command array) : (Command * bool) array =
        commands |> Array.map (fun cmd ->
            let isValid = 
                match cmd with
                | Build(builderId, unitDefName, position) ->
                    context.CanBuildAt(position, unitDefName)
                | Move(unitId, destination) ->
                    match context.GetUnit(unitId) with
                    | Some unit -> unit.IsAlive
                    | None -> false
                | Attack(attackerId, targetId) ->
                    match context.GetUnit(attackerId), context.GetUnit(targetId) with
                    | Some attacker, Some target -> attacker.IsAlive && target.IsAlive
                    | _ -> false
                | Stop(unitId) | Guard(unitId, _) ->
                    match context.GetUnit(unitId) with
                    | Some unit -> unit.IsAlive
                    | None -> false
                | Patrol(unitId, positions) ->
                    match context.GetUnit(unitId) with
                    | Some unit -> unit.IsAlive && not (List.isEmpty positions)
                    | None -> false
            
            (cmd, isValid))

/// F# computation expression for command building
type CommandBuilder(context: IGameContext) =
    
    member _.Bind(result: CommandResult, continuation: string -> CommandResult) : CommandResult =
        match result with
        | Ok value -> continuation value
        | Error error -> Error error
    
    member _.Return(command: Command) : CommandResult =
        context.ExecuteCommand command
    
    member _.ReturnFrom(result: CommandResult) : CommandResult =
        result
    
    member _.Zero() : CommandResult =
        Ok "No operation"
    
    member _.Delay(f: unit -> CommandResult) = f
    member _.Run(f: unit -> CommandResult) = f()

/// Command planning and optimization
module CommandPlanning =
    
    /// Optimize command order for efficiency
    let optimizeCommandOrder (commands: Command array) : Command array =
        // Group commands by type for batch execution
        let buildCommands = commands |> Array.filter (function Build _ -> true | _ -> false)
        let moveCommands = commands |> Array.filter (function Move _ -> true | _ -> false)
        let attackCommands = commands |> Array.filter (function Attack _ -> true | _ -> false)
        let otherCommands = commands |> Array.filter (function 
            | Build _ | Move _ | Attack _ -> false | _ -> true)
        
        // Execute in optimal order: builds first, then moves, then attacks
        Array.concat [buildCommands; moveCommands; attackCommands; otherCommands]
    
    /// Plan construction sequence using spatial optimization
    let planConstructionSequence (context: IGameContext) (buildRequests: (string * Vector3) array) : Command array =
        let builders = context.GetFriendlyUnitsArray()
                      |> Array.filter (fun unit -> (* check if builder *) true) // Simplified
        
        if Array.isEmpty builders then [||]
        else
            // Assign build tasks to nearest builders
            buildRequests
            |> Array.mapi (fun i (unitDefName, position) ->
                let nearestBuilder = 
                    builders 
                    |> Array.minBy (fun builder -> 
                        Vector3.Distance(Vector3(builder.X, builder.Y, builder.Z), position))
                
                Build(nearestBuilder.Id, unitDefName, position))
    
    /// Generate patrol routes for military units
    let generatePatrolRoutes (worldState: WorldState) (unitIds: int array) : Command array =
        unitIds
        |> Array.choose (fun unitId ->
            let unitIndex = Array.tryFindIndex ((=) unitId) worldState.UnitIds
            match unitIndex with
            | Some index ->
                let unitPos = worldState.UnitPositions.[index]
                let patrolPoints = [
                    unitPos
                    Vector3(unitPos.X + 200.0f, unitPos.Y, unitPos.Z)
                    Vector3(unitPos.X + 200.0f, unitPos.Y, unitPos.Z + 200.0f)
                    Vector3(unitPos.X, unitPos.Y, unitPos.Z + 200.0f)
                ]
                Some (Patrol(unitId, patrolPoints))
            | None -> None)
