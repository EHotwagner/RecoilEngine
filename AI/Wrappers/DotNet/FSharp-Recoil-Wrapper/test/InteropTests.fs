/// Test file for F# P/Invoke integration with native SpringAI wrapper
module SpringAI.Core.Tests.InteropTests

open NUnit.Framework
open SpringAI.Core
open System.Numerics

[<TestFixture>]
type InteropTests() =
    
    [<Test>]
    member this.``Basic P/Invoke calls work``() =
        try
            let unitCount = NativeInterop.GetUnitCount()
            let metal = NativeInterop.GetMetal()
            let energy = NativeInterop.GetEnergy()
            let frame = NativeInterop.GetCurrentFrame()
            
            Assert.That(unitCount, Is.GreaterThanOrEqualTo(0))
            Assert.That(metal, Is.GreaterThanOrEqualTo(0.0f))
            Assert.That(energy, Is.GreaterThanOrEqualTo(0.0f))
            Assert.That(frame, Is.GreaterThanOrEqualTo(0))
            
            printfn $"✅ Basic queries: Units={unitCount}, Metal={metal}, Energy={energy}, Frame={frame}"
        with
        | ex -> Assert.Fail($"P/Invoke failed: {ex.Message}")
    
    [<Test>]
    member this.``Unit array filling works``() =
        try
            let unitCount = NativeInterop.GetUnitCount()
            if unitCount > 0 then
                let nativeUnits = Array.zeroCreate<NativeUnit> unitCount
                let filledCount = NativeInterop.FillUnitArray(nativeUnits, unitCount)
                
                Assert.That(filledCount, Is.GreaterThan(0))
                Assert.That(filledCount, Is.LessThanOrEqualTo(unitCount))
                
                // Check first unit has valid data
                if filledCount > 0 then
                    let firstUnit = nativeUnits.[0]
                    Assert.That(firstUnit.id, Is.GreaterThan(0))
                    Assert.That(firstUnit.health, Is.GreaterThan(0.0f))
                    Assert.That(firstUnit.maxHealth, Is.GreaterThanOrEqualTo(firstUnit.health))
                    
                    printfn $"✅ Filled {filledCount} units. First unit: ID={firstUnit.id}, Health={firstUnit.health}/{firstUnit.maxHealth}"
        with
        | ex -> Assert.Fail($"Unit array filling failed: {ex.Message}")
    
    [<Test>]
    member this.``Resource state filling works``() =
        try
            let mutable resources = Unchecked.defaultof<NativeResourceState>
            let result = NativeInterop.FillResourceState(&resources)
            
            Assert.That(result, Is.GreaterThan(0))
            Assert.That(resources.metal, Is.GreaterThanOrEqualTo(0.0f))
            Assert.That(resources.energy, Is.GreaterThanOrEqualTo(0.0f))
            Assert.That(resources.currentFrame, Is.GreaterThanOrEqualTo(0))
            
            printfn $"✅ Resources: Metal={resources.metal}, Energy={resources.energy}, Frame={resources.currentFrame}"
        with
        | ex -> Assert.Fail($"Resource state filling failed: {ex.Message}")
    
    [<Test>]
    member this.``High-level world state retrieval works``() =
        try
            let worldState = DataOrientedInterop.getWorldState()
            
            Assert.That(worldState.Units, Is.Not.Null)
            Assert.That(worldState.Resources.Metal, Is.GreaterThanOrEqualTo(0.0f<metal>))
            Assert.That(worldState.Resources.Energy, Is.GreaterThanOrEqualTo(0.0f<energy>))
            Assert.That(worldState.MapWidth, Is.GreaterThan(0.0f<elmo>))
            Assert.That(worldState.MapHeight, Is.GreaterThan(0.0f<elmo>))
            
            printfn $"✅ World state: {worldState.Units.Length} units, {worldState.Resources.Metal} metal, Map {worldState.MapWidth}x{worldState.MapHeight}"
        with
        | ex -> Assert.Fail($"High-level world state retrieval failed: {ex.Message}")
    
    [<Test>]
    member this.``Command execution works``() =
        try
            let commands = [|
                Move(1, Vector3(100.0f, 0.0f, 100.0f))
                Build(2, "factory", Vector3(200.0f, 0.0f, 200.0f))
                Attack(3, 4)
            |]
            
            let result = DataOrientedInterop.executeCommandBatch(commands)
            
            Assert.That(result.SuccessCount + result.FailureCount, Is.EqualTo(commands.Length))
            Assert.That(result.ExecutionTimeMs, Is.GreaterThanOrEqualTo(0.0))
            
            printfn $"✅ Command execution: {result.SuccessCount} succeeded, {result.FailureCount} failed, took {result.ExecutionTimeMs:F2}ms"
        with
        | ex -> Assert.Fail($"Command execution failed: {ex.Message}")
    
    [<Test>]
    member this.``Spatial queries work``() =
        try
            let worldState = DataOrientedInterop.getWorldState()
            if worldState.Units.Length > 0 then
                let center = Vector3(150.0f, 0.0f, 150.0f)
                let radius = 100.0f<elmo>
                let nearbyUnits = DataOrientedInterop.getUnitsInRadius worldState center radius
                
                Assert.That(nearbyUnits, Is.Not.Null)
                Assert.That(nearbyUnits.Length, Is.LessThanOrEqualTo(worldState.Units.Length))
                
                printfn $"✅ Spatial query: Found {nearbyUnits.Length} units near {center} within radius {radius}"
        with
        | ex -> Assert.Fail($"Spatial query failed: {ex.Message}")
    
    [<Test>]
    member this.``Position validation works``() =
        try
            let validPos = Vector3(100.0f, 0.0f, 100.0f)
            let invalidPos = Vector3(-100.0f, 0.0f, -100.0f)
            
            let validResult = DataOrientedInterop.isPositionValid(validPos)
            let invalidResult = DataOrientedInterop.isPositionValid(invalidPos)
            
            Assert.That(validResult, Is.True)
            Assert.That(invalidResult, Is.False)
            
            printfn $"✅ Position validation: {validPos} = {validResult}, {invalidPos} = {invalidResult}"
        with
        | ex -> Assert.Fail($"Position validation failed: {ex.Message}")
    
    [<Test>]
    member this.``Memory pool functionality works``() =
        try
            let array1 = ArrayPools.getIntArray(100)
            let array2 = ArrayPools.getIntArray(50)
            
            Assert.That(array1.Length, Is.GreaterThanOrEqualTo(100))
            Assert.That(array2.Length, Is.GreaterThanOrEqualTo(50))
            
            ArrayPools.returnIntArray(array1)
            ArrayPools.returnIntArray(array2)
            
            // Get arrays again to test reuse
            let array3 = ArrayPools.getIntArray(100)
            Assert.That(array3.Length, Is.GreaterThanOrEqualTo(100))
            
            printfn $"✅ Memory pools: Created and reused arrays successfully"
        with
        | ex -> Assert.Fail($"Memory pool functionality failed: {ex.Message}")

[<TestFixture>]
type PerformanceTests() =
    
    [<Test>]
    member this.``World state retrieval performance``() =
        let iterations = 100
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        for i in 1..iterations do
            let worldState = DataOrientedInterop.getWorldState()
            Assert.That(worldState.Units, Is.Not.Null)
        
        stopwatch.Stop()
        let averageMs = stopwatch.Elapsed.TotalMilliseconds / float iterations
        
        Assert.That(averageMs, Is.LessThan(5.0)) // Should be under 5ms per call
        printfn $"✅ Performance: World state retrieval averaged {averageMs:F3}ms over {iterations} iterations"
    
    [<Test>]
    member this.``Command batch performance``() =
        let iterations = 50
        let commands = [|
            for i in 1..10 do
                yield Move(i, Vector3(float32 i * 10.0f, 0.0f, float32 i * 10.0f))
        |]
        
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        for i in 1..iterations do
            let result = DataOrientedInterop.executeCommandBatch(commands)
            Assert.That(result.SuccessCount + result.FailureCount, Is.EqualTo(commands.Length))
        
        stopwatch.Stop()
        let averageMs = stopwatch.Elapsed.TotalMilliseconds / float iterations
        
        Assert.That(averageMs, Is.LessThan(2.0)) // Should be under 2ms per batch
        printfn $"✅ Performance: Command batch execution averaged {averageMs:F3}ms over {iterations} iterations"
