# Quick Start Guide: Data-Oriented F# AI Development

## 🚀 Getting Started in 5 Minutes

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with F# extensions
- CMake (for native builds)
- RecoilEngine development environment

### Immediate Next Steps

#### 1. Set Up Development Environment
```bash
# Clone and navigate to wrapper directory
cd AI/Wrappers/DotNet

# Create initial build structure
mkdir -p build
cd build
cmake ..
make  # or use Visual Studio on Windows
```

#### 2. Start with Milestone 1.1 (Week 1)
**Focus**: Get basic native interop working

```cpp
// src/native/SpringAIWrapperExports.cpp
extern "C" {
    EXPORT float GetMetal() {
        return 100.0f; // Mock data for testing
    }
    
    EXPORT float GetEnergy() {
        return 50.0f; // Mock data for testing
    }
    
    EXPORT int GetUnitCount() {
        return 5; // Mock data for testing
    }
}
```

#### 3. Create Basic F# Test
```fsharp
// test/BasicInteropTests.fs
[<Test>]
let ``Native interop returns mock data`` () =
    let metal = NativeInterop.GetMetal()
    metal |> should equal 100.0f<metal>
```

#### 4. Validate Build System
```bash
# Test that everything compiles
dotnet build src/fsharp-core/SpringAI.Core.fsproj
dotnet test test/SpringAI.Tests.fsproj
```

## 📋 Development Checklist by Phase

### Phase 1: Foundation (Week 1-2) ✅
- [ ] Native C++ stub library builds
- [ ] Basic P/Invoke calls work from F#
- [ ] Units of measure enforce type safety
- [ ] Mock resource data retrieval works
- [ ] Test infrastructure is set up

**Key Files to Create:**
- `src/native/SpringAIWrapperExports.cpp`
- `src/fsharp-core/Types.fs` (basic version)
- `src/fsharp-core/Interop.fs` (basic P/Invoke)
- `test/BasicInteropTests.fs`

### Phase 2: Data Structures (Week 3-4)
- [ ] WorldState SOA implementation
- [ ] Spatial grid for fast queries
- [ ] Array processing functions
- [ ] Performance benchmarks show improvement

**Key Files to Create:**
- `src/fsharp-core/Types.fs` (complete WorldState)
- `src/fsharp-core/GameContext.fs` (spatial operations)
- `test/SpatialGridTests.fs`
- `benchmarks/ArrayProcessingBenchmarks.fs`

### Phase 3: Event System (Week 5-6)
- [ ] Event batching architecture
- [ ] Efficient event collection
- [ ] AI interface with batch support
- [ ] Performance monitoring

**Key Files to Create:**
- `src/fsharp-core/AI.fs` (enhanced with batching)
- `src/fsharp-core/Events.fs`
- `test/EventBatchingTests.fs`

### Phase 4: Commands (Week 7-8)
- [ ] Command batching system
- [ ] Native batch execution
- [ ] Memory pooling
- [ ] GC pressure optimization

**Key Files to Create:**
- `src/fsharp-core/Commands.fs` (complete)
- `src/native/CommandBatchExecution.cpp`
- `test/CommandBatchTests.fs`
- `benchmarks/MemoryPoolBenchmarks.fs`

### Phase 5: C# Integration (Week 9-10)
- [ ] C# wrapper interfaces
- [ ] Type converters
- [ ] Integration testing
- [ ] End-to-end scenarios

**Key Files to Create:**
- `src/csharp-compat/AI/ICSharpAI.cs` (enhanced)
- `src/csharp-compat/TypeConverters.cs`
- `test/CSharpIntegrationTests.cs`
- `examples/CSharp/ExampleCSharpAI.cs`

### Phase 6: Advanced Features (Week 11-12)
- [ ] Multi-system AI architecture
- [ ] Advanced spatial algorithms
- [ ] ML feature extraction
- [ ] Production optimization

## 🔧 Development Tools & Setup

### Recommended IDE Setup
```json
// .vscode/settings.json
{
    "FSharp.enableAnalyzers": true,
    "FSharp.enableReferenceCodeLens": true,
    "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true
}
```

### Essential NuGet Packages
```xml
<!-- src/fsharp-core/SpringAI.Core.fsproj -->
<PackageReference Include="FSharp.Core" Version="8.0.0" />
<PackageReference Include="System.Numerics.Vectors" Version="4.5.0" />
<PackageReference Include="BenchmarkDotNet" Version="0.13.0" />
<PackageReference Include="NUnit" Version="3.13.0" />
```

### Build Scripts
```bash
#!/bin/bash
# build.sh - Complete build script

echo "Building native library..."
cd src/native
mkdir -p build && cd build
cmake .. && make

echo "Building F# core..."
cd ../../../
dotnet build src/fsharp-core/SpringAI.Core.fsproj

echo "Building C# compatibility..."
dotnet build src/csharp-compat/SpringAI.CSharp.csproj

echo "Running tests..."
dotnet test test/SpringAI.Tests.fsproj

echo "Running benchmarks..."
dotnet run --project benchmarks/SpringAI.Benchmarks.fsproj
```

## ⚡ Performance Validation

### Quick Performance Check
```fsharp
// benchmarks/QuickPerformanceCheck.fs
[<MemoryDiagnoser>]
type QuickBenchmark() =
    
    [<Benchmark>]
    member this.TraditionalUnitProcessing() =
        let units = generateTestUnits(1000)
        units |> List.filter (fun u -> u.Health < u.MaxHealth * 0.5f)
    
    [<Benchmark>]
    member this.DataOrientedUnitProcessing() =
        let worldState = generateTestWorldState(1000)
        Array.zip worldState.UnitIds worldState.UnitHealth
        |> Array.zip worldState.UnitMaxHealth
        |> Array.filter (fun (maxHealth, (_, health)) -> health < maxHealth * 0.5f)
```

### Memory Usage Monitoring
```fsharp
let monitorMemoryUsage operation =
    let before = GC.GetTotalMemory(false)
    let result = operation()
    let after = GC.GetTotalMemory(false)
    printfn "Memory used: %d bytes" (after - before)
    result
```

## 🎯 Success Indicators by Week

### Week 1-2: Foundation Success
```fsharp
// Should pass these tests
[<Test>]
let ``Native calls return expected values`` () = // ✅
[<Test>]
let ``F# types compile without errors`` () = // ✅
[<Test>]
let ``Basic resource queries work`` () = // ✅
```

### Week 3-4: Data Structures Success
```fsharp
// Should pass these benchmarks
[<Test>]
let ``SOA access is faster than AOS`` () = // ✅ 2x improvement
[<Test>]
let ``Spatial queries beat linear search`` () = // ✅ 10x improvement
[<Test>]
let ``Array operations use SIMD`` () = // ✅ Vector instructions
```

### Week 5-6: Event System Success
```fsharp
// Should handle these loads
[<Test>]
let ``Process 1000 events in <1ms`` () = // ✅ Batch processing
[<Test>]
let ``Memory stable over 1000 frames`` () = // ✅ No memory leaks
[<Test>]
let ``Event ordering preserved`` () = // ✅ Correctness
```

## 🚨 Common Pitfalls & Solutions

### Pitfall 1: P/Invoke Performance
**Problem**: Frequent P/Invoke calls are slow
**Solution**: Batch operations, minimize calls
```fsharp
// Bad: Multiple calls
let metal = NativeInterop.GetMetal()
let energy = NativeInterop.GetEnergy()

// Good: Single batch call
let resources = NativeInterop.GetResourcesBatch()
```

### Pitfall 2: Array Allocations
**Problem**: Creating new arrays every frame
**Solution**: Use array pools and in-place operations
```fsharp
// Bad: New allocation
let filtered = units |> Array.filter predicate

// Good: Reuse arrays
let filtered = ArrayPool.getArray(units.Length)
let count = ArrayOperations.filterInPlace units predicate filtered
```

### Pitfall 3: Cache Misses
**Problem**: Random memory access patterns
**Solution**: Structure-of-Arrays layout
```fsharp
// Bad: Array of Structures (AOS)
type Unit = { Id: int; Position: Vector3; Health: float32 }
let units: Unit[] = [| ... |]

// Good: Structure of Arrays (SOA)
type WorldState = {
    UnitIds: int[]
    UnitPositions: Vector3[]
    UnitHealth: float32[]
}
```

## 📊 Testing Strategy

### Unit Tests (Every Component)
```fsharp
[<TestFixture>]
type ComponentTests() =
    [<SetUp>]
    member this.Setup() = // Initialize test data
    
    [<Test>]
    member this.``Feature works correctly``() = // Functional test
    
    [<Test>]
    member this.``Performance meets target``() = // Performance test
    
    [<TearDown>]
    member this.Cleanup() = // Clean up resources
```

### Integration Tests (Each Milestone)
```fsharp
[<TestFixture>]
type IntegrationTests() =
    [<Test>]
    member this.``End to end workflow``() =
        // Full AI lifecycle test
        let ai = createTestAI()
        ai.Initialize()
        for frame in 1..100 do ai.ProcessFrame(frame)
        ai.Shutdown()
```

### Performance Tests (Continuous)
```fsharp
[<MemoryDiagnoser>]
type PerformanceTests() =
    [<Benchmark>]
    member this.ProcessLargeWorldState() =
        // Benchmark critical paths
        let worldState = generateLargeWorldState(5000)
        processWorldState worldState
```

## 🎯 Next Immediate Actions

1. **Today**: Set up development environment and build system
2. **This Week**: Complete Milestone 1.1 (Native interface)
3. **Next Week**: Implement basic F# types and P/Invoke (Milestone 1.2)
4. **Month 1**: Complete Phase 1-2 (Foundation + Data Structures)

**Start with**: Creating the native stub library and getting your first P/Invoke call working. Everything builds from there!
