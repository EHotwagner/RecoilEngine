# .NET AI Wrapper for RecoilEngine

This directory contains a .NET wrapper for creating AI implementations for the RecoilEngine (Spring) game engine.

## Overview

The .NET wrapper allows you to write AI implementations in C#, F#, and other .NET languages. It provides a bridge between the native C AI interface and managed .NET code, handling marshalling of data types, events, and commands.

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
- **DotNetInterface.h/cpp**: Main interface class that manages .NET AI instances
- **DotNetExport.cpp**: C exports required by the Spring AI interface system

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
