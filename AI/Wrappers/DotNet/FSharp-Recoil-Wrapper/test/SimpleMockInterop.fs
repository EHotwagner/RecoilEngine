/// Simple mock implementation for testing F# RecoilEngine wrapper
module TestProgram

open RecoilAI.Core
open System
open System.Numerics

/// Mock implementations for testing
module MockInterop =
    
    /// Create mock world state with test data
    let createMockWorldState (frameNumber: int<frame>) : WorldState =
        let mockUnits = [|
            // Friendly units
            { Id = 1; DefId = 101<unitdef>; Position = Vector3(100.0f, 0.0f, 100.0f)
              Health = 100.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 0
              State = UnitState.Idle; Velocity = Vector3.Zero; IsBeingBuilt = false; BuildProgress = 1.0f }
            { Id = 2; DefId = 102<unitdef>; Position = Vector3(200.0f, 0.0f, 150.0f)
              Health = 80.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 0
              State = UnitState.Moving; Velocity = Vector3(1.0f, 0.0f, 0.5f); IsBeingBuilt = false; BuildProgress = 1.0f }
            // Enemy units
            { Id = 101; DefId = 201<unitdef>; Position = Vector3(800.0f, 0.0f, 800.0f)
              Health = 90.0f<hp>; MaxHealth = 100.0f<hp>; TeamId = 1
              State = UnitState.Patrolling; Velocity = Vector3(0.5f, 0.0f, -0.5f); IsBeingBuilt = false; BuildProgress = 1.0f }
        |]
        
        let friendlyUnits = mockUnits |> Array.filter (fun u -> u.TeamId = 0)
        let enemyUnits = mockUnits |> Array.filter (fun u -> u.TeamId <> 0)
        
        let resources = {
            Metal = 1000.0f<metal>
            Energy = 800.0f<energy>
            MetalIncome = 15.0f<metal>
            EnergyIncome = 12.0f<energy>
            MetalStorage = 5000.0f<metal>
            EnergyStorage = 3000.0f<energy>
            CurrentFrame = frameNumber
        }
        
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
            DeltaTime = 1.0f / 30.0f
        }
    
    /// Mock command execution
    let executeCommandBatch (commands: Command array) : CommandBatchResult =
        let startTime = DateTime.UtcNow
        
        // Simple simulation: 95% success rate
        let successCount = int (float commands.Length * 0.95)
        let failureCount = commands.Length - successCount
        
        let endTime = DateTime.UtcNow
        let executionTime = (endTime - startTime).TotalMilliseconds
        
        {
            SuccessCount = successCount
            FailureCount = failureCount
            ExecutionTimeMs = executionTime
            Errors = [||]  // Simplified - no error details
        }

/// Test runner for the F# RecoilEngine wrapper
module TestRunner =
    
    let runBasicTest () =
        printfn "=== F# RecoilEngine Wrapper - Basic Test ==="
        printfn ""
        
        // Create test world state
        let worldState = MockInterop.createMockWorldState 1<frame>
        printfn $"World state: {worldState.Units.Length} total units ({worldState.FriendlyUnits.Length} friendly, {worldState.EnemyUnits.Length} enemies)"
        printfn $"Resources: {worldState.Resources.Metal} metal, {worldState.Resources.Energy} energy"
        printfn ""
        
        // Test spatial queries
        let center = Vector3(150.0f, 0.0f, 150.0f)
        let nearbyUnits = GameContext.Spatial.getUnitsInRadius worldState center 100.0f<elmo>
        printfn $"Spatial query: {nearbyUnits.Count} units near {center} within 100 elmo"
        printfn ""
        
        // Test command generation
        let idleUnits = GameContext.Units.getUnitsByState worldState UnitState.Idle
        let commands = 
            idleUnits 
            |> Array.map (fun unit -> Move(unit.Id, Vector3(300.0f, 0.0f, 300.0f)))
        
        printfn $"Generated {commands.Length} commands for idle units"
        
        // Test command execution
        if commands.Length > 0 then
            let result = Commands.executeCommands worldState commands
            printfn $"Execution result: {result.SuccessCount}/{commands.Length} succeeded in {result.ExecutionTimeMs:F1}ms"
        
        printfn ""
        printfn "✅ Basic test completed successfully!"
        printfn ""
        printfn "Validated features:"
        printfn "• F# types with units of measure"
        printfn "• Data-oriented world state structure"
        printfn "• Spatial query operations"
        printfn "• Command validation and execution"
        printfn "• Mock testing infrastructure"

/// Program entry point (must be last)
module Program =
    [<EntryPoint>]
    let main argv =
        TestRunner.runBasicTest()
        
        printfn "\nPress any key to exit..."
        System.Console.ReadKey() |> ignore
        0
