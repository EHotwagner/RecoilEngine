# Implementation Plan: Data-Oriented F# AI Wrapper for RecoilEngine/Spring

## Project Overview

**Goal**: Create a high-performance, F#-first, data-oriented .NET AI wrapper for RecoilEngine/Spring, optimized for Beyond All Reason (BAR) AI development.

**Duration**: ~8-12 weeks (depending on team size and experience)
**Target Performance**: 3-5x improvement over traditional OOP approaches

---

## Phase 1: Foundation & Native Interface (Week 1-2)

### Milestone 1.1: Native C++ Interface Setup
**Duration**: 3-4 days
**Goal**: Establish basic native interop layer

#### Tasks:
1. **Create Native C++ Stub Library**
   ```bash
   # Directory structure
   src/native/
   ├── SpringAIWrapperInterface.h
   ├── SpringAIWrapperInterface.cpp
   ├── SpringAIWrapperExports.cpp
   └── CMakeLists.txt
   ```

2. **Implement Core Native Functions**
   ```cpp
   // SpringAIWrapperExports.cpp
   extern "C" {
       EXPORT int GetUnitCount();
       EXPORT int GetUnits(int* unitIds, int maxCount);
       EXPORT int GetUnitPositions(const int* unitIds, float* positions, int count);
       EXPORT float GetMetal();
       EXPORT float GetEnergy();
       EXPORT int ExecuteCommand(int commandType, const void* commandData);
   }
   ```

3. **Build System Integration**
   - Update main `CMakeLists.txt` to include DotNet wrapper
   - Configure build for both Debug and Release
   - Set up proper export symbols

#### Acceptance Criteria:
- [ ] Native library builds successfully on Windows/Linux
- [ ] Exports are visible and callable from .NET
- [ ] Basic resource queries return mock data
- [ ] Unit count and basic unit data retrieval works

#### Test Plan:
```csharp
[Test]
public void NativeInterface_GetMetal_ReturnsValue()
{
    var metal = NativeInterop.GetMetal();
    Assert.That(metal, Is.GreaterThanOrEqualTo(0));
}
```

### Milestone 1.2: F# Core Types and Interop
**Duration**: 2-3 days
**Goal**: Establish F# type system and basic P/Invoke

#### Tasks:
1. **Create Core F# Types**
   ```fsharp
   // Types.fs - Basic types only
   [<Measure>] type metal
   [<Measure>] type energy
   [<Measure>] type frame
   
   type Command = 
       | Build of builderId: int * unitDefName: string * position: Vector3
       | Move of unitId: int * destination: Vector3
   
   type ResourceState = {
       Metal: float32<metal>
       Energy: float32<energy>
       CurrentFrame: int<frame>
   }
   ```

2. **Basic P/Invoke Layer**
   ```fsharp
   // Interop.fs - Essential functions only
   module NativeInterop =
       [<DllImport("SpringAIWrapper")>]
       extern float GetMetal()
       
       [<DllImport("SpringAIWrapper")>]
       extern float GetEnergy()
   ```

3. **Simple Test Interface**
   ```fsharp
   type IBasicGameContext =
       abstract member GetMetal: unit -> float32<metal>
       abstract member GetEnergy: unit -> float32<energy>
   ```

#### Acceptance Criteria:
- [ ] F# types compile without errors
- [ ] P/Invoke calls work from F#
- [ ] Units of measure enforce type safety
- [ ] Basic resource state can be retrieved

#### Test Plan:
```fsharp
[<Test>]
let ``GetResourceState returns valid data`` () =
    let context = TestGameContext()
    let resources = context.GetResources()
    resources.Metal |> should be (greaterThan 0.0f<metal>)
```

---

## Phase 2: Data-Oriented Core Architecture (Week 3-4)

### Milestone 2.1: World State Arrays (SOA)
**Duration**: 4-5 days
**Goal**: Implement Structure-of-Arrays for game state

#### Tasks:
1. **Design WorldState Type**
   ```fsharp
   type WorldState = {
       UnitIds: int array
       UnitPositions: Vector3 array
       UnitHealth: float32 array
       UnitMaxHealth: float32 array
       UnitFactions: BARFaction array
       CurrentFrame: int<frame>
   }
   ```

2. **Native Batch Data Retrieval**
   ```cpp
   // Get all unit data in single calls
   EXPORT int GetWorldState(WorldStateData* data);
   EXPORT int GetUnitBatch(int* ids, float* positions, float* health, int maxCount);
   ```

3. **F# Array Processing Functions**
   ```fsharp
   module DataOrientedProcessing =
       let calculateHealthPercentages (worldState: WorldState) : float32 array =
           Array.map2 (/) worldState.UnitHealth worldState.UnitMaxHealth
       
       let filterByFaction (worldState: WorldState) (faction: BARFaction) : int array =
           Array.zip worldState.UnitIds worldState.UnitFactions
           |> Array.filter (fun (_, f) -> f = faction)
           |> Array.map fst
   ```

#### Acceptance Criteria:
- [ ] World state retrieved in single native call
- [ ] Arrays are properly aligned and sized
- [ ] Array operations work efficiently
- [ ] Memory layout is cache-friendly

#### Test Plan:
```fsharp
[<Test>]
let ``WorldState arrays have consistent lengths`` () =
    let worldState = getTestWorldState()
    worldState.UnitIds.Length |> should equal worldState.UnitPositions.Length
    worldState.UnitHealth.Length |> should equal worldState.UnitIds.Length
```

### Milestone 2.2: Spatial Indexing System
**Duration**: 3-4 days
**Goal**: Implement efficient spatial queries

#### Tasks:
1. **Spatial Grid Implementation**
   ```fsharp
   type SpatialGrid = {
       GridSize: int
       CellSize: float32<elmo>
       UnitCells: int array array
   }
   
   let buildSpatialGrid (units: CompactUnit array) (cellSize: float32<elmo>) : SpatialGrid
   ```

2. **Fast Spatial Queries**
   ```fsharp
   let queryUnitsInRadius (grid: SpatialGrid) (center: Vector3) (radius: float32<elmo>) : int array
   let queryUnitsInArea (grid: SpatialGrid) (min: Vector3) (max: Vector3) : int array
   ```

3. **Performance Benchmarks**
   - Compare O(n) linear search vs O(1) grid lookup
   - Measure cache misses and memory access patterns

#### Acceptance Criteria:
- [ ] Spatial grid builds correctly
- [ ] Radius queries return correct units
- [ ] Performance is significantly better than linear search
- [ ] Grid updates efficiently when units move

#### Test Plan:
```fsharp
[<Test>]
let ``Spatial grid radius query finds nearby units`` () =
    let grid = buildTestSpatialGrid()
    let center = Vector3(100.0f, 0.0f, 100.0f)
    let nearbyUnits = queryUnitsInRadius grid center 50.0f<elmo>
    nearbyUnits |> should not' (be Empty)
```

---

## Phase 3: Event System & Batch Processing (Week 5-6)

### Milestone 3.1: Event Batching Architecture
**Duration**: 4-5 days
**Goal**: Implement efficient event collection and processing

#### Tasks:
1. **Event Batch Types**
   ```fsharp
   type GameEvent = 
       | UnitCreated of unitId: int * builderId: int * frame: int<frame>
       | UnitDestroyed of unitId: int * attackerId: int * frame: int<frame>
       | FrameUpdate of frame: int<frame>
   
   type EventBatch = {
       Events: GameEvent array
       EventCount: int
       FrameNumber: int<frame>
   }
   ```

2. **Event Collection System**
   ```fsharp
   type EventCollector() =
       let eventBuffer = ResizeArray<GameEvent>()
       
       member this.AddEvent(event: GameEvent) = eventBuffer.Add(event)
       member this.FlushEvents(frame: int<frame>) : EventBatch = 
           let events = eventBuffer.ToArray()
           eventBuffer.Clear()
           { Events = events; EventCount = events.Length; FrameNumber = frame }
   ```

3. **Batch Processing Pipeline**
   ```fsharp
   let processEventBatch (batch: EventBatch) (processor: GameEvent -> unit) : unit =
       // Process in chunks to avoid memory pressure
       let chunkSize = 100
       batch.Events
       |> Array.chunkBySize chunkSize
       |> Array.iter (Array.iter processor)
   ```

#### Acceptance Criteria:
- [ ] Events are collected efficiently during frame
- [ ] Batch processing handles large event counts
- [ ] Memory usage is controlled and predictable
- [ ] Event ordering is preserved

#### Test Plan:
```fsharp
[<Test>]
let ``Event batch processes all events`` () =
    let events = generateTestEvents(1000)
    let batch = { Events = events; EventCount = events.Length; FrameNumber = 100<frame> }
    let mutable processedCount = 0
    processEventBatch batch (fun _ -> processedCount <- processedCount + 1)
    processedCount |> should equal 1000
```

### Milestone 3.2: AI Interface with Batch Support
**Duration**: 3-4 days
**Goal**: Implement F# AI interface with batch operations

#### Tasks:
1. **Enhanced AI Interface**
   ```fsharp
   type IAI =
       abstract member HandleEventBatch: EventBatch -> unit
       abstract member PlanActionsFromWorldState: WorldState -> CommandBatch
       abstract member UpdateStrategy: WorldState -> unit
   ```

2. **Base AI Implementation**
   ```fsharp
   [<AbstractClass>]
   type BaseFSharpAI(context: IGameContext) =
       let mutable worldStateCache: WorldState option = None
       let mutable spatialGridCache: SpatialGrid option = None
       
       member this.ProcessEventBatch(batch: EventBatch) = 
           // Efficient batch processing implementation
   ```

3. **Performance Monitoring**
   ```fsharp
   type PerformanceMetrics = {
       FrameTime: TimeSpan
       EventProcessingTime: TimeSpan
       CommandGenerationTime: TimeSpan
       MemoryUsage: int64
   }
   ```

#### Acceptance Criteria:
- [ ] AI processes event batches efficiently
- [ ] World state caching works correctly
- [ ] Performance metrics are captured
- [ ] Strategy updates based on data analysis

#### Test Plan:
```fsharp
[<Test>]
let ``AI processes event batch within time limit`` () =
    let ai = TestAI()
    let batch = generateLargeEventBatch()
    let stopwatch = Stopwatch.StartNew()
    ai.HandleEventBatch(batch)
    stopwatch.Stop()
    stopwatch.ElapsedMilliseconds |> should be (lessThan 16L) // 60 FPS target
```

---

## Phase 4: Command System & Optimization (Week 7-8)

### Milestone 4.1: Command Batching System
**Duration**: 4-5 days
**Goal**: Implement efficient command execution

#### Tasks:
1. **Command Batch Types**
   ```fsharp
   type CommandBatch = {
       Commands: Command array
       CommandCount: int
       Priority: int array
       FrameToExecute: int<frame> array
   }
   ```

2. **Native Command Execution**
   ```cpp
   EXPORT int ExecuteCommandBatch(int* commandTypes, void** commandData, int count);
   ```

3. **Command Optimization**
   ```fsharp
   module CommandOptimization =
       let optimizeCommandOrder (commands: Command array) : Command array
       let validateCommandsBatch (context: IGameContext) (commands: Command array) : bool array
       let planConstructionSequence (context: IGameContext) (requests: (string * Vector3) array) : Command array
   ```

#### Acceptance Criteria:
- [ ] Commands execute in batches efficiently
- [ ] Command validation works for entire batches
- [ ] Command ordering optimization improves performance
- [ ] Native interface handles batch execution

#### Test Plan:
```fsharp
[<Test>]
let ``Command batch execution succeeds`` () =
    let commands = generateTestCommands(50)
    let batch = { Commands = commands; CommandCount = commands.Length; Priority = Array.create commands.Length 1; FrameToExecute = Array.create commands.Length 100<frame> }
    let result = executeCommandBatch batch
    result.SuccessCount |> should be (greaterThan 0)
```

### Milestone 4.2: Memory Management & Pooling
**Duration**: 3-4 days
**Goal**: Implement array pooling and memory optimization

#### Tasks:
1. **Array Pool Implementation**
   ```fsharp
   type ArrayPool<'T>() =
       let pools = ConcurrentDictionary<int, ConcurrentQueue<'T array>>()
       
       member this.GetArray(size: int) : 'T array
       member this.ReturnArray(array: 'T array) : unit
   ```

2. **Memory-Efficient Operations**
   ```fsharp
   module ArrayPools =
       let intPool = ArrayPool<int>()
       let floatPool = ArrayPool<float32>()
       let vector3Pool = ArrayPool<Vector3>()
   ```

3. **GC Pressure Monitoring**
   ```fsharp
   let measureGCPressure (operation: unit -> 'T) : 'T * int64 =
       let beforeGC = GC.GetTotalMemory(false)
       let result = operation()
       let afterGC = GC.GetTotalMemory(false)
       (result, afterGC - beforeGC)
   ```

#### Acceptance Criteria:
- [ ] Array pools reduce allocations significantly
- [ ] Memory usage remains stable over time
- [ ] GC pressure is minimized during gameplay
- [ ] Performance improves measurably

#### Test Plan:
```fsharp
[<Test>]
let ``Array pools reduce memory allocations`` () =
    let (_, allocations1) = measureGCPressure (fun () -> 
        for i in 1..1000 do Array.zeroCreate<int> 100 |> ignore)
    
    let (_, allocations2) = measureGCPressure (fun () ->
        for i in 1..1000 do 
            let arr = ArrayPools.intPool.GetArray(100)
            ArrayPools.intPool.ReturnArray(arr))
    
    allocations2 |> should be (lessThan (allocations1 / 2L))
```

---

## Phase 5: C# Compatibility & Integration (Week 9-10)

### Milestone 5.1: C# Wrapper Layer
**Duration**: 4-5 days
**Goal**: Provide seamless C# compatibility

#### Tasks:
1. **C# Interface Adapters**
   ```csharp
   public interface ICSharpAI
   {
       void OnEventBatch(GameEvent[] events);
       Command[] PlanActionsFromWorldState(WorldState worldState);
   }
   ```

2. **Type Converters**
   ```csharp
   public static class TypeConverters
   {
       public static FSharpCommand ToFSharpCommand(CSharpCommand command);
       public static CSharpUnitInfo FromFSharpUnit(UnitInfo unit);
   }
   ```

3. **C# Example Implementation**
   ```csharp
   public class ExampleCSharpAI : BaseCSharpAI
   {
       protected override Command[] PlanActionsFromWorldState(WorldState worldState)
       {
           // Use F# optimizations from C#
           return FSharpAIHelpers.GenerateOptimalCommands(worldState);
       }
   }
   ```

#### Acceptance Criteria:
- [ ] C# developers can use the wrapper easily
- [ ] Type conversions are automatic and efficient
- [ ] C# code benefits from F# optimizations
- [ ] IntelliSense and tooling work properly

#### Test Plan:
```csharp
[Test]
public void CSharpAI_CanProcessWorldState()
{
    var ai = new ExampleCSharpAI();
    var worldState = TestHelper.CreateTestWorldState();
    var commands = ai.PlanActionsFromWorldState(worldState);
    Assert.That(commands, Is.Not.Empty);
}
```

### Milestone 5.2: Integration Testing
**Duration**: 3-4 days
**Goal**: End-to-end testing with RecoilEngine

#### Tasks:
1. **Mock RecoilEngine Interface**
   ```cpp
   // Create minimal Spring AI interface mock
   class MockSpringAI : public ISpringAI {
   public:
       virtual int HandleEvent(int topic, const void* data) override;
       virtual int ExecuteCommand(int commandId, void* params) override;
   };
   ```

2. **Integration Test Suite**
   ```fsharp
   [<Test>]
   let ``Full AI lifecycle works`` () =
       let mockEngine = MockSpringAI()
       let ai = DataOrientedBARAI(mockEngine)
       
       // Simulate game lifecycle
       ai.Initialize()
       for frame in 1..100 do
           ai.ProcessFrame(frame)
       ai.Shutdown()
   ```

3. **Performance Benchmarks**
   ```fsharp
   [<Test>]
   let ``Performance meets requirements`` () =
       let ai = DataOrientedBARAI()
       let worldState = generateLargeWorldState(1000) // 1000 units
       
       let stopwatch = Stopwatch.StartNew()
       let commands = ai.PlanActionsFromWorldState(worldState)
       stopwatch.Stop()
       
       stopwatch.ElapsedMilliseconds |> should be (lessThan 5L) // Sub-frame processing
   ```

#### Acceptance Criteria:
- [ ] Full AI lifecycle works with mock engine
- [ ] Performance targets are met consistently
- [ ] Memory usage remains stable
- [ ] No crashes or exceptions under load

---

## Phase 6: Advanced Features & Optimization (Week 11-12)

### Milestone 6.1: Advanced AI Features
**Duration**: 4-5 days
**Goal**: Implement sophisticated AI behaviors

#### Tasks:
1. **Multi-System AI Architecture**
   ```fsharp
   type AISystem =
       | EconomySystem of EconomyState
       | MilitarySystem of MilitaryState
       | BuildSystem of BuildState
       | ScoutSystem of ScoutState
   
   let runAISystems (worldState: WorldState) (systems: AISystem array) : CommandBatch array
   ```

2. **Advanced Spatial Operations**
   ```fsharp
   module AdvancedSpatial =
       let findOptimalBuildLocations (worldState: WorldState) (unitDefName: string) : Vector3 array
       let calculateThreatMap (worldState: WorldState) : float32 array array
       let planOptimalPaths (worldState: WorldState) (moves: (int * Vector3) array) : (int * Vector3 list) array
   ```

3. **Machine Learning Integration Prep**
   ```fsharp
   type MLFeatures = {
       UnitCounts: int array
       ResourceRatios: float32 array
       ThreatLevels: float32 array
       MapControl: float32
   }
   
   let extractMLFeatures (worldState: WorldState) : MLFeatures
   ```

#### Acceptance Criteria:
- [ ] AI systems coordinate effectively
- [ ] Advanced spatial algorithms work correctly
- [ ] Feature extraction for ML is implemented
- [ ] Behavior is intelligent and adaptive

### Milestone 6.2: Final Optimization & Documentation
**Duration**: 3-4 days
**Goal**: Polish and prepare for release

#### Tasks:
1. **Performance Profiling**
   - Use dotTrace/PerfView to identify hotspots
   - Optimize critical paths
   - Validate SIMD usage where applicable

2. **Complete Documentation**
   ```markdown
   # API Documentation
   - F# Core API reference
   - C# compatibility guide
   - Performance optimization guide
   - Integration examples
   ```

3. **Example AI Implementations**
   - Simple economic AI
   - Aggressive military AI
   - Balanced strategy AI
   - Advanced ML-ready AI

#### Acceptance Criteria:
- [ ] Performance exceeds baseline by 3x minimum
- [ ] Documentation is complete and accurate
- [ ] Examples demonstrate key features
- [ ] Code is production-ready

---

## Testing Strategy

### Unit Tests (Throughout Development)
```fsharp
// NUnit/xUnit tests for each component
[<TestFixture>]
type DataOrientedProcessingTests() =
    [<Test>]
    member this.``calculateHealthPercentages returns correct values``() = ...
    
    [<Test>]
    member this.``spatialGrid finds units in radius``() = ...
```

### Integration Tests (Each Milestone)
```fsharp
// End-to-end tests with mock Spring interface
[<TestFixture>]
type IntegrationTests() =
    [<Test>]
    member this.``AI completes full game lifecycle``() = ...
    
    [<Test>]
    member this.``Performance meets requirements under load``() = ...
```

### Performance Tests (Continuous)
```fsharp
// Benchmark tests using BenchmarkDotNet
[<MemoryDiagnoser>]
type PerformanceBenchmarks() =
    [<Benchmark>]
    member this.ProcessEventBatch() = ...
    
    [<Benchmark>]
    member this.GenerateCommands() = ...
```

---

## Success Metrics

### Performance Targets
- **Event Processing**: 3-5x faster than traditional callbacks
- **Memory Usage**: <50% of baseline OOP implementation
- **Frame Processing**: <5ms for 1000-unit scenarios
- **Spatial Queries**: <1ms for radius searches

### Quality Targets
- **Test Coverage**: >85% for core functionality
- **Documentation**: Complete API docs + tutorials
- **Compatibility**: Full C# interop with no performance loss
- **Stability**: No crashes in 24-hour stress testing

### Delivery Targets
- **Phase 1-2**: Basic functionality working
- **Phase 3-4**: Core features complete
- **Phase 5-6**: Production-ready release

---

## Risk Mitigation

### Technical Risks
1. **P/Invoke Performance**: Early prototyping and benchmarking
2. **Memory Management**: Continuous profiling and testing
3. **Spring Integration**: Mock interfaces for isolated testing

### Schedule Risks
1. **Complexity Underestimation**: Buffer time in each phase
2. **Learning Curve**: Pair programming and knowledge sharing
3. **Integration Issues**: Early integration testing

### Mitigation Strategies
- Weekly milestone reviews and adjustments
- Continuous integration and automated testing
- Performance monitoring throughout development
- Regular code reviews and architecture validation

This implementation plan provides a structured approach to building a high-performance, data-oriented F# AI wrapper with clear milestones, testable deliverables, and measurable success criteria.
