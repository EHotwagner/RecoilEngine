# F# RecoilEngine Wrapper - Standalone Design Plan

## Overview

The `FSharp-Recoil-Wrapper` is a **standalone, general-purpose F# wrapper** for the RecoilEngine/Spring RTS engine. Unlike BAR-specific wrappers, this provides a **pure, data-oriented interface** to the core RecoilEngine AI system that can be used by any RTS game or AI project built on Spring/RecoilEngine.

## Architecture Goals

### Core Principles
1. **Engine Agnostic**: Works with any Spring/RecoilEngine-based RTS game
2. **Data-Oriented**: Pure functional pipeline with transparent data flow
3. **Zero Hidden State**: All game state visible and immutable at each frame
4. **Batch Processing**: Commands processed in efficient batches
5. **Testable**: Can be tested independently without full engine integration

### Performance Targets
- **Sub-millisecond** command batch processing
- **Cache-friendly** Structure-of-Arrays (SOA) data layout  
- **SIMD-optimized** array operations where possible
- **Minimal allocations** during steady-state operation

## Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    F# RecoilEngine Wrapper                 │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Core Types    │  │  Game Context   │  │   Commands      │ │
│  │                 │  │                 │  │                 │ │
│  │ • WorldState    │  │ • Resources     │  │ • Move/Attack   │ │
│  │ • Unit Arrays   │  │ • Map Info      │  │ • Build/Stop    │ │
│  │ • Spatial Grid  │  │ • Frame Data    │  │ • Batch Exec    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   P/Invoke      │  │  Native Stub    │  │   Mock Layer    │ │
│  │                 │  │                 │  │                 │ │
│  │ • Array Fills   │  │ • C++ Interface │  │ • Test Data     │ │
│  │ • Marshaling    │  │ • Direct Memory │  │ • No Engine     │ │
│  │ • Error Handle  │  │ • Export Funcs  │  │ • Validation    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│             RecoilEngine/Spring AI Interface               │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Component Design

### 1. Core Types (`Types.fs`)

**Purpose**: Define all fundamental data structures with units of measure

```fsharp
// Core units of measure (engine-agnostic)
[<Measure>] type elmo      // Engine length units
[<Measure>] type frame     // Game simulation frames
[<Measure>] type hp        // Health points
[<Measure>] type metal     // Resource: Metal
[<Measure>] type energy    // Resource: Energy

// Fundamental game state (SOA design)
type WorldState = {
    Units: Unit array           // All units in SOA layout
    Resources: Resources        // Current resource state
    MapWidth: float32<elmo>     // Map dimensions
    MapHeight: float32<elmo>
    CurrentFrame: int<frame>    // Current simulation frame
}

// Unit data (cache-friendly layout)
type Unit = {
    Id: int                     // Unique unit identifier
    DefId: int                  // Unit definition ID
    Position: Vector3           // World position
    Health: float32<hp>         // Current health
    MaxHealth: float32<hp>      // Maximum health
    TeamId: int                 // Owning team
    State: UnitState           // Current unit state
}

// Command types (discriminated unions)
type Command =
    | Move of unitId: int * position: Vector3
    | Attack of attackerId: int * targetId: int  
    | Build of builderId: int * unitType: int * position: Vector3
    | Stop of unitId: int
    | Guard of unitId: int * targetId: int
```

### 2. Game Context (`GameContext.fs`)

**Purpose**: Manage game state queries and spatial operations

```fsharp
module GameContext =
    
    /// High-level world state access
    type IGameContext =
        abstract GetWorldState: unit -> WorldState
        abstract GetResources: unit -> Resources
        abstract GetUnitsInRadius: center:Vector3 -> radius:float32<elmo> -> Unit array
        abstract GetEnemyUnits: unit -> Unit array
        abstract GetFriendlyUnits: unit -> Unit array
        
    /// Spatial query operations (grid-based)
    module Spatial =
        let getUnitsInRadius (worldState: WorldState) (center: Vector3) (radius: float32<elmo>) : Unit array = 
            // Efficient spatial grid lookup
            
        let getNearestUnit (worldState: WorldState) (position: Vector3) (teamId: int) : Unit option =
            // Find closest unit of specified team
            
        let getUnitsInArea (worldState: WorldState) (min: Vector3) (max: Vector3) : Unit array =
            // Get all units in rectangular area
```

### 3. Commands (`Commands.fs`)

**Purpose**: Handle command creation, validation, and batch execution

```fsharp
module Commands =
    
    /// Command batch execution result
    type CommandBatchResult = {
        SuccessCount: int
        FailureCount: int
        ExecutionTimeMs: float
    }
    
    /// Command validation and execution
    module Execution =
        let validateCommand (worldState: WorldState) (command: Command) : bool =
            // Validate command against current world state
            
        let createCommandBatch (commands: Command list) : NativeCommand array =
            // Convert F# commands to native format
            
        let executeCommandBatch (commands: Command array) : CommandBatchResult =
            // Execute batch via P/Invoke or mock
```

### 4. P/Invoke Interface (`Interop.fs`)

**Purpose**: Direct communication with native RecoilEngine AI interface

```fsharp
module NativeInterop =
    
    // Native data structures (matching C++ layout)
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type NativeUnit = {
        id: int
        defId: int
        x: float32; y: float32; z: float32
        health: float32
        maxHealth: float32
        teamId: int
        state: int
    }
    
    // P/Invoke declarations
    [<DllImport("RecoilAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int fillWorldState(NativeUnit* units, int maxUnits, float32* resources)
    
    [<DllImport("RecoilAIWrapper", CallingConvention = CallingConvention.Cdecl)>]
    extern int executeCommands(NativeCommand* commands, int commandCount)
    
    // High-level wrapper functions
    let getWorldState() : WorldState = 
        // Call native function and convert to F# types
        
    let executeCommandBatch(commands: Command array) : CommandBatchResult =
        // Convert to native and execute
```

### 5. Native C++ Stub (`native/`)

**Purpose**: Minimal C++ interface for direct memory access

```cpp
// SpringAIWrapperInterface.h
extern "C" {
    // Core data access functions
    int fillWorldState(NativeUnit* units, int maxUnits, float* resources);
    int fillSpatialGrid(int* grid, int gridWidth, int gridHeight);
    int executeCommands(NativeCommand* commands, int commandCount);
    
    // Utility functions
    int getMapInfo(float* width, float* height);
    int getCurrentFrame();
}
```

## Testing Strategy

### 1. Mock Testing Layer

Create a complete mock implementation that allows testing without RecoilEngine:

```fsharp
// MockInterop.fs
module MockInterop =
    
    let getWorldState() : WorldState =
        // Return predefined test data
        {
            Units = [| 
                { Id = 1; DefId = 101; Position = Vector3(100.0f, 0.0f, 100.0f); ... }
                { Id = 2; DefId = 102; Position = Vector3(200.0f, 0.0f, 200.0f); ... }
            |]
            Resources = { Metal = 1000.0f<metal>; Energy = 500.0f<energy>; ... }
            MapWidth = 2048.0f<elmo>
            MapHeight = 2048.0f<elmo>
            CurrentFrame = 1<frame>
        }
    
    let executeCommandBatch(commands: Command array) : CommandBatchResult =
        // Mock successful execution
        { SuccessCount = commands.Length; FailureCount = 0; ExecutionTimeMs = 1.0 }
```

### 2. Unit Tests

```fsharp
// Tests/CoreTests.fs
module CoreTests =
    
    [<Test>]
    let ``WorldState creation and access`` () =
        let worldState = MockInterop.getWorldState()
        Assert.AreEqual(2, worldState.Units.Length)
        Assert.AreEqual(1000.0f<metal>, worldState.Resources.Metal)
    
    [<Test>]
    let ``Command batch processing`` () =
        let commands = [| Move(1, Vector3(300.0f, 0.0f, 300.0f)) |]
        let result = MockInterop.executeCommandBatch(commands)
        Assert.AreEqual(1, result.SuccessCount)
    
    [<Test>]
    let ``Spatial queries`` () =
        let worldState = MockInterop.getWorldState()
        let center = Vector3(150.0f, 0.0f, 150.0f)
        let nearbyUnits = GameContext.Spatial.getUnitsInRadius worldState center 100.0f<elmo>
        Assert.IsTrue(nearbyUnits.Length > 0)
```

### 3. Integration Tests

```fsharp
// Tests/IntegrationTests.fs
module IntegrationTests =
    
    [<Test>]
    let ``Full pipeline test`` () =
        // Test complete data flow: 
        // WorldState -> Analysis -> Commands -> Execution -> Validation
        let worldState = MockInterop.getWorldState()
        
        // Simple AI logic
        let commands = 
            worldState.Units
            |> Array.filter (fun u -> u.State = UnitState.Idle)
            |> Array.map (fun u -> Move(u.Id, Vector3(u.Position.X + 100.0f, 0.0f, u.Position.Z)))
        
        let result = MockInterop.executeCommandBatch(commands)
        Assert.AreEqual(commands.Length, result.SuccessCount)
```

## Project Structure

```
FSharp-Recoil-Wrapper/
├── src/
│   ├── Types.fs                    # Core data types
│   ├── GameContext.fs              # World state access
│   ├── Commands.fs                 # Command processing
│   ├── Interop.fs                  # P/Invoke interface
│   └── RecoilAI.Core.fsproj        # Main library project
├── native/
│   ├── SpringAIWrapperInterface.h  # C++ header
│   ├── SpringAIWrapperInterface.cpp # C++ implementation
│   ├── SpringAIWrapperExports.cpp  # Export definitions
│   ├── CMakeLists.txt              # Build configuration
│   └── test_wrapper.cpp            # Native unit tests
├── test/
│   ├── MockInterop.fs              # Mock implementation
│   ├── CoreTests.fs                # Unit tests
│   ├── IntegrationTests.fs         # Integration tests
│   └── RecoilAI.Tests.fsproj       # Test project
├── examples/
│   ├── BasicAI.fs                  # Simple AI example
│   ├── DataFlow.fs                 # Data pipeline demo
│   └── Examples.fsproj             # Example project
├── build-scripts/
│   ├── build-native.sh             # Unix native build
│   ├── build-native.cmd            # Windows native build
│   ├── test-all.sh                 # Unix full test
│   └── test-all.cmd                # Windows full test
└── README.md                       # Usage documentation
```

## Independent Testing Approach

### Phase 1: Pure F# Logic Testing
1. **Mock-Only Tests**: Test all F# logic without any native dependencies
2. **Data Structure Validation**: Verify types, serialization, and array operations  
3. **Algorithm Testing**: Test spatial queries, command generation, etc.

### Phase 2: Native Interface Testing  
1. **Stub Library**: Build minimal C++ stub that returns test data
2. **P/Invoke Validation**: Test marshaling and memory management
3. **Performance Benchmarks**: Measure array filling and command execution speed

### Phase 3: Engine Integration Testing
1. **RecoilEngine AI Interface**: Connect to actual Spring AI callbacks
2. **Live Game Testing**: Test with actual RTS game scenarios
3. **Stress Testing**: High unit counts, rapid command batches

## Testing Commands

```bash
# Phase 1: Mock testing (no dependencies)
cd FSharp-Recoil-Wrapper
dotnet test test/RecoilAI.Tests.fsproj --filter "Category=Mock"

# Phase 2: Native interface testing (requires CMake)
cd native && cmake . && make
cd .. && dotnet test test/RecoilAI.Tests.fsproj --filter "Category=Native"

# Phase 3: Full integration (requires RecoilEngine)
dotnet test test/RecoilAI.Tests.fsproj --filter "Category=Integration"
```

## Key Benefits of This Design

1. **Standalone Testing**: Can validate all F# logic without RecoilEngine installation
2. **Performance Focus**: Data-oriented design optimized for high-frequency AI operations  
3. **Engine Agnostic**: Works with any Spring/RecoilEngine-based game
4. **Progressive Integration**: Test complexity increases gradually from pure F# to full engine
5. **Clear Separation**: Mock, native, and integration layers are completely separate

This design allows full development and testing of the F# wrapper logic independently, with native integration as the final step.
