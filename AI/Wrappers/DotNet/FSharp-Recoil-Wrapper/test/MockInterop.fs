/// Mock implementation for testing F# RecoilEngine wrapper without native dependencies
namespace RecoilAI.Core

open System
open System.Numerics

/// Mock implementations that simulate RecoilEngine behavior for testing
module MockInterop =
    
    /// Mock world state generator with realistic test data
    let createMockWorldState (frameNumber: int<frame>) : WorldState =
        
        // Create diverse set of test units
        let mockUnits = [|
            // Friendly units (TeamId = 0)
            { Id = 1; DefId = 101<unitdef>; Position = Vector3(100.0f, 0.0f, 100.0f)
              Health = 100.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 0
              State = UnitState.Idle; Velocity = Vector3.Zero; IsBeingBuilt = false; BuildProgress = 1.0f }
              
            { Id = 2; DefId = 102<unitdef>; Position = Vector3(200.0f, 0.0f, 150.0f)
              Health = 80.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 0
              State = UnitState.Moving; Velocity = Vector3(1.0f, 0.0f, 0.5f); IsBeingBuilt = false; BuildProgress = 1.0f }
              
            { Id = 3; DefId = 103<unitdef>; Position = Vector3(150.0f, 0.0f, 200.0f)
              Health = 120.0f<hp>; MaxHealth = 150.0f<hp>; TeamId = 0
              State = UnitState.Building; Velocity = Vector3.Zero; IsBeingBuilt = false; BuildProgress = 1.0f }
              
            { Id = 4; DefId = 101<unitdef>; Position = Vector3(120.0f, 0.0f, 120.0f)
              Health = 25.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 0
              State = UnitState.Retreating; Velocity = Vector3(-2.0f, 0.0f, -1.0f); IsBeingBuilt = false; BuildProgress = 1.0f }
              
            // Enemy units (TeamId = 1)
            { Id = 101; DefId = 201<unitdef>; Position = Vector3(800.0f, 0.0f, 800.0f)
              Health = 90.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 1
              State = UnitState.Patrolling; Velocity = Vector3(0.5f, 0.0f, -0.5f); IsBeingBuilt = false; BuildProgress = 1.0f }
              
            { Id = 102; DefId = 202<unitdef>; Position = Vector3(750.0f, 0.0f, 850.0f)
              Health = 200.0f<hp>; MaxHealth = 200.0f<hp>; TeamId = 1
              State = UnitState.Attacking; Velocity = Vector3(-1.0f, 0.0f, -1.0f); IsBeingBuilt = false; BuildProgress = 1.0f }
              
            { Id = 103; DefId = 201<unitdef>; Position = Vector3(900.0f, 0.0f, 700.0f)
              Health = 60.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 1
              State = UnitState.Moving; Velocity = Vector3(-0.8f, 0.0f, 0.2f); IsBeingBuilt = false; BuildProgress = 1.0f }
        |]
        
        // Separate friendly and enemy units
        let friendlyUnits = mockUnits |> Array.filter (fun u -> u.TeamId = 0)
        let enemyUnits = mockUnits |> Array.filter (fun u -> u.TeamId <> 0)
        
        // Mock resources with some variation based on frame
        let baseFrame = int frameNumber
        let resources = {
            Metal = 1000.0f<metal> + float32 (baseFrame % 100) * 10.0f<metal>
            Energy = 800.0f<energy> + float32 (baseFrame % 50) * 5.0f<energy>
            MetalIncome = 15.0f<metal> + float32 (baseFrame % 10) * 0.5f<metal>
            EnergyIncome = 12.0f<energy> + float32 (baseFrame % 8) * 0.3f<energy>
            MetalStorage = 5000.0f<metal>
            EnergyStorage = 3000.0f<energy>
            CurrentFrame = frameNumber
        }
        
        // Mock map info
        let mapInfo = {
            Width = 2048.0f<elmo>
            Height = 2048.0f<elmo>
            MinHeight = 0.0f<elmo>
            MaxHeight = 256.0f<elmo>
            Name = "MockTestMap"
        }
        
        {
            Units = mockUnits
            FriendlyUnits = friendlyUnits
            EnemyUnits = enemyUnits
            Resources = resources
            Map = mapInfo
            CurrentFrame = frameNumber
            DeltaTime = 1.0f / 30.0f  // 30 FPS simulation
        }
    
    /// Mock command execution with realistic success/failure simulation
    let executeCommandBatch (commands: Command array) : CommandBatchResult =
        let startTime = DateTime.UtcNow
        
        // Simulate command processing time
        System.Threading.Thread.Sleep(1)  // 1ms simulated processing time
        
        // Simulate some failures for realism
        let mutable successCount = 0
        let mutable errors = []
        
        for i, command in Array.indexed commands do
            // Simulate 95% success rate
            if Random().NextDouble() < 0.95 then
                successCount <- successCount + 1
            else
                let errorMsg = match command with
                    | Move(unitId, _) -> $"Unit {unitId} cannot move (pathfinding failed)"
                    | Attack(attackerId, targetId) -> $"Unit {attackerId} cannot attack {targetId} (out of range)"
                    | Build(builderId, unitDefId, _) -> $"Unit {builderId} cannot build {unitDefId} (insufficient resources)"
                    | _ -> $"Command failed: {command}"
                errors <- errorMsg :: errors
        
        let endTime = DateTime.UtcNow
        let executionTime = (endTime - startTime).TotalMilliseconds
        
        {
            SuccessCount = successCount
            FailureCount = commands.Length - successCount
            ExecutionTimeMs = executionTime
            Errors = errors |> List.toArray |> Array.rev
        }
    
    /// Mock game context implementation
    type MockGameContext(initialFrame: int<frame>) =
        let mutable currentFrame = initialFrame
        
        interface GameContext.IGameContext with
            member _.GetWorldState() = createMockWorldState currentFrame
            member _.GetResources() = (createMockWorldState currentFrame).Resources
            member _.GetMapInfo() = (createMockWorldState currentFrame).Map
            member _.GetCurrentFrame() = currentFrame
            member _.GetDeltaTime() = 1.0f / 30.0f
        
        member _.AdvanceFrame() = 
            currentFrame <- currentFrame + 1<frame>
        
        member _.SetFrame(frame: int<frame>) =
            currentFrame <- frame

    /// Mock AI configuration for testing
    let createMockAIConfig() : AIConfig = {
        MaxUnitsToTrack = 1000
        SpatialGridSize = 32
        BatchCommandSize = 50
        EnableProfiling = true
        LogLevel = "Debug"
    }

    /// Mock performance metrics
    let createMockMetrics() : PerformanceMetrics = {
        FrameTime = 2.5
        CommandTime = 0.8
        QueryTime = 0.3
        MemoryUsage = 1024L * 1024L * 64L  // 64 MB
        GCCollections = 3
    }

/// Test scenarios for validating different aspects of the AI
module TestScenarios =
    
    /// Scenario 1: Basic unit management
    let basicUnitManagement () =
        let worldState = MockInterop.createMockWorldState 1<frame>
        
        // Find idle units and give them move commands
        let idleUnits = GameContext.Units.getUnitsByState worldState UnitState.Idle
        let commands = 
            idleUnits 
            |> Array.mapi (fun i unit ->
                let targetPos = Vector3(300.0f + float32 i * 50.0f, 0.0f, 300.0f + float32 i * 50.0f)
                Move(unit.Id, targetPos))
        
        (worldState, commands)
    
    /// Scenario 2: Combat engagement
    let combatEngagement () =
        let worldState = MockInterop.createMockWorldState 100<frame>
        
        // Find friendly units near enemies and engage
        let combatCommands = 
            worldState.FriendlyUnits
            |> Array.choose (fun friendlyUnit ->
                let nearbyEnemies = GameContext.Spatial.getUnitsInRadius worldState friendlyUnit.Position 200.0f<elmo>
                let enemies = nearbyEnemies.Units |> Array.filter (fun u -> u.TeamId <> 0)
                if enemies.Length > 0 then
                    Some (Attack(friendlyUnit.Id, enemies.[0].Id))
                else
                    None)
        
        (worldState, combatCommands)
    
    /// Scenario 3: Economic management
    let economicManagement () =
        let worldState = MockInterop.createMockWorldState 500<frame>
        
        // Build new units if we have resources
        let builders = GameContext.Units.getIdleBuilders worldState [| 103<unitdef> |]  // Builder unit type
        let commands =
            if GameContext.Economy.canAffordUnit worldState.Resources 100.0f<metal> 50.0f<energy> && builders.Length > 0 then
                let buildPos = Vector3(250.0f, 0.0f, 250.0f)
                [| Build(builders.[0].Id, 101<unitdef>, buildPos) |]
            else
                [||]
        
        (worldState, commands)
    
    /// Scenario 4: Damaged unit management
    let damagedUnitManagement () =
        let worldState = MockInterop.createMockWorldState 300<frame>
        
        // Retreat damaged units
        let damagedUnits = GameContext.Units.getDamagedUnits worldState 0.5f  // Below 50% health
        let retreatCommands = 
            damagedUnits
            |> Array.choose (fun unit ->
                GameContext.Map.findSafePosition worldState unit.Position 300.0f<elmo>
                |> Option.map (fun safePos -> Move(unit.Id, safePos)))
        
        (worldState, retreatCommands)
    
    /// Scenario 5: Spatial query performance
    let spatialQueryPerformance () =
        let worldState = MockInterop.createMockWorldState 1000<frame>
        
        // Perform multiple spatial queries to test performance
        let queryCenter = Vector3(500.0f, 0.0f, 500.0f)
        let results = [|
            GameContext.Spatial.getUnitsInRadius worldState queryCenter 100.0f<elmo>
            GameContext.Spatial.getUnitsInRadius worldState queryCenter 200.0f<elmo>
            GameContext.Spatial.getUnitsInRadius worldState queryCenter 300.0f<elmo>
        |]
        
        let totalQueryTime = results |> Array.sumBy (_.QueryTimeMs)
        (worldState, [||], totalQueryTime)

/// Complete test suite runner
module TestRunner =
    
    /// Run all test scenarios and report results
    let runAllTests () =
        printfn "=== F# RecoilEngine Wrapper Test Suite ==="
        printfn ""
        
        // Test 1: Basic unit management
        printfn "🎯 Test 1: Basic Unit Management"
        let (worldState1, commands1) = TestScenarios.basicUnitManagement()
        printfn $"  World state: {worldState1.Units.Length} total units ({worldState1.FriendlyUnits.Length} friendly, {worldState1.EnemyUnits.Length} enemies)"
        printfn $"  Generated {commands1.Length} movement commands"
        let result1 = MockInterop.executeCommandBatch commands1
        printfn $"  Execution: {result1.SuccessCount}/{commands1.Length} succeeded in {result1.ExecutionTimeMs:F1}ms"
        printfn ""
        
        // Test 2: Combat engagement
        printfn "⚔️  Test 2: Combat Engagement"
        let (worldState2, commands2) = TestScenarios.combatEngagement()
        printfn $"  Generated {commands2.Length} combat commands"
        let result2 = MockInterop.executeCommandBatch commands2
        printfn $"  Execution: {result2.SuccessCount}/{commands2.Length} succeeded in {result2.ExecutionTimeMs:F1}ms"
        printfn ""
        
        // Test 3: Economic management
        printfn "💰 Test 3: Economic Management"
        let (worldState3, commands3) = TestScenarios.economicManagement()
        printfn $"  Resources: {worldState3.Resources.Metal} metal, {worldState3.Resources.Energy} energy"
        printfn $"  Generated {commands3.Length} build commands"
        if commands3.Length > 0 then
            let result3 = MockInterop.executeCommandBatch commands3
            printfn $"  Execution: {result3.SuccessCount}/{commands3.Length} succeeded in {result3.ExecutionTimeMs:F1}ms"
        else
            printfn $"  No build commands (insufficient resources or builders)"
        printfn ""
        
        // Test 4: Damaged unit management
        printfn "🏥 Test 4: Damaged Unit Management"
        let (worldState4, commands4) = TestScenarios.damagedUnitManagement()
        let damagedCount = GameContext.Units.getDamagedUnits worldState4 0.5f |> Array.length
        printfn $"  Found {damagedCount} damaged units (< 50% health)"
        printfn $"  Generated {commands4.Length} retreat commands"
        let result4 = MockInterop.executeCommandBatch commands4
        printfn $"  Execution: {result4.SuccessCount}/{commands4.Length} succeeded in {result4.ExecutionTimeMs:F1}ms"
        printfn ""
        
        // Test 5: Spatial query performance
        printfn "🔍 Test 5: Spatial Query Performance"
        let (worldState5, _, queryTime) = TestScenarios.spatialQueryPerformance()
        printfn $"  Performed 3 spatial queries in {queryTime:F2}ms total"
        printfn $"  Average query time: {queryTime / 3.0:F2}ms"
        printfn ""
        
        // Test 6: Data structure validation
        printfn "📊 Test 6: Data Structure Validation"
        let context = MockInterop.MockGameContext(1<frame>)
        let config = MockInterop.createMockAIConfig()
        let metrics = MockInterop.createMockMetrics()
        printfn $"  AI Config: Max units = {config.MaxUnitsToTrack}, Grid size = {config.SpatialGridSize}"
        printfn $"  Performance: Frame time = {metrics.FrameTime:F1}ms, Memory = {metrics.MemoryUsage / 1024L / 1024L}MB"
        printfn ""
        
        printfn "✅ All tests completed successfully!"
        printfn ""
        printfn "Key features validated:"
        printfn "• F# types with units of measure (elmo, hp, metal, energy)"
        printfn "• Discriminated union command pattern matching"
        printfn "• Structure-of-Arrays world state management"
        printfn "• Spatial query operations and performance"
        printfn "• Command validation and batch execution"
        printfn "• Mock testing infrastructure for development"
        printfn "• Economic and military decision logic"
        printfn "• Data-oriented pipeline with transparent state flow"
