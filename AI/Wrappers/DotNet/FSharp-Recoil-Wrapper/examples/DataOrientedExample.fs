/// Example demonstrating F# P/Invoke integration with native SpringAI wrapper
/// This shows the complete data-oriented pipeline from native C++ to F# AI logic
module SpringAI.Core.Examples.DataOrientedExample

open System
open System.Numerics
open SpringAI.Core

/// Simple AI function that processes world state and generates commands using data-oriented approach
let exampleAIFunction (worldState: WorldState) : Command array =
    // Economic logic using array operations
    let lowHealthUnits = 
        worldState.Units 
        |> Array.filter (fun unit -> float32 unit.Health < float32 unit.MaxHealth * 0.3f)
    
    let idleUnits = 
        worldState.Units 
        |> Array.filter (fun unit -> unit.State = UnitState.Idle)
    
    let builders = 
        worldState.Units 
        |> Array.filter (fun unit -> unit.DefId = 102) // Builder def ID from mock data
    
    [|
        // Retreat damaged units
        for unit in lowHealthUnits do
            let retreatPos = Vector3(unit.Position.X - 50.0f, unit.Position.Y, unit.Position.Z - 50.0f)
            if DataOrientedInterop.isPositionValid(retreatPos) then
                yield Move(unit.Id, retreatPos)
        
        // Send idle units to explore
        for i, unit in Array.indexed idleUnits do
            if i < 3 then // Limit to 3 units
                let explorePos = Vector3(float32 i * 100.0f + 300.0f, 0.0f, float32 i * 100.0f + 300.0f)
                yield Move(unit.Id, explorePos)
        
        // Build factories with available builders
        for builder in builders do
            if worldState.Resources.Metal > 200.0f<metal> then
                let buildPos = Vector3(builder.Position.X + 80.0f, builder.Position.Y, builder.Position.Z)
                if DataOrientedInterop.isPositionValid(buildPos) then
                    yield Build(builder.Id, "factory", buildPos)
    |]

/// Demonstrate the complete data pipeline
let runDataOrientedAIExample () =
    printfn "=== F# Data-Oriented AI Example ==="
    printfn ""
    
    try
        // Step 1: Get world state from native code via P/Invoke
        printfn "📡 Retrieving world state from native code..."
        let worldState = DataOrientedInterop.getWorldState()
        
        printfn $"   Units: {worldState.Units.Length}"
        printfn $"   Resources: {worldState.Resources.Metal} metal, {worldState.Resources.Energy} energy"
        printfn $"   Map: {worldState.MapWidth} x {worldState.MapHeight}"
        printfn $"   Frame: {worldState.CurrentFrame}"
        printfn ""
        
        // Step 2: Show unit details
        printfn "🤖 Unit Analysis:"
        for unit in worldState.Units do
            let healthPct = (float32 unit.Health / float32 unit.MaxHealth) * 100.0f
            printfn $"   Unit {unit.Id}: DefId={unit.DefId}, Health={healthPct:F1}%%, State={unit.State}, Pos=({unit.Position.X:F1}, {unit.Position.Z:F1})"
        printfn ""
        
        // Step 3: Run AI logic using data-oriented processing
        printfn "🧠 Running AI logic..."
        let commands = exampleAIFunction worldState
        printfn $"   Generated {commands.Length} commands"
        
        for i, command in Array.indexed commands do
            match command with
            | Move(unitId, pos) -> printfn $"   {i+1}. Move unit {unitId} to ({pos.X:F1}, {pos.Z:F1})"
            | Build(builderId, unitType, pos) -> printfn $"   {i+1}. Build {unitType} with unit {builderId} at ({pos.X:F1}, {pos.Z:F1})"
            | Attack(attackerId, targetId) -> printfn $"   {i+1}. Unit {attackerId} attack unit {targetId}"
            | Stop(unitId) -> printfn $"   {i+1}. Stop unit {unitId}"
            | Guard(unitId, targetId) -> printfn $"   {i+1}. Unit {unitId} guard unit {targetId}"
            | Patrol(unitId, positions) -> printfn $"   {i+1}. Unit {unitId} patrol {positions.Length} positions"
        printfn ""
        
        // Step 4: Execute commands via native interface
        if commands.Length > 0 then
            printfn "⚡ Executing commands..."
            let result = DataOrientedInterop.executeCommandBatch(commands)
            printfn $"   Success: {result.SuccessCount}/{commands.Length}"
            printfn $"   Execution time: {result.ExecutionTimeMs:F2}ms"
            printfn ""
        
        // Step 5: Demonstrate spatial queries
        printfn "🗺️  Spatial Query Example:"
        let center = Vector3(150.0f, 0.0f, 150.0f)
        let radius = 100.0f<elmo>
        let nearbyUnits = DataOrientedInterop.getUnitsInRadius worldState center radius
        printfn $"   Found {nearbyUnits.Length} units within {radius} of {center}"
        for unit in nearbyUnits do
            let distance = Vector3.Distance(center, unit.Position)
            printfn $"   - Unit {unit.Id} at distance {distance:F1}"
        printfn ""
        
        // Step 6: Performance measurement
        printfn "📊 Performance Test:"
        let iterations = 100
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        for i in 1..iterations do
            let _ = DataOrientedInterop.getWorldState()
            ()
        
        stopwatch.Stop()
        let avgMs = stopwatch.Elapsed.TotalMilliseconds / float iterations
        printfn $"   World state retrieval: {avgMs:F3}ms average over {iterations} iterations"
        printfn ""
        
        printfn "✅ Data-oriented AI pipeline completed successfully!"
        printfn ""
        printfn "Key benefits demonstrated:"
        printfn "• Direct array filling from native code (no object marshaling)"
        printfn "• Structure-of-Arrays processing for cache efficiency"
        printfn "• Batch command execution"
        printfn "• Efficient spatial queries"
        printfn "• Memory pooling for reduced allocations"
        printfn "• Type safety with F# units of measure"
        
    with
    | ex ->
        printfn $"❌ Error: {ex.Message}"
        printfn ""
        printfn "Troubleshooting tips:"
        printfn "1. Make sure SpringAIWrapper.dll is built and in the output directory"
        printfn "2. Check that the native library path is correct"
        printfn "3. Verify all native functions are exported correctly"
        printfn "4. Run the native test program first to validate the C++ library"

/// Entry point for console testing
[<EntryPoint>]
let main argv =
    runDataOrientedAIExample()
    
    printfn ""
    printfn "Press any key to exit..."
    Console.ReadKey() |> ignore
    0
