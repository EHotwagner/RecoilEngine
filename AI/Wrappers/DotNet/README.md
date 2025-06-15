# .NET AI Wrapper for RecoilEngine/Spring

**F#-First Data-Oriented AI Development Platform for Beyond All Reason (BAR)**

This wrapper provides a modern, high-performance .NET interface for developing AI for the RecoilEngine (formerly Spring Engine). Designed with a data-oriented architecture and F#-first approach, it enables efficient batch processing and cache-friendly AI systems optimized for Beyond All Reason (BAR).

## 🎯 Key Features

- **F#-First Design**: Leverages F#'s superior type system and array processing capabilities
- **Data-Oriented Architecture**: Arrays and batch processing for optimal performance
- **C# Compatibility**: Full compatibility layer for C# developers
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
    
    subgraph "C# Compatibility"
        J[OOP Event Adapters]
        K[Traditional Interfaces]
        L[Type Converters]
    end
    
    subgraph "Native Layer"
        M[RecoilEngine Callbacks]
        N[P/Invoke Interface]
    end
    
    A --> F
    A --> G
    A --> H
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
    J --> "C# Traditional AI"
```

### Why F#-First Data-Oriented Design?

Real-time strategy games require high-performance AI that can process hundreds of units efficiently:

- **Data locality**: Arrays keep related data together for better cache performance
- **Batch processing**: Process entire arrays of units/events at once
- **Minimal allocations**: Reuse arrays and avoid GC pressure during frames
- **Functional composition**: F#'s array processing is highly optimized
- **Type safety**: Units of measure and discriminated unions prevent common bugs
- **Immutable snapshots**: Pure functions enable easy testing and debugging

**C# developers still get full support** through the compatibility layer, but benefit from F#'s optimizations automatically.

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

### C# Traditional Development (Compatibility)

```csharp
using SpringAI.CSharp.AI;

public class MyAI : BaseCSharpAI
{
    protected override void ProcessUpdate(int frame, ResourceState resources, Strategy strategy)
    {
        var builders = GetFriendlyUnits().Where(u => u.IsBuilder).ToList();
        
        foreach (var builder in builders.Take(1))
        {
            if (resources.CanAfford(100, 50)) // F# type safety prevents bugs
            {
                var command = new BuildCommand(builder.Id, "armlab", builder.Position);
                ExecuteCommand(TypeConverters.ToFSCommand(command)); // F# validation
            }
        }
    }
}
```

## 📁 Project Structure

```
src/
├── fsharp-core/           # Primary F# API
│   ├── Types.fs           # Discriminated unions, records, units of measure
│   ├── GameContext.fs     # F# interfaces and context
│   ├── AI.fs              # F# AI interface with computation expressions
│   └── ActivePatterns.fs  # Pattern matching helpers
│
├── csharp-compat/         # C# compatibility layer
│   ├── AI/               # C# AI interfaces and adapters
│   ├── Commands/         # C# command classes
│   ├── Compatibility/    # F#↔C# type converters
│   └── Events/           # C# event classes
│
├── native/               # Native C++ interop
└── managed/              # Legacy/transition code (being phased out)

examples/
├── FSharp/               # F# example AIs
└── CSharp/               # C# example AIs using F# core

docs/
├── Architecture.md       # Detailed architecture documentation
├── BarIntegration.md     # BAR-specific integration details
├── FSharpConsiderations.md # F# language design considerations
└── F#-First-Architecture.md # Complete F#-first design guide
```

## 🔧 Building

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- CMake 3.15+ (for native components)
- C++17 compatible compiler

### Build Commands

```bash
# Build F# core (primary API)
cd src/fsharp-core
dotnet build

# Build C# compatibility layer
cd src/csharp-compat
dotnet build

# Build native interop layer
mkdir build && cd build
cmake ..
cmake --build .
```

## 📚 Documentation

- **[F#-First Architecture](F%23-First-Architecture.md)** - Complete design philosophy and implementation
- **[Architecture Overview](Architecture.md)** - System architecture and data flow
- **[BAR Integration](BarIntegration.md)** - BAR-specific features and data access
- **[F# Considerations](FSharpConsiderations.md)** - F# language design decisions

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

1. **F# developers**: Contribute to the core API in `src/fsharp-core/`
2. **C# developers**: Improve compatibility layer in `src/csharp-compat/`
3. **Game experts**: Enhance BAR-specific features and examples
4. **Documentation**: Help improve guides and examples

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Implement changes (F# core first, then C# compatibility)
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

**Ready to build the next generation of RTS AI?** Start with F# for maximum power, or use C# for familiarity - either way, you get the benefits of F#'s superior type system.

## Overview

The .NET wrapper allows you to write AI implementations in C#, F#, and other .NET languages. It provides a bridge between the native C AI interface and managed .NET code, handling marshalling of data types, events, and commands.

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

See `examples/ExampleDotNetAI.cs` for a complete C# example that demonstrates:
- Basic initialization
- Resource monitoring
- Unit management
- Enemy engagement
- Event handling

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
