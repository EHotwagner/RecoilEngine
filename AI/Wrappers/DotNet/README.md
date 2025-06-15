# F# RecoilEngine AI Development Platform

**Pure F# Data-Oriented AI Development for Beyond All Reason (BAR)**

This platform provides a modern, high-performance F# interface for developing AI for the RecoilEngine (formerly Spring Engine). Designed with a pure data-oriented architecture, it enables efficient batch processing and cache-friendly AI systems optimized for Beyond All Reason (BAR).

## 🎯 Key Features

- **Pure F# Design**: Leverages F#'s superior type system and array processing capabilities
- **Data-Oriented Architecture**: Arrays and batch processing for optimal performance
- **Type Safety**: Units of measure prevent resource calculation bugs
- **Cache-Friendly**: Structured data layout optimized for modern CPU architectures
- **Batch Processing**: Event collections and world state arrays for efficient updates
- **Pattern Matching**: Exhaustive matching ensures all game states are handled
- **Zero-Allocation**: Designed to minimize GC pressure during frame processing

## 🏗️ Data-Oriented Architecture

```mermaid
graph TB
    subgraph "F# Core (Data-Oriented)"
        A[World State Arrays]
        B[Event Collections]
        C[Batch Processing]
        D[Units of Measure]
        E[Discriminated Unions]
    end
    
    subgraph "AI Systems"
        F[Economy System]
        G[Military System]
        H[Build System]
        I[Scouting System]
    end
    
    subgraph "Native Layer"
        M[RecoilEngine Callbacks]
        N[P/Invoke Interface]
    end
    
    A --> F
    A --> G
    A --> H
    A --> I
    
    M --> A
    N --> B
    A --> I
    B --> F
    B --> G
    B --> H
    B --> I
    
    M --> N
    N --> B
    N --> A
    A --> J
    B --> J
    J --> K
      A --> "F# Data-Oriented AI"
```

## 📁 Project Structure

This platform is organized into two distinct components:

### FSharp-Recoil-Wrapper/
**Pure F# engine wrapper** - Provides direct, efficient access to RecoilEngine functionality
- `src/` - F# wrapper core files (Types.fs, Interop.fs, Commands.fs, etc.)
- `native/` - Native C++ P/Invoke interface files
- Purpose: Low-level, data-oriented engine access

### FSharp-BAR-AI/
**BAR-specific AI implementation** - High-level AI logic for Beyond All Reason
- `src/` - F# BAR AI implementation files
- `examples/` - Complete working AI examples and patterns
- Purpose: Game-specific AI strategies and behaviors

This separation ensures:
- **Clean dependencies**: Wrapper has no game-specific logic
- **Reusability**: Engine wrapper can be used for other Spring games
- **Maintainability**: Clear boundary between engine interface and AI logic
- **Performance**: Each component optimized for its specific role

### Why F#-First Data-Oriented Design?

Real-time strategy games require high-performance AI that can process hundreds of units efficiently:

- **Data locality**: Arrays keep related data together for better cache performance
- **Batch processing**: Process entire arrays of units/events at once
- **Minimal allocations**: Reuse arrays and avoid GC pressure during frames
- **Functional composition**: F#'s array processing is highly optimized
- **Type safety**: Units of measure and discriminated unions prevent common bugs
- **Immutable snapshots**: Pure functions enable easy testing and debugging

## 🚀 Quick Start

### F# Data-Oriented Development (Recommended)

```fsharp
open SpringAI.Core

type DataOrientedAI() =
    // Mutable arrays for performance
    let mutable worldState = WorldSnapshot.empty
    let mutable economyTargets: EconomyTarget[] = [||]
    let eventBuffer = ResizeArray<GameEvent>()
    
    interface IAI with
        member this.OnEvent(event) =
            // Buffer events for batch processing
            eventBuffer.Add(convertToFSharpEvent event)
        
        member this.OnUpdate(frame) =
            // Batch process all events for this frame
            let frameEvents = this.ProcessEventBuffer(frame)
            
            // Update world state with efficient array operations
            worldState <- this.UpdateWorldState(worldState, frameEvents)
            
            // Run AI systems on batched data
            this.RunEconomySystem(worldState, frameEvents)
            this.RunMilitarySystem(worldState, frameEvents)
            
            eventBuffer.Clear()
    
    member private this.RunEconomySystem(world: WorldSnapshot, events: FrameEvents) =
        // Process idle builders in batch
        let idleBuilders = 
            events.UnitsIdle
            |> Array.filter (fun unitId -> this.IsBuilder(world.Units.[unitId]))
          // Assign construction tasks efficiently
        let tasks = this.PlanConstruction(world, idleBuilders)
        tasks |> Array.iter this.IssueConstructionOrder
```

## 🔧 Building

### Prerequisites

- .NET 8.0 SDK or later
- F# support (comes with .NET SDK)
- Visual Studio 2022, VS Code, or JetBrains Rider
- CMake 3.15+ (for native components)
- C++17 compatible compiler

### Build Commands

```bash
# Build F# wrapper core
cd FSharp-Recoil-Wrapper/src
dotnet build

# Build F# BAR AI implementation
cd FSharp-BAR-AI/src
dotnet build

# Build native interop layer
cd FSharp-Recoil-Wrapper/native
mkdir build && cd build
cmake ..
cmake --build .

# Build native interop layer
mkdir build && cd build
cmake ..
cmake --build .
```

## 📚 Documentation

- **[F# Data-Oriented Architecture](FSharp-DataOriented-Architecture.md)** - Complete design philosophy and implementation
- **[Architecture Overview](Architecture.md)** - System architecture and data flow
- **[BAR Integration](BarIntegration.md)** - BAR-specific features and data access
- **[Implementation Plan](IMPLEMENTATION_PLAN.md)** - Step-by-step development roadmap
- **[Quick Start Guide](QUICK_START_GUIDE.md)** - Getting started quickly

## 🎮 BAR-Specific Features

### Type-Safe Resource Management

```fsharp
[<Measure>] type metal
[<Measure>] type energy

let buildLab (resources: ResourceState) =
    if resources.Metal >= 100.0f<metal> && resources.Energy >= 50.0f<energy> then
        Some (Build(builderId, "armlab", position))
    else None
```

### BAR Faction Support

```fsharp
type BARFaction = ARM | COR | Unknown

let selectUnits faction =
    match faction with
    | ARM -> ["armcom"; "armlab"; "armvp"; "armstump"]
    | COR -> ["corcom"; "corlab"; "corvp"; "corraid"]
    | Unknown -> []
```

### BAR Game Phase Detection

```fsharp
let determinePhase frame resources =
    match frame, resources with
    | f, _ when f < 1800<frame> -> EarlyGame f
    | f, { Metal = m; Energy = e } when f < 7200<frame> && m > 500.0f<metal> -> MidGame f
    | f, _ -> LateGame f
```

## 🤝 Contributing

1. **F# developers**: Contribute to the core wrapper API in `FSharp-Recoil-Wrapper/src/`
2. **AI developers**: Enhance BAR-specific features in `FSharp-BAR-AI/src/`
3. **Game experts**: Improve BAR-specific strategies and examples
4. **Documentation**: Help improve guides and examples

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Implement changes (wrapper core or AI implementation)
4. Add tests and examples
5. Update documentation
6. Submit a pull request

## 📄 License

This project is licensed under the GPL v2+ - see the [LICENSE](../../LICENSE) file for details.

## 🙏 Acknowledgments

- **RecoilEngine Team** - For the excellent RTS engine
- **Beyond All Reason Community** - For the amazing game and mod ecosystem
- **F# Community** - For the language that makes this wrapper possible
- **Spring Engine Contributors** - For the foundation this builds upon

---

**Ready to build the next generation of RTS AI with F#?** Get started with the pure data-oriented approach for maximum performance and type safety.

## Overview

This F# platform provides a pure data-oriented interface for developing AI for the RecoilEngine. It handles marshalling of data types, events, and commands through direct P/Invoke interfaces, eliminating unnecessary layers and maximizing performance.

### .NET Naming Conventions

This wrapper follows standard .NET naming conventions:
- **PascalCase** for classes, methods, properties, and file names
- **camelCase** for parameters and local variables  
- **Descriptive names** that clearly indicate purpose
- **Consistent file naming** matching contained class names

### F# Support

The wrapper is designed to be highly F#-friendly, with:
- Type-safe domain modeling using discriminated unions and records
- Active patterns for pattern matching on game entities
- Option types instead of null references
- Railway-oriented programming for error handling
- Computation expressions for command chaining
- Functional reactive programming patterns for event handling

See `FSharpConsiderations.md` for detailed F# integration guidelines.

## Architecture

The wrapper consists of two main components:

### Native Layer (`src/native/`)
- **SpringAIWrapperInterface.h/cpp**: Main interface class that manages .NET AI instances
- **SpringAIWrapperExports.cpp**: C exports required by the Spring AI interface system

### Managed Layer (`src/managed/`)
- **Interop/**: P/Invoke declarations for calling native functions
- **Events/**: .NET event classes that wrap native game events
- **AI/**: Base classes and interfaces for AI implementations
- **Types/**: Game data type definitions

## Building

### Prerequisites
- .NET 8.0 SDK or later
- CMake 3.10 or later
- C++ compiler (Visual Studio 2019+ on Windows, GCC/Clang on Linux)

### Build Steps

1. Ensure .NET SDK is in your PATH:
   ```bash
   dotnet --version
   ```

2. Configure CMake with .NET wrapper enabled:
   ```bash
   cmake -DAI_TYPES=ALL ..
   # or specifically:
   cmake -DAI_TYPES=DOTNET ..
   ```

3. Build the project:
   ```bash
   cmake --build . --target DotNet-AIWrapper
   ```

This will build both the native interop library and the managed .NET assembly.

## Creating Your Own AI

### Step 1: Inherit from BaseAI

```csharp
using SpringAI;

public class MyAI : BaseAI
{
    public override void OnInit(int skirmishAIId, bool savedGame)
    {
        base.OnInit(skirmishAIId, savedGame);
        // Your initialization code here
    }

    public override void OnUpdate(int frame)
    {
        base.OnUpdate(frame);
        // Your per-frame logic here
    }

    // Override other event methods as needed...
}
```

### Step 2: Handle Game Events

The AI receives various events from the game engine:

- `OnInit()`: Called when the AI starts
- `OnUpdate()`: Called every game frame (~30 times per second)
- `OnUnitCreated()`: When a unit starts construction
- `OnUnitDamaged()`: When a unit takes damage
- `OnUnitDestroyed()`: When a unit is destroyed
- `OnRelease()`: When the AI should shut down

### Step 3: Issue Commands

Use the `Callback` property to interact with the game:

```csharp
// Move a unit
Callback.MoveUnit(unitId, new Vector3(x, y, z));

// Attack an enemy
Callback.AttackUnit(myUnitId, enemyUnitId);

// Get game information
var friendlyUnits = Callback.GetFriendlyUnits();
float metal = Callback.GetMetal();
```

## Example AI

See `FSharp-BAR-AI/examples/` for complete F# examples that demonstrate:
- Data-oriented world state management
- Efficient batch processing
- Resource monitoring with units of measure
- Unit management through arrays
- Type-safe event handling

For F# developers, see:
- `examples/FSharpAI.fs` - F# AI implementation using functional programming patterns
- `examples/FSharp/` - Complete F# project structure with type-safe domain modeling
- `FSharpConsiderations.md` - Comprehensive guide for F# integration and best practices

## Current Status

🔧 **Work in Progress**: This wrapper is currently under development.

### Completed:
- [x] Basic project structure
- [x] CMake build integration
- [x] Native C++ interface stub
- [x] .NET project configuration
- [x] Basic P/Invoke declarations
- [x] Event system design
- [x] AI base classes
- [x] Example AI implementation

### TODO:
- [ ] Complete P/Invoke declarations for all AI interface functions
- [ ] Implement event marshalling from C++ to .NET
- [ ] Implement command marshalling from .NET to C++
- [ ] Complete game callback interface implementation
- [ ] Memory management and resource cleanup
- [ ] Error handling and logging
- [ ] Performance optimization
- [ ] Unit tests
- [ ] Documentation and examples

## Contributing

To contribute to the .NET wrapper development:

1. Review the architecture analysis in `architecture.md`
2. Check the current TODO list above
3. Follow the existing code patterns and naming conventions
4. Add unit tests for new functionality
5. Update documentation as needed

## Troubleshooting

### Common Issues

**"DotNet-AIWrapper will not be built"**: 
- Ensure .NET SDK is installed and in PATH
- Check that `AI_TYPES` includes "ALL" or "DOTNET"

**Build failures**:
- Verify CMake version is 3.10+
- Check that all dependencies are available
- Review build logs for specific error messages

### Getting Help

- Review the architecture documentation in `architecture.md`
- Check existing AI wrapper implementations (C++, Java) for reference
- Consult the Spring engine documentation for AI interface details

## License

This wrapper follows the same licensing as the RecoilEngine project (GPL v2 or later).

## How RecoilEngine/Spring Delivers Events

Based on analysis of the RecoilEngine source code, the event delivery model is:

1. **Frame-based simulation loop**: RecoilEngine runs at 30 simulation frames per second (GAME_SPEED = 30)
2. **Event callbacks are immediate**: When events occur within a simulation frame, they are delivered immediately via callbacks (not batched)
3. **OnUpdate called every frame**: The Update event is sent once per simulation frame (30 times per second)
4. **No event streaming or batching**: Events like UnitCreated, UnitDestroyed, etc. are delivered as individual callbacks when they occur

**Timing and Order**:
- Each simulation frame: `CGame::SimFrame()` is called
- During simulation: Events occur and callbacks are triggered immediately
- At frame end: `Update(frameNum)` is called on all AIs
- Events are **not** queued or batched - they arrive as soon as they happen within the frame

This means your AI will receive:
```
OnUpdate(frame=1) -> UnitCreated(id=5) -> UnitDestroyed(id=3) -> ... -> OnUpdate(frame=2) -> ...
```

The F#-first wrapper maintains this immediate callback model while providing additional safety and functional programming benefits.
