# Implementation Plan: Pure F# Data-Oriented AI Wrapper for RecoilEngine/Spring

## Project Overview

**Goal**: Create a high-performance, pure F# data-oriented AI wrapper for RecoilEngine/Spring, optimized for Beyond All Reason (BAR) AI development with transparent data pipeline architecture.

**Duration**: ~6-8 weeks (pure F# data-oriented implementation)
**Target Performance**: 3-5x improvement over traditional object-oriented approaches

---

## Phase 1: Foundation & Native Interface (Week 1-2)

### Milestone 1.1: Native C++ Interface Setup
**Duration**: 3-4 days
**Goal**: Establish basic native interop layer

#### Tasks:
1. **Create Native C++ Stub Library**   ```bash
   # Directory structure
   native/
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
```fsharp
[<Test>]
let ``NativeInterface GetMetal returns value`` () =
    let metal = NativeInterop.GetMetal()
    metal |> should be (greaterThanOrEqualTo 0.0f<metal>)
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

## Phase 3: Data Pipeline & Array Processing (Week 5-6)

### Milestone 3.1: Data-Oriented Pipeline Architecture
**Duration**: 4-5 days
**Goal**: Implement transparent data flow pipeline without event abstractions

#### Tasks:
1. **World State Array Types**
   ```fsharp
   type WorldState = {
       Frame: int
       Units: Unit[]
       Resources: ResourceState
       SpatialGrid: SpatialGrid
       LastUpdateTime: int64
   }
   
   type Unit = {
       Id: int
       DefId: int
       Position: Vector3
       Health: float32<health>
       TeamId: int
       State: UnitState
   }
   ```

2. **Direct Array Fill System**
   ```fsharp
   module NativeArrayFiller =
       // Direct P/Invoke functions that fill F# arrays
       [<DllImport("SpringAIWrapper")>]
       extern int FillUnitArray(Unit[] units, int maxCount)
       
       [<DllImport("SpringAIWrapper")>]
       extern bool FillResourceState(ResourceState& resources)
       
       let updateWorldState (worldState: WorldState) : WorldState =
           // Native code fills arrays directly - no event processing
           let unitCount = FillUnitArray(worldState.Units, worldState.Units.Length)
           let mutable resources = worldState.Resources
           FillResourceState(&resources)
           { worldState with Resources = resources }
   ```

3. **Pure Function Processing Pipeline**
   ```fsharp
   let processAIFrame (aiFunction: WorldState -> Command[]) (worldState: WorldState) : Command[] =
       // Simple, transparent pipeline: fill arrays → process → return commands
       let updatedWorld = NativeArrayFiller.updateWorldState worldState
       aiFunction updatedWorld
   ```

#### Acceptance Criteria:
- [ ] World state arrays are filled directly by native code
- [ ] No event abstraction layer - pure data transformation
- [ ] AI function receives complete world state snapshot
- [ ] Memory layout is cache-friendly (Structure-of-Arrays)

#### Test Plan:
```fsharp
[<Test>]
let ``World state arrays are filled correctly`` () =
    let worldState = generateTestWorldState()
    let mutable processedCount = 0
    let result = processWorldState worldState (fun _ -> processedCount <- processedCount + 1)
    processedCount |> should equal worldState.UnitIds.Length
```

### Milestone 3.2: AI Interface with Batch Support
**Duration**: 3-4 days
**Goal**: Implement F# AI interface with batch operations

#### Tasks:
1. **F# AI Interface with Pure Functions**
   ```fsharp
   type IAI =
       abstract member ProcessWorldState: WorldState -> Command array
       abstract member UpdateStrategy: WorldState -> unit
   ```

2. **Base AI Implementation**
   ```fsharp
   [<AbstractClass>]
   type BaseFSharpAI(context: IGameContext) =
       let mutable worldStateCache: WorldState option = None
       let mutable spatialGridCache: SpatialGrid option = None
       
       member this.ProcessFrame(worldState: WorldState) = 
           // Efficient data processing implementation
   ```

3. **Performance Monitoring**
   ```fsharp
   type PerformanceMetrics = {
       FrameTime: TimeSpan
       DataProcessingTime: TimeSpan
       CommandGenerationTime: TimeSpan
       MemoryUsage: int64
   }
   ```

#### Acceptance Criteria:
- [ ] AI processes world state data efficiently
- [ ] World state caching works correctly
- [ ] Performance metrics are captured
- [ ] Strategy updates based on data analysis

#### Test Plan:
```fsharp
[<Test>]
let ``AI processes world state within time limit`` () =
    let ai = TestAI()
    let worldState = generateLargeWorldState()
    let stopwatch = Stopwatch.StartNew()
    ai.ProcessWorldState(worldState)
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
    let (_, allocations1) = measureGCPressure (fun () ->        for i in 1..1000 do Array.zeroCreate<int> 100 |> ignore)
    
    let (_, allocations2) = measureGCPressure (fun () ->
        for i in 1..1000 do 
            let arr = ArrayPools.intPool.GetArray(100)
            ArrayPools.intPool.ReturnArray(arr))
    
    allocations2 |> should be (lessThan (allocations1 / 2L))
```

---

## Phase 5: BAR AI Implementation & Examples (Week 7-8)

### Milestone 5.1: BAR-Specific AI Logic
**Duration**: 4-5 days
**Goal**: Create complete working BAR AI using the pure F# wrapper

#### Tasks:
1. **BAR Unit Analysis Functions**
   ```fsharp
   module BARUnitAnalyzer =
       let classifyUnit (unit: Unit) (unitDef: UnitDefinition) : BARUnitClass =
           match unitDef.Categories with
           | cats when Array.contains "COMMANDER" cats -> Commander
           | cats when Array.contains "BUILDER" cats -> Builder
           | cats when Array.contains "FACTORY" cats -> Factory
           | _ -> Generic
   ```

2. **Economic Strategy Functions**
   ```fsharp
   module BAREconomy =
       let planEconomicBuilding (world: WorldState) : Command[] =
           let metalRatio = world.Resources.Metal / world.Resources.MetalStorage
           if metalRatio < 0.2f<ratio> then
               buildMetalExtractors world
           else
               buildEnergyProducers world
   ```

3. **Complete BAR AI Example**
   ```fsharp
   let barAIFunction (world: WorldState) : Command[] =
       [|
           yield! BAREconomy.planEconomicBuilding world
           yield! BARMilitary.planUnitProduction world
           yield! BARMilitary.planCombatActions world
       |]
   ```

#### Acceptance Criteria:
- [ ] Complete working BAR AI that can play games
- [ ] AI demonstrates economic and military strategies
- [ ] Performance meets 30Hz frame rate requirements
- [ ] All BAR unit types are properly handled

#### Test Plan:
```fsharp
[<Test>]
let ``BAR AI can manage economy effectively`` () =
    let world = TestHelper.createTestWorldState()
    let commands = barAIFunction world
    commands |> should contain (fun c -> match c with Build _ -> true | _ -> false)
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
       virtual int FillUnitArray(Unit* units, int maxCount) override;
       virtual int FillResourceState(ResourceState* resources) override;
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
   - Data-oriented patterns guide
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
// BenchmarkDotNet tests for performance validation
[<MemoryDiagnoser>]
type PerformanceBenchmarks() =
    [<Benchmark>]
    member this.ProcessWorldState() = ...
    
    [<Benchmark>]
    member this.GenerateCommands() = ...
```

---

## Success Metrics

### Performance Targets
- **Data Processing**: 3-5x faster than traditional object-oriented callbacks
- **Memory Usage**: <50% of baseline OOP implementation
- **Frame Processing**: <5ms for 1000-unit scenarios
- **Spatial Queries**: <1ms for radius searches

### Quality Targets
- **Test Coverage**: >85% for core functionality
- **Documentation**: Complete API docs + tutorials
- **Performance**: Consistent sub-frame processing times
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
