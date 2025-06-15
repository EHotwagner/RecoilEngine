/// Basic AI example demonstrating the F# RecoilEngine wrapper
module BasicAI

open RecoilAI.Core
open System.Numerics

/// Simple AI implementation using the data-oriented wrapper
type SimpleRecoilAI() =
    let mutable config = MockInterop.createMockAIConfig()
    let mutable frameCount = 0
    
    /// Main AI decision loop
    member this.ProcessFrame(worldState: WorldState) : Command array =
        frameCount <- frameCount + 1
        
        let commands = ResizeArray<Command>()
        
        // 1. Economic logic: manage builders and construction
        this.ManageEconomy worldState commands
        
        // 2. Military logic: handle combat and unit positioning  
        this.ManageMilitary worldState commands
        
        // 3. Support logic: repair damaged units, reclaim resources
        this.ManageSupport worldState commands
        
        commands.ToArray()
    
    /// Economic management: building and resource optimization
    member private this.ManageEconomy (worldState: WorldState) (commands: ResizeArray<Command>) =
        let resources = worldState.Resources
        let builders = GameContext.Units.getIdleBuilders worldState [| 103<unitdef> |]  // Assume 103 is builder
        
        // Build more economy units if we have resources
        if GameContext.Economy.canAffordUnit resources 80.0f<metal> 0.0f<energy> && builders.Length > 0 then
            // Find safe build location
            let buildPos = this.FindSafeBuildLocation worldState (Vector3(200.0f, 0.0f, 200.0f))
            commands.Add(Build(builders.[0].Id, 101<unitdef>, buildPos))  // Assume 101 is economy unit
        
        // Expand if metal income is high
        if resources.MetalIncome > 20.0f<metal> && builders.Length > 1 then
            let expansionPos = this.FindExpansionLocation worldState
            commands.Add(Build(builders.[1].Id, 102<unitdef>, expansionPos))  // Assume 102 is extractor
    
    /// Military management: combat, positioning, and tactics
    member private this.ManageMilitary (worldState: WorldState) (commands: ResizeArray<Command>) =
        let friendlyUnits = worldState.FriendlyUnits
        let enemyUnits = worldState.EnemyUnits
        
        // Engage nearby enemies
        for friendlyUnit in friendlyUnits do
            if friendlyUnit.State = UnitState.Idle || friendlyUnit.State = UnitState.Moving then
                let nearbyEnemies = GameContext.Spatial.getUnitsInRadius worldState friendlyUnit.Position 300.0f<elmo>
                let enemies = nearbyEnemies.Units |> Array.filter (fun u -> u.TeamId <> 0)
                
                if enemies.Length > 0 then
                    // Attack closest enemy
                    let closestEnemy = 
                        enemies 
                        |> Array.minBy (fun enemy -> Vector3.DistanceSquared(friendlyUnit.Position, enemy.Position))
                    commands.Add(Attack(friendlyUnit.Id, closestEnemy.Id))
        
        // Retreat damaged units
        let damagedUnits = GameContext.Units.getDamagedUnits worldState 0.3f  // Below 30% health
        for damagedUnit in damagedUnits do
            match GameContext.Map.findSafePosition worldState damagedUnit.Position 400.0f<elmo> with
            | Some safePos -> commands.Add(Move(damagedUnit.Id, safePos))
            | None -> commands.Add(Stop(damagedUnit.Id))  // Stop and hope for the best
    
    /// Support operations: repair, reclaim, patrol
    member private this.ManageSupport (worldState: WorldState) (commands: ResizeArray<Command>) =
        let repairers = GameContext.Units.getIdleBuilders worldState [| 103<unitdef> |]
        let damagedFriendlies = GameContext.Units.getDamagedUnits worldState 0.8f  // Below 80% health
        
        // Repair damaged friendly units
        for repairer in repairers do
            let nearbyDamaged = 
                damagedFriendlies
                |> Array.filter (fun unit -> 
                    Vector3.Distance(repairer.Position, unit.Position) < 200.0f)
                |> Array.sortBy (fun unit -> float32 unit.Health / float32 unit.MaxHealth)
            
            if nearbyDamaged.Length > 0 then
                commands.Add(Repair(repairer.Id, nearbyDamaged.[0].Id))
        
        // Set up patrol routes for idle military units
        let idleMilitary = GameContext.Units.getIdleMilitaryUnits worldState [| 101<unitdef>; 102<unitdef> |]
        for unit in idleMilitary |> Array.take (min 3 idleMilitary.Length) do  // Limit patrol units
            let patrolRoute = this.CreatePatrolRoute worldState unit.Position
            if patrolRoute.Length > 1 then
                commands.Add(Patrol(unit.Id, patrolRoute))
    
    /// Find safe location for building construction
    member private this.FindSafeBuildLocation (worldState: WorldState) (preferredPos: Vector3) : Vector3 =
        // Check if preferred position is safe
        let nearbyEnemies = GameContext.Spatial.getUnitsInRadius worldState preferredPos 500.0f<elmo>
        if nearbyEnemies.Units |> Array.exists (fun u -> u.TeamId <> 0) then
            // Find alternative location closer to our base
            let baseCenter = 
                worldState.FriendlyUnits
                |> Array.map (_.Position)
                |> Array.fold (+) Vector3.Zero
                |> fun total -> total / float32 worldState.FriendlyUnits.Length
            
            GameContext.Map.clampToMap worldState.Map (baseCenter + Vector3(50.0f, 0.0f, 50.0f))
        else
            GameContext.Map.clampToMap worldState.Map preferredPos
    
    /// Find good location for economic expansion
    member private this.FindExpansionLocation (worldState: WorldState) : Vector3 =
        // Simple strategy: expand toward map edges away from enemies
        let mapCenter = Vector3(float32 worldState.Map.Width / 2.0f, 0.0f, float32 worldState.Map.Height / 2.0f)
        let avgEnemyPos = 
            if worldState.EnemyUnits.Length > 0 then
                worldState.EnemyUnits
                |> Array.map (_.Position)
                |> Array.fold (+) Vector3.Zero
                |> fun total -> total / float32 worldState.EnemyUnits.Length
            else
                mapCenter
        
        // Move away from average enemy position
        let awayFromEnemies = Vector3.Normalize(mapCenter - avgEnemyPos)
        let expansionPos = mapCenter + awayFromEnemies * 300.0f
        GameContext.Map.clampToMap worldState.Map expansionPos
    
    /// Create patrol route around a central position
    member private this.CreatePatrolRoute (worldState: WorldState) (centerPos: Vector3) : Vector3 array =
        let patrolRadius = 150.0f
        [|
            centerPos + Vector3(patrolRadius, 0.0f, 0.0f)
            centerPos + Vector3(0.0f, 0.0f, patrolRadius)
            centerPos + Vector3(-patrolRadius, 0.0f, 0.0f)
            centerPos + Vector3(0.0f, 0.0f, -patrolRadius)
        |]
        |> Array.map (GameContext.Map.clampToMap worldState.Map)
    
    interface IAI with
        member this.HandleEvent event =
            // Simple event handling - could be expanded
            match event with
            | UnitDestroyed(unitId, teamId) when teamId = 0 ->
                printfn $"Lost friendly unit {unitId}"
            | EnemySighted(unitId, enemyId, position) ->
                printfn $"Unit {unitId} spotted enemy {enemyId} at {position}"
            | _ -> ()
        
        member this.PlanActions worldState =
            this.ProcessFrame worldState
        
        member this.Initialize aiConfig =
            config <- aiConfig
            printfn $"SimpleRecoilAI initialized with config: {aiConfig}"
        
        member this.GetMetrics() =
            MockInterop.createMockMetrics()
        
        member this.Shutdown() =
            printfn "SimpleRecoilAI shutting down"

/// Example program demonstrating the AI in action
module Program =
    
    [<EntryPoint>]
    let main argv =
        printfn "=== F# RecoilEngine Wrapper - Basic AI Example ==="
        printfn ""
        
        // Initialize AI
        let ai = SimpleRecoilAI()
        let config = MockInterop.createMockAIConfig()
        (ai :> IAI).Initialize(config)
        printfn ""
        
        // Simulate several game frames
        let context = MockInterop.MockGameContext(1<frame>)
        
        for frame = 1 to 10 do
            printfn $"--- Frame {frame} ---"
            
            // Get current world state
            let worldState = (context :> GameContext.IGameContext).GetWorldState()
            printfn $"World: {worldState.FriendlyUnits.Length} friendly, {worldState.EnemyUnits.Length} enemy units"
            printfn $"Resources: {worldState.Resources.Metal} metal, {worldState.Resources.Energy} energy"
            
            // Let AI make decisions
            let commands = (ai :> IAI).PlanActions(worldState)
            printfn $"AI generated {commands.Length} commands:"
            
            // Show first few commands
            for i = 0 to min 2 (commands.Length - 1) do
                match commands.[i] with
                | Move(unitId, pos) -> printfn $"  Move unit {unitId} to ({pos.X:F0}, {pos.Z:F0})"
                | Attack(attackerId, targetId) -> printfn $"  Unit {attackerId} attack unit {targetId}"
                | Build(builderId, unitType, pos) -> printfn $"  Unit {builderId} build {unitType} at ({pos.X:F0}, {pos.Z:F0})"
                | Repair(repairerId, targetId) -> printfn $"  Unit {repairerId} repair unit {targetId}"
                | _ -> printfn $"  {commands.[i]}"
            
            if commands.Length > 3 then
                printfn $"  ... and {commands.Length - 3} more commands"
            
            // Execute commands
            if commands.Length > 0 then
                let result = MockInterop.executeCommandBatch commands
                printfn $"Execution: {result.SuccessCount}/{commands.Length} succeeded in {result.ExecutionTimeMs:F1}ms"
                
                // Show any errors
                if result.Errors.Length > 0 then
                    printfn $"Errors: {result.Errors.[0]}"
                    if result.Errors.Length > 1 then
                        printfn $"  ... and {result.Errors.Length - 1} more errors"
            
            context.AdvanceFrame()
            printfn ""
        
        // Show final performance metrics
        let metrics = (ai :> IAI).GetMetrics()
        printfn "Final Performance Metrics:"
        printfn $"  Frame processing time: {metrics.FrameTime:F1}ms"
        printfn $"  Command execution time: {metrics.CommandTime:F1}ms"
        printfn $"  Spatial query time: {metrics.QueryTime:F1}ms"
        printfn $"  Memory usage: {metrics.MemoryUsage / 1024L / 1024L}MB"
        printfn ""
        
        (ai :> IAI).Shutdown()
        
        printfn "✅ Basic AI example completed successfully!"
        printfn ""
        printfn "This example demonstrates:"
        printfn "• Data-oriented AI decision making"
        printfn "• Economic, military, and support logic separation"
        printfn "• Spatial queries for tactical decisions"
        printfn "• Command validation and batch execution"
        printfn "• Performance monitoring and metrics"
        printfn "• Clean separation between AI logic and game interface"
        
        0
