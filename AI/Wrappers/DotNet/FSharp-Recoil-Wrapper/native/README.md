# Native C++ Stub Library for F# Data-Oriented Spring AI

This directory contains the Native C++ stub library that provides the foundational P/Invoke interface for the F# data-oriented Spring AI wrapper.

## Directory Structure

```
native/
├── SpringAIWrapperInterface.h      # Header with data structures and function declarations
├── SpringAIWrapperInterface.cpp    # Implementation with mock data for testing
├── SpringAIWrapperExports.cpp      # Export functions for P/Invoke calls
├── CMakeLists.txt                  # Build configuration
├── test_wrapper.cpp               # Simple test program
└── README.md                      # This file
```

## Architecture

This is a **simplified, data-oriented** approach compared to traditional Spring AI interfaces:

### Key Principles:
1. **Direct Array Filling**: Native code fills F# arrays directly via P/Invoke
2. **Structure of Arrays (SOA)**: Data is organized for cache-efficient processing
3. **Batch Operations**: Commands and queries operate on arrays, not individual objects
4. **No Event Abstraction**: Pure data transformation without event-driven patterns
5. **Minimal C++ Complexity**: Simple C functions, no complex class hierarchies

### Core Data Structures:

```cpp
// Unit data in Structure-of-Arrays style
typedef struct {
    int id;
    int defId;
    float x, y, z;              // Position
    float health;
    float maxHealth;
    int teamId;
    int state;
} Unit;

// Resource state snapshot
typedef struct {
    float metal;
    float energy;
    float metalStorage;
    float energyStorage;
    float metalIncome;
    float energyIncome;
    int currentFrame;
} ResourceState;

// Command for batch execution
typedef struct {
    int commandType;
    int unitId;
    int targetUnitId;
    float x, y, z;
    char buildUnitName[64];
    int priority;
} Command;
```

## Key Functions

### Array Filling (Core Interface)
- `FillUnitArray()` - Fills array with current unit data
- `FillResourceState()` - Fills resource state structure
- `ExecuteCommandBatch()` - Executes array of commands

### Queries
- `GetUnitCount()` - Returns current unit count
- `GetMetal()`, `GetEnergy()` - Resource queries
- `GetUnitsInRadius()` - Spatial queries for AI processing

### Map Information
- `GetMapWidth()`, `GetMapHeight()` - Map dimensions
- `IsPositionValid()` - Position validation

## Building

### Windows (Visual Studio)
```bash
mkdir build && cd build
cmake -G "Visual Studio 17 2022" ..
cmake --build . --config Release
```

### Linux/Mac
```bash
mkdir build && cd build
cmake ..
make -j4
```

### Testing
```bash
# Run the test program
./SpringAIWrapperTest  # Linux/Mac
.\Release\SpringAIWrapperTest.exe  # Windows
```

## Integration with F#

The native library is designed to be called from F# via P/Invoke:

```fsharp
// F# P/Invoke declarations
[<DllImport("SpringAIWrapper")>]
extern int FillUnitArray(Unit[] units, int maxCount)

[<DllImport("SpringAIWrapper")>]
extern int FillResourceState(ResourceState& resources)

[<DllImport("SpringAIWrapper")>]
extern int ExecuteCommandBatch(Command[] commands, int commandCount)
```

## Mock Data for Testing

Currently provides mock data for development and testing:
- 5 test units with different types (Commander, Builder, Scout, Fighter, Factory)
- Mock resource state with simulated income
- Simple spatial queries and command execution

## Future Integration

When integrated with actual Spring Engine:
- Replace mock data with real Spring API calls
- Connect to actual unit management system
- Implement real command execution
- Add proper error handling and validation

## Performance Characteristics

- **Cache Friendly**: SOA layout minimizes cache misses
- **Batch Oriented**: Reduces P/Invoke overhead
- **Minimal Allocations**: Uses pre-allocated arrays
- **Direct Memory Access**: No object marshaling overhead

This design targets 3-5x performance improvement over traditional event-driven AI interfaces.
