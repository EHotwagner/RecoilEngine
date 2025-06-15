# F# Integration Considerations for the .NET AI Wrapper

## Overview

The .NET AI wrapper is designed to be language-agnostic within the .NET ecosystem. F# developers can leverage the wrapper to create sophisticated AI implementations using F#'s functional programming paradigms, pattern matching, and type safety features. This document outlines considerations and best practices for consuming the .NET wrapper from F#.

## F#-First Design Philosophy

### Why Design F#-First?

F# provides superior abstractions for game AI development:
- **Immutable data structures** prevent race conditions in concurrent scenarios
- **Discriminated unions** model game states more precisely than inheritance hierarchies
- **Pattern matching** creates more readable and maintainable decision trees
- **Option types** eliminate null reference exceptions
- **Units of measure** prevent unit conversion bugs
- **Computation expressions** provide clean error handling and async workflows

### F#-First Architecture

Instead of retrofitting F# onto C# designs, we can design the core API in F# and provide C# compatibility layers:

```fsharp
// Core F# API Design
namespace SpringAI.Core

[<Measure>] type metal
[<Measure>] type energy  
[<Measure>] type frame
[<Measure>] type elmo

// Primary F# event types
type GameEvent =
    | GameStarted of aiId: int * savedGame: bool
    | FrameUpdate of frame: int<frame>
    | UnitCreated of unitId: int * builderId: int * frame: int<frame>
    | UnitDamaged of unitId: int * attackerId: int * damage: float32 * frame: int<frame>
    | UnitDestroyed of unitId: int * attackerId: int * frame: int<frame>
    | GameEnded of reason: int

// Core resource state with units of measure
type ResourceState = {
    Metal: float32<metal>
    Energy: float32<energy>
    MetalIncome: float32<metal>
    EnergyIncome: float32<energy>
    CurrentFrame: int<frame>
}

// Primary AI interface designed for F#
type IGameContext =
    abstract member GetResources: unit -> ResourceState
    abstract member GetUnit: int -> UnitInfo option
    abstract member GetFriendlyUnits: unit -> UnitInfo list
    abstract member ExecuteCommand: Command -> Result<string, string>

// F# command types
type Command =
    | Build of builderId: int * unitDefName: string * position: Vector3
    | Move of unitId: int * destination: Vector3
    | Attack of attackerId: int * targetId: int
    | Stop of unitId: int
```

## F# Advantages for AI Development

### Functional Programming Benefits

F# offers several advantages for AI development:

```fsharp
// Immutable data structures prevent accidental state mutation
type GameState = {
    Frame: int
    Metal: float32
    Energy: float32
    Units: Unit list
}

// Pattern matching for complex decision trees
let determineStrategy gameState faction =
    match gameState with
    | { Metal = m; Energy = e } when m > 1000.0f && e > 2000.0f ->
        AggressiveExpansion
    | { Units = units } when List.length units < 10 ->
        EconomicFocus
    | _ -> DefensivePosition
```

### Type Safety and Domain Modeling

```fsharp
// Discriminated unions for type-safe unit classification
type BARFaction = ARM | COR
type UnitRole = 
    | Commander of health: float32
    | Builder of efficiency: float32
    | Factory of production: ProductionType
    | Combat of weapon: WeaponType
    | Economic of resourceType: ResourceType

// Option types for safe null handling
type UnitInfo = {
    Id: int
    DefId: int
    Faction: BARFaction option
    Role: UnitRole
    Position: Vector3
}
```

## Wrapper Design Considerations for F#

### 1. Interface Design Patterns

#### Functional-First Interfaces

```csharp
// C# interface designed for F# consumption
public interface IGameCallback
{
    // Return Option<T> equivalents for F#
    Unit? GetUnit(int unitId);  // Becomes Option<Unit> in F#
    
    // Use IReadOnlyList for F# list compatibility
    IReadOnlyList<Unit> GetFriendlyUnits();
    
    // Immutable value types
    GameResources GetResources();  // Instead of separate Metal/Energy calls
}

// Value types for immutability
public readonly struct GameResources
{
    public readonly float Metal;
    public readonly float Energy;
    public readonly float MetalIncome;
    public readonly float EnergyIncome;
    
    public GameResources(float metal, float energy, float metalIncome, float energyIncome)
    {
        Metal = metal;
        Energy = energy;
        MetalIncome = metalIncome;
        EnergyIncome = energyIncome;
    }
}
```

#### F#-Friendly Event Design

```csharp
// Events that work well with F# discriminated unions
public abstract class AIEvent
{
    public int Frame { get; }
    protected AIEvent(int frame) => Frame = frame;
}

public sealed class UnitCreatedEvent : AIEvent
{
    public int UnitId { get; }
    public int BuilderId { get; }
    
    public UnitCreatedEvent(int frame, int unitId, int builderId) 
        : base(frame) 
    {
        UnitId = unitId;
        BuilderId = builderId;
    }
}
```

### 2. F# Wrapper Module Design

#### Core F# Module Structure

```fsharp
// SpringAI.FSharp.fs - F#-specific wrapper module
module SpringAI.FSharp

open System
open SpringAI
open SpringAI.Events

// Type aliases for F# conventions
type GameCallback = IGameCallback
type AIEvent = SpringAI.Events.AIEvent

// F#-friendly discriminated union for events
type BAREvent =
    | UnitCreated of frame:int * unitId:int * builderId:int    
    | UnitDamaged of frame:int * unitId:int * attackerId:int * damage:float32
    | UnitDestroyed of frame:int * unitId:int * attackerId:int
    | GameUpdate of frame:int
    | GameInit of aiId:int * savedGame:bool
    | GameRelease of reason:int

// Better approach: Add conversion methods directly to C# event classes
// This avoids runtime type checking and is more efficient

// Option 1: Type extensions on C# classes (recommended)
type UnitCreatedEvent with
    member this.ToBAREvent() = UnitCreated(this.Frame, this.UnitId, this.BuilderId)

type UnitDamagedEvent with
    member this.ToBAREvent() = UnitDamaged(this.Frame, this.UnitId, this.AttackerId, this.Damage)

type UnitDestroyedEvent with
    member this.ToBAREvent() = UnitDestroyed(this.Frame, this.UnitId, this.AttackerId)

type UpdateEvent with
    member this.ToBAREvent() = GameUpdate(this.Frame)

type InitEvent with
    member this.ToBAREvent() = GameInit(this.SkirmishAIId, this.SavedGame)

type ReleaseEvent with
    member this.ToBAREvent() = GameRelease(this.Reason)

// Usage: Clean and efficient
let handleEvent (event: AIEvent) =
    match event with
    | :? UnitCreatedEvent as e -> e.ToBAREvent()
    | :? UnitDamagedEvent as e -> e.ToBAREvent()
    | :? UnitDestroyedEvent as e -> e.ToBAREvent()
    | :? UpdateEvent as e -> e.ToBAREvent()
    | :? InitEvent as e -> e.ToBAREvent()
    | :? ReleaseEvent as e -> e.ToBAREvent()
    | _ -> failwith $"Unknown event type: {event.GetType().Name}"

// Option 2: Even better - Add conversion method to base C# class
// Modify AIEvent in C# to include:
// public abstract BAREvent ToBAREvent();

// Then F# usage becomes:
let handleEventSimple (event: AIEvent) = event.ToBAREvent()

// Option 3: Static conversion module (if you can't modify C# classes)
module BAREventConverter =
    let convert (event: AIEvent) : BAREvent =
        match event with
        | :? UnitCreatedEvent as e -> UnitCreated(e.Frame, e.UnitId, e.BuilderId)
        | :? UnitDamagedEvent as e -> UnitDamaged(e.Frame, e.UnitId, e.AttackerId, e.Damage)
        | :? UnitDestroyedEvent as e -> UnitDestroyed(e.Frame, e.UnitId, e.AttackerId)
        | :? UpdateEvent as e -> GameUpdate(e.Frame)
        | :? InitEvent as e -> GameInit(e.SkirmishAIId, e.SavedGame)
        | :? ReleaseEvent as e -> GameRelease(e.Reason)
        | _ -> failwith $"Unknown event type: {event.GetType().Name}"

// Option 4: Best approach - Design C# events with F# in mind
// Modify the C# AIEvent hierarchy to include an EventType enum:

type EventType =
    | UnitCreated = 1
    | UnitDamaged = 2
    | UnitDestroyed = 3
    | GameUpdate = 4
    | GameInit = 5
    | GameRelease = 6

// Then conversion becomes O(1) without reflection:
let convertEventEfficiently (event: AIEvent) : BAREvent =
    match event.EventType with
    | EventType.UnitCreated ->
        let e = event :?> UnitCreatedEvent
        UnitCreated(e.Frame, e.UnitId, e.BuilderId)
    | EventType.UnitDamaged ->
        let e = event :?> UnitDamagedEvent
        UnitDamaged(e.Frame, e.UnitId, e.AttackerId, e.Damage)
    | EventType.UnitDestroyed ->
        let e = event :?> UnitDestroyedEvent
        UnitDestroyed(e.Frame, e.UnitId, e.AttackerId)
    | EventType.GameUpdate ->
        let e = event :?> UpdateEvent
        GameUpdate(e.Frame)
    | EventType.GameInit ->
        let e = event :?> InitEvent
        GameInit(e.SkirmishAIId, e.SavedGame)
    | EventType.GameRelease ->
        let e = event :?> ReleaseEvent
        GameRelease(e.Reason)
    | _ -> failwith $"Unknown event type: {event.EventType}"
```

#### Option Type Integration

```fsharp
// F#-friendly wrapper functions that return options
module GameCallback =
    let tryGetUnit (callback: GameCallback) unitId =
        match callback.GetUnit(unitId) with
        | null -> None
        | unit -> Some unit
    
    let tryGetUnitPosition (callback: GameCallback) unitId =
        tryGetUnit callback unitId
        |> Option.map (fun unit -> unit.Position)
    
    // Async operations for non-blocking AI
    let getUnitsAsync (callback: GameCallback) = async {
        return callback.GetFriendlyUnits() |> List.ofSeq
    }
```

### 3. F# AI Implementation Patterns

#### Functional AI Base Class

```fsharp
// F#-friendly AI base class
[<AbstractClass>]
type FSharpAI() =
    inherit BaseAI()
    
    // F# event handlers using pattern matching
    abstract member HandleBAREvent: BAREvent -> unit
    
    // Override C# events and convert to F# events
    override this.OnUnitCreated(unitId, builderId) =
        UnitCreated(this.CurrentFrame, unitId, builderId)
        |> this.HandleBAREvent
    
    override this.OnUnitDamaged(unitId, attackerId, damage, direction, weaponDefId, paralyzer) =
        UnitDamaged(this.CurrentFrame, unitId, attackerId, damage)
        |> this.HandleBAREvent
    
    override this.OnUpdate(frame) =
        base.OnUpdate(frame)
        GameUpdate(frame) |> this.HandleBAREvent

// Example F# AI implementation
type ExampleFSharpAI() =
    inherit FSharpAI()
    
    // Immutable state using record types
    let mutable gameState = {
        Frame = 0
        Metal = 0.0f
        Energy = 0.0f
        Units = []
    }
    
    // Pattern matching for event handling
    override this.HandleBAREvent event =
        match event with
        | GameUpdate frame ->
            gameState <- { gameState with Frame = frame }
            this.ProcessGameUpdate()
            
        | UnitCreated(frame, unitId, builderId) ->
            let newUnit = this.Callback.GetUnit(unitId)
            gameState <- { gameState with Units = newUnit :: gameState.Units }
            this.ProcessNewUnit(unitId, builderId)
            
        | UnitDamaged(frame, unitId, attackerId, damage) ->
            this.HandleCombat(unitId, attackerId, damage)
            
        | _ -> () // Handle other events
    
    member private this.ProcessGameUpdate() =
        // F# pipe operator for functional composition
        this.Callback.GetResources()
        |> this.UpdateResourceState
        |> this.PlanEconomicActions
        |> this.ExecuteActions
    
    member private this.UpdateResourceState resources =
        gameState <- { 
            gameState with 
                Metal = resources.Metal
                Energy = resources.Energy 
        }
        resources
```

#### F#-First AI Interface

```fsharp
// Primary AI interface optimized for F# patterns
type IAI =
    abstract member HandleEvent: GameEvent -> unit
    abstract member GetNextCommands: ResourceState -> Command list
    abstract member Initialize: IGameContext -> unit
    abstract member Shutdown: int -> unit

// Example F# AI implementation
type FSharpAI(context: IGameContext) =
    let mutable gameState = {
        Metal = 0.0f<metal>
        Energy = 0.0f<energy>
        MetalIncome = 0.0f<metal>
        EnergyIncome = 0.0f<energy>
        CurrentFrame = 0<frame>
    }
    
    interface IAI with
        member this.HandleEvent event =
            match event with
            | GameStarted(aiId, savedGame) ->
                printfn "F# AI %d started (saved: %b)" aiId savedGame
                
            | FrameUpdate frame ->
                gameState <- context.GetResources()
                this.ProcessFrame(frame)
                
            | UnitCreated(unitId, builderId, frame) ->
                this.HandleUnitCreated(unitId, builderId)
                
            | UnitDamaged(unitId, attackerId, damage, frame) ->
                this.HandleCombat(unitId, attackerId, damage)
                
            | UnitDestroyed(unitId, attackerId, frame) ->
                this.HandleUnitLoss(unitId, attackerId)
                
            | GameEnded reason ->
                printfn "Game ended with reason: %d" reason
        
        member this.GetNextCommands resources =
            this.PlanActions(resources)
        
        member this.Initialize context =
            printfn "F# AI initialized"
        
        member this.Shutdown reason =
            printfn "F# AI shutting down: %d" reason
    
    member private this.ProcessFrame(frame: int<frame>) =
        // F# frame processing logic
        ()
    
    member private this.PlanActions(resources: ResourceState) : Command list =
        match resources with
        | { Metal = m; Energy = e } when m > 1000.0f<metal> && e > 2000.0f<energy> ->
            [ Build(1, "armvp", Vector3.Zero) ]
        | { Metal = m } when m < 500.0f<metal> ->
            [ Build(1, "armmex", Vector3.Zero) ]
        | _ -> 
            []
```

### C# Compatibility Layer

The C# API becomes a compatibility layer that wraps the F# core:

```csharp
// C# wrapper around F# types
using SpringAI.Core;
using Microsoft.FSharp.Core;

namespace SpringAI.CSharp
{
    // C# event wrapper
    public abstract class AIEvent
    {
        public int Frame { get; set; }
        public abstract GameEvent ToFSharpEvent();
    }
    
    public class UnitCreatedEvent : AIEvent
    {
        public int UnitId { get; set; }
        public int BuilderId { get; set; }
        
        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewUnitCreated(UnitId, BuilderId, Frame);
    }
    
    // C# AI interface that delegates to F#
    public interface ICSharpAI
    {
        void OnInit(int aiId, bool savedGame);
        void OnUpdate(int frame);
        void OnUnitCreated(int unitId, int builderId);
        // ... other methods
    }
    
    // Bridge between C# and F# APIs
    public class CSharpAIAdapter : ICSharpAI
    {
        private readonly IAI _fsharpAI;
        private readonly IGameContext _context;
        
        public CSharpAIAdapter(IAI fsharpAI, IGameContext context)
        {
            _fsharpAI = fsharpAI;
            _context = context;
        }
        
        public void OnInit(int aiId, bool savedGame)
        {
            var fsharpEvent = GameEvent.NewGameStarted(aiId, savedGame);
            _fsharpAI.HandleEvent(fsharpEvent);
        }
        
        public void OnUpdate(int frame)
        {
            var fsharpEvent = GameEvent.NewFrameUpdate(frame);
            _fsharpAI.HandleEvent(fsharpEvent);
            
            // Execute planned commands
            var resources = _context.GetResources();
            var commands = _fsharpAI.GetNextCommands(resources);
            foreach (var command in commands)
            {
                _context.ExecuteCommand(command);
            }
        }
        
        public void OnUnitCreated(int unitId, int builderId)
        {
            var fsharpEvent = GameEvent.NewUnitCreated(unitId, builderId, 0);
            _fsharpAI.HandleEvent(fsharpEvent);
        }
    }
}
```

### Project Structure for F#-First Design

```
SpringAI.FSharp/              # Core F# library
├── SpringAI.FSharp.fsproj
├── Core/
│   ├── Types.fs              # Core F# types (events, commands, etc.)
│   ├── GameContext.fs        # F# game interface
│   ├── AI.fs                 # F# AI interface
│   └── Commands.fs           # F# command types
├── Interop/
│   ├── NativeInterop.fs      # P/Invoke to native layer
│   └── Marshalling.fs        # Type conversions
└── Extensions/
    ├── ActivePatterns.fs     # F# active patterns
    ├── ComputationExpressions.fs
    └── DomainSpecificLanguage.fs

SpringAI.CSharp/              # C# compatibility layer
├── SpringAI.CSharp.csproj
├── Events/
│   └── CSharpEvents.cs       # C# event wrappers
├── AI/
│   ├── CSharpAI.cs          # C# AI interface
│   └── AIAdapter.cs         # Bridge to F# core
└── Compatibility/
    └── TypeConverters.cs     # Convert between C# and F# types

SpringAI.Native/              # Native C++ interop
├── SpringAI.Native.vcxproj
└── src/
    ├── FSharpInterop.cpp     # Optimized for F# types
    └── Exports.cpp           # C exports
```

### Benefits of F#-First Design

1. **Type Safety**: F# discriminated unions are compile-time checked, eliminating runtime errors
2. **Performance**: No reflection needed - pattern matching compiles to efficient switch statements
3. **Immutability**: Default immutable types prevent threading issues
4. **Expressiveness**: Domain modeling with DUs is more precise than inheritance
5. **Error Handling**: Built-in Result/Option types eliminate null reference exceptions
6. **Concurrency**: F# async and agents provide better concurrency primitives

### C# Consumption of F#-First API

C# developers can still use the wrapper, but through F#-optimized types:

```csharp
// C# using F# types
using SpringAI.Core;
using Microsoft.FSharp.Collections;

public class CSharpBARAI : ICSharpAI
{
    private readonly IGameContext _context;
    
    public CSharpBARAI(IGameContext context)
    {
        _context = context;
    }
    
    public void OnUpdate(int frame)
    {
        var resources = _context.GetResources();
        
        // C# can still work with F# types
        if (resources.Metal.Value > 1000 && resources.Energy.Value > 2000)
        {
            var buildCommand = Command.NewBuild(1, "armvp", Vector3.Zero);
            var result = _context.ExecuteCommand(buildCommand);
            
            // Handle F# Result type in C#
            if (result.IsOk)
            {
                Console.WriteLine($"Success: {result.ResultValue}");
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorValue}");
            }
        }
    }
}
```

## Implementation Guidelines

### 1. Nullable Reference Types

Since C# 8.0+ supports nullable reference types, ensure F# compatibility:

```csharp
// In C# wrapper
#nullable enable

public interface IGameCallback
{
    Unit? GetUnit(int unitId);  // Nullable return
    string GetUnitName(int unitDefId);  // Non-nullable return
}
```

```fsharp
// F# consumption
let tryGetUnitName (callback: IGameCallback) unitDefId =
    let name = callback.GetUnitName(unitDefId)
    if String.IsNullOrEmpty(name) then None else Some name
```

### 2. Async/Await Integration

```csharp
// C# async methods for F# async workflows
public interface IGameCallback
{
    Task<IReadOnlyList<Unit>> GetUnitsAsync();
    Task<bool> ExecuteCommandAsync(Command command);
}
```

```fsharp
// F# async consumption
let processUnitsAsync (callback: GameCallback) = async {
    let! units = callback.GetUnitsAsync() |> Async.AwaitTask
    return units |> Seq.filter isValidTarget |> List.ofSeq
}
```

### 3. Immutability Considerations

```csharp
// Design wrapper classes as immutable when possible
public readonly struct Vector3
{
    public readonly float X, Y, Z;
    public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
}

// Or use init-only properties for complex types
public class Unit
{
    public int Id { get; init; }
    public Vector3 Position { get; init; }
    public float Health { get; init; }
}
```

### 4. Collection Types

```csharp
// Use IReadOnlyList and IReadOnlyCollection for F# list compatibility
public interface IGameCallback
{
    IReadOnlyList<Unit> GetFriendlyUnits();
    IReadOnlyDictionary<string, string> GetModOptions();
}
```

```fsharp
// Natural F# consumption
let friendlyUnits = callback.GetFriendlyUnits() |> List.ofSeq
let modOptions = callback.GetModOptions() |> Map.ofSeq
```

## Performance Considerations for F#

### 1. Avoiding Excessive Allocations

```fsharp
// Use struct tuples and value types when possible
type analyzeUnit (unitInfo: struct(int * float32 * Vector3)) =
    let (unitId, health, position) = unitInfo
    // Process without heap allocation
    ()

// Pool expensive operations
let mutable unitCache = Map.empty<int, UnitInfo>

let getCachedUnit unitId callback =
    match Map.tryFind unitId unitCache with
    | Some cached -> cached
    | None ->
        let unit = callback.GetUnit(unitId)
        let info = createUnitInfo unit
        unitCache <- Map.add unitId info unitCache
        info
```

### 2. Lazy Evaluation

```fsharp
// Use lazy evaluation for expensive computations
let expensiveAnalysis = lazy (
    callback.GetFriendlyUnits()
    |> Seq.map analyzeUnit
    |> Seq.filter isStrategicUnit
    |> List.ofSeq
)

// Only computed when needed
let strategicUnits = expensiveAnalysis.Value
```

## Testing Considerations

### 1. F# Testing with the Wrapper

```fsharp
// Using Expecto for F# testing
open Expecto
open SpringAI.FSharp

[<Tests>]
let tests =
    testList "F# AI Wrapper Tests" [
        test "Unit classification works correctly" {
            let mockCallback = createMockCallback()
            let unitInfo = GameCallback.tryGetUnit mockCallback 1
            
            match unitInfo with
            | Some unit when unit.Categories.Contains("COMMANDER") ->
                Expect.equal unit.Faction (Some ARM) "Should identify ARM commander"
            | _ ->
                failtest "Expected commander unit"
        }
        
        testAsync "Async operations complete successfully" {
            let mockCallback = createMockCallback()
            let! units = GameCallback.getUnitsAsync mockCallback
            Expect.isGreaterThan (List.length units) 0 "Should return units"
        }
    ]
```

## Recommended Project Structure

```
BAR.AI.FSharp/
├── BAR.AI.FSharp.fsproj
├── Core/
│   ├── Types.fs              # F# type definitions
│   ├── GameCallback.fs       # F# wrapper functions
│   └── Events.fs             # F# event handling
├── AI/
│   ├── FSharpAI.fs          # F# AI base class
│   ├── Strategy.fs          # Strategy patterns
│   └── Planning.fs          # AI planning logic
├── Examples/
│   ├── SimpleAI.fs          # Basic F# AI example
│   └── AdvancedAI.fs        # Complex F# AI example
└── Tests/
    ├── Tests.fs             # F# unit tests
    └── TestHelpers.fs       # Testing utilities
```

## Additional F# Idioms and Patterns

### 1. Active Patterns for Enhanced Pattern Matching

```fsharp
// Active patterns for unit classification
let (|Commander|Builder|Combat|Economic|Other|) (unit: Unit) =
    let categories = unit.Categories
    if categories.Contains("COMMANDER") then Commander
    elif categories.Contains("BUILDER") then Builder
    elif categories.Contains("WEAPON") then Combat  
    elif categories.Contains("ENERGY") || categories.Contains("METAL") then Economic
    else Other

// Active patterns for game state
let (|EarlyGame|MidGame|LateGame|) frame =
    match frame with
    | f when f < 1800 -> EarlyGame    // First 60 seconds
    | f when f < 9000 -> MidGame      // 1-5 minutes
    | _ -> LateGame                   // 5+ minutes

// Usage in AI logic
let selectStrategy unit frame =
    match unit, frame with
    | Commander, EarlyGame -> ExpandBase
    | Builder, EarlyGame -> BuildEconomy
    | Combat, LateGame -> AttackEnemy
    | Economic, MidGame -> OptimizeProduction
    | _ -> Wait
```

### 2. F# Computation Expressions for BAR Operations

```fsharp
// Result-based computation expression for safe command execution
type BARResultBuilder() =
    member _.Bind(result, f) =
        match result with
        | Ok value -> f value
        | Error e -> Error e
    
    member _.Return(value) = Ok value
    member _.ReturnFrom(result) = result

let barResult = BARResultBuilder()

// Safe command execution with error handling
let executeSequence (callback: IGameCallback) = barResult {
    let! builder = tryGetBuilder callback
    let! buildCommand = createBuildCommand builder "armsolar"
    let! result = executeBuildCommand callback buildCommand
    return result
}
```

### 3. Units of Measure for Type Safety

```fsharp
// Define units of measure for BAR-specific values
[<Measure>] type frame
[<Measure>] type metal
[<Measure>] type energy
[<Measure>] type elmo    // BAR distance units
[<Measure>] type dps     // Damage per second

// Type-safe wrapper functions
module SafeGameCallback =
    let getFrame (callback: IGameCallback) : int<frame> = 
        callback.GetCurrentFrame() * 1<frame>
    
    let getMetal (callback: IGameCallback) : float32<metal> =
        callback.GetMetal() * 1.0f<metal>
    
    let getEnergy (callback: IGameCallback) : float32<energy> =
        callback.GetEnergy() * 1.0f<energy>
    
    let getDistance (pos1: Vector3) (pos2: Vector3) : float32<elmo> =
        Vector3.Distance(pos1, pos2) * 1.0f<elmo>

// Usage with compile-time safety
let canAffordUnit (metalCost: float32<metal>) (energyCost: float32<energy>) callback =
    let currentMetal = SafeGameCallback.getMetal callback
    let currentEnergy = SafeGameCallback.getEnergy callback
    currentMetal >= metalCost && currentEnergy >= energyCost
```

### 4. Railway-Oriented Programming for AI Decisions

```fsharp
// Railway-oriented programming for complex AI decision chains
type AIResult<'T> = Result<'T, string>

module AIResult =
    let bind f result =
        match result with
        | Ok value -> f value
        | Error e -> Error e
    
    let map f result =
        match result with
        | Ok value -> Ok (f value)
        | Error e -> Error e

// Chain AI operations safely
let planFactoryConstruction callback = 
    tryFindBuilder callback
    |> AIResult.bind (findBuildSite callback)
    |> AIResult.bind (checkResources callback)
    |> AIResult.bind (issueBuildCommand callback)
    |> AIResult.map (fun cmd -> sprintf "Building factory at %A" cmd.TargetPosition)
```

### 5. Type Providers for Static BAR Data

```fsharp
// Future enhancement: F# Type Provider for BAR unit definitions
// This would generate compile-time types from BAR's unit definition files

// Conceptual usage after implementation:
module BARUnits =
    type ARM = BARTypeProvider<"BAR.sdd", "ARM">
    type COR = BARTypeProvider<"BAR.sdd", "COR">

// Compile-time safe unit references
let buildArmSolar callback =
    let unitDef = ARM.Buildings.armsolar
    callback.CreateUnit(unitDef.Id, Vector3.Zero)

// IntelliSense and compile-time validation
let armFactories = [
    ARM.Factories.armlab
    ARM.Factories.armvp
    ARM.Factories.armap
    ARM.Factories.armhp
]
```

### 6. Functional Reactive Programming with F# Events

```fsharp
// FRP-style event handling for reactive AI
module ReactiveAI =
    open System.Reactive.Linq
    
    // Convert C# events to F# observables
    let toObservable (event: IEvent<'T>) =
        event |> Observable.fromEvent
    
    // Reactive AI that responds to events
    type ReactiveBARAI() =
        inherit FSharpAI()
        
        let unitCreated = Event<UnitCreatedEvent>()
        let unitDestroyed = Event<UnitDestroyedEvent>()
        
        // Reactive streams
        let criticalUnitsDestroyed =
            unitDestroyed.Publish
            |> Observable.filter (fun e -> 
                let unit = this.Callback.GetUnit(e.UnitId)
                unit.Categories.Contains("COMMANDER"))
        
        let massiveArmyBuilt =
            unitCreated.Publish
            |> Observable.windowWithCount 10
            |> Observable.filter (fun window -> 
                window |> Seq.forall (fun e -> e.Frame < this.CurrentFrame + 300))
        
        // React to patterns
        do
            criticalUnitsDestroyed.Subscribe(this.HandleCriticalLoss) |> ignore
            massiveArmyBuilt.Subscribe(this.HandleMassProduction) |> ignore
```

### 7. Domain-Specific Language (DSL) for Build Orders

```fsharp
// F# DSL for expressing build orders
type BuildStep =
    | BuildUnit of string * int  // unitDefName * count
    | WaitFor of BuildCondition
    | Parallel of BuildStep list

and BuildCondition =
    | MetalReaches of float32
    | EnergyReaches of float32
    | FrameReaches of int
    | UnitsComplete of string * int

// DSL functions
let buildUnit name count = BuildUnit(name, count)
let waitForMetal amount = WaitFor(MetalReaches amount)
let waitForEnergy amount = WaitFor(EnergyReaches amount)
let inParallel steps = Parallel steps

// Express complex build orders declaratively
let armStartBuildOrder = [
    buildUnit "armsolar" 2
    waitForMetal 100.0f
    buildUnit "armlab" 1
    inParallel [
        buildUnit "armsolar" 3
        buildUnit "armkbot" 5
    ]
    waitForEnergy 500.0f
    buildUnit "armvp" 1
]

// Execute build order
let rec executeBuildOrder callback buildOrder =
    // Implementation that interprets and executes the build order
    ()
```

### 8. F# Lens Library Integration for Deep Updates

```fsharp
// Using F# lens libraries for deep immutable updates
open Aether  // Popular F# lens library

// Define lenses for nested game state
let metalLens = 
    (fun (state: GameState) -> state.Resources.Metal),
    (fun metal state -> { state with Resources = { state.Resources with Metal = metal }})

let unitsLens =
    (fun (state: GameState) -> state.Units),
    (fun units state -> { state with Units = units })

// Clean immutable updates
let updateMetal newMetal state =
    Optic.set metalLens newMetal state

let addUnit newUnit state =
    Optic.map unitsLens (fun units -> newUnit :: units) state
```

## Data-Oriented AI Design Patterns

For high-performance game AI, a data-oriented approach is highly recommended. F# excels at this due to its immutable data structures, array processing capabilities, and functional composition.

### Core Data-Oriented Architecture

#### World State Arrays

Instead of object-oriented entity management, maintain typed arrays for different aspects of the world state:

```fsharp
// Core world state with frame-based snapshots
type WorldSnapshot = {
    Frame: int<frame>
    Timestamp: DateTime
    
    // Unit arrays indexed by UnitId
    Units: Map<int, UnitData>
    UnitPositions: Vector3[]
    UnitHealths: float32[]
    UnitTypes: UnitDefId[]
    UnitStates: UnitState[]
    
    // Resource state
    Resources: ResourceState
    
    // Team/ally information
    MyTeam: int
    MyAllyTeam: int
    Teams: TeamData[]
    
    // Map information (cached/static)
    MapWidth: int<elmo>
    MapHeight: int<elmo>
    MetalSpots: Vector3[]
}

// Efficient unit data with value types
[<Struct>]
type UnitData = {
    Id: int
    DefId: UnitDefId
    Team: int
    Position: Vector3
    Health: float32
    MaxHealth: float32
    IsBeingBuilt: bool
    BuildProgress: float32
}

// Unit states as discriminated union
type UnitState =
    | Idle
    | Moving of target: Vector3
    | Building of targetDefId: UnitDefId * position: Vector3
    | Attacking of targetId: int
    | Dead
```

#### Event Collection Arrays

Collect events by type in arrays for batch processing:

```fsharp
// Frame-based event collections
type FrameEvents = {
    Frame: int<frame>
    
    // Event arrays for efficient batch processing
    UnitsCreated: UnitCreatedData[]
    UnitsDestroyed: UnitDestroyedData[]
    UnitsDamaged: UnitDamagedData[]
    UnitsIdle: int[]  // Just unit IDs
    EnemiesSpotted: int[]
    EnemiesLost: int[]
    
    // Resource events
    ResourceUpdates: ResourceState option
}

[<Struct>]
type UnitCreatedData = {
    UnitId: int
    BuilderId: int
    DefId: UnitDefId
    Position: Vector3
    Team: int
}

[<Struct>]
type UnitDestroyedData = {
    UnitId: int
    AttackerId: int option
    Position: Vector3  // Last known position
    DefId: UnitDefId
}
```

### Data-Oriented AI Implementation

#### Event Collection Pattern

```fsharp
type DataOrientedAI() =
    // Mutable state for performance - updated each frame
    let mutable currentWorld = WorldSnapshot.empty
    let mutable frameEvents = FrameEvents.empty
    let mutable eventBuffer = ResizeArray<GameEvent>()
    
    // Arrays for different AI subsystems
    let mutable economyTargets: EconomyTarget[] = [||]
    let mutable militaryTargets: MilitaryTarget[] = [||]
    let mutable buildQueue: BuildOrder[] = [||]
    
    interface IAI with
        member this.OnEvent(event: AIEvent) =
            // Convert to F# event and buffer
            let fsharpEvent = event |> convertToFSharpEvent
            eventBuffer.Add(fsharpEvent)
        
        member this.OnUpdate(frame: int) =
            // Process all buffered events for this frame
            let events = this.ProcessEventBuffer(frame)
            
            // Update world state
            currentWorld <- this.UpdateWorldState(currentWorld, events)
            
            // Run AI systems with updated data
            this.RunEconomySystem(currentWorld, events)
            this.RunMilitarySystem(currentWorld, events)
            this.RunBuildSystem(currentWorld, events)
            
            // Clear event buffer for next frame
            eventBuffer.Clear()
    
    member private this.ProcessEventBuffer(frame: int) : FrameEvents =
        // Convert event buffer to structured frame events
        let unitsCreated = ResizeArray<UnitCreatedData>()
        let unitsDestroyed = ResizeArray<UnitDestroyedData>()
        // ... other event arrays
        
        for event in eventBuffer do
            match event with
            | UnitCreated (f, unitId, builderId) when f = frame =>
                // Query additional data from game callback
                let pos = gameCallback.GetUnitPosition(unitId)
                let defId = gameCallback.GetUnitDefId(unitId)
                let team = gameCallback.GetUnitTeam(unitId)
                unitsCreated.Add({
                    UnitId = unitId
                    BuilderId = builderId
                    DefId = defId
                    Position = pos
                    Team = team
                })
            | UnitDestroyed (f, unitId, attackerId) when f = frame =>
                // Build destroyed unit data...
                ()
            | _ -> ()
        
        {
            Frame = frame
            UnitsCreated = unitsCreated.ToArray()
            UnitsDestroyed = unitsDestroyed.ToArray()
            // ... other arrays
        }
```

#### Batch Processing Systems

```fsharp
    member private this.RunEconomySystem(world: WorldSnapshot, events: FrameEvents) =
        // Process economy in batches
        let myUnits = world.Units |> Map.filter (fun _ unit -> unit.Team = world.MyTeam)
        
        // Find idle builders
        let idleBuilders = 
            events.UnitsIdle
            |> Array.filter (fun unitId -> 
                match Map.tryFind unitId myUnits with
                | Some unit -> this.IsBuilder(unit.DefId)
                | None -> false)
        
        // Batch assign construction tasks
        let constructionTasks = this.PlanConstruction(world, idleBuilders)
        constructionTasks |> Array.iter this.IssueConstructionOrder
        
        // Update economy targets array
        economyTargets <- this.UpdateEconomyTargets(world, events)
    
    member private this.RunMilitarySystem(world: WorldSnapshot, events: FrameEvents) =
        // Process military units in batches
        let myMilitaryUnits = 
            world.Units 
            |> Map.filter (fun _ unit -> 
                unit.Team = world.MyTeam && this.IsMilitaryUnit(unit.DefId))
            |> Map.toArray
            |> Array.map snd
        
        // Batch target assignment
        let enemyUnits = events.EnemiesSpotted |> Array.map (fun id -> world.Units.[id])
        let assignments = this.AssignTargets(myMilitaryUnits, enemyUnits)
        assignments |> Array.iter this.IssueAttackOrder
```

### Performance Optimizations

#### Array Processing with SIMD

```fsharp
// Leverage F#'s array processing for performance
module ArrayOps =
    let inline updateUnitHealths (healths: float32[]) (damages: UnitDamagedData[]) =
        damages
        |> Array.iter (fun dmg -> 
            if dmg.UnitId < healths.Length then
                healths.[dmg.UnitId] <- max 0.0f (healths.[dmg.UnitId] - dmg.Damage))
    
    let inline findUnitsInRange (positions: Vector3[]) (center: Vector3) (range: float32) =
        positions
        |> Array.mapi (fun i pos -> i, Vector3.Distance(pos, center))
        |> Array.filter (fun (_, dist) -> dist <= range)
        |> Array.map fst
    
    let inline filterUnitsByType (units: UnitData[]) (targetType: UnitDefId) =
        units |> Array.filter (fun unit -> unit.DefId = targetType)
```

#### Memory-Efficient Data Structures

```fsharp
// Use structs for frequently allocated data
[<Struct>]
type TargetAssignment = {
    AttackerId: int
    TargetId: int
    Priority: float32
    Distance: float32
}

// Use ArrayPool for temporary arrays
open System.Buffers

type AISystem() =
    let arrayPool = ArrayPool<int>.Shared
    
    member this.ProcessLargeDataSet(data: int[]) =
        let tempArray = arrayPool.Rent(data.Length * 2)
        try
            // Process data efficiently
            ()
        finally
            arrayPool.Return(tempArray)
```

### Benefits of Data-Oriented Design

1. **Performance**: Arrays and batch processing are cache-friendly and enable SIMD optimizations
2. **Clarity**: Separating data from behavior makes the AI logic more understandable
3. **Testing**: Pure functions operating on data are easier to test
4. **Concurrency**: Immutable data structures prevent race conditions
5. **Debugging**: Easy to snapshot and inspect the complete world state

This approach leverages F#'s strengths while providing excellent performance for real-time game AI scenarios.

## Performance and Design Best Practices Summary

### ✅ DO: Efficient Event Conversion
```fsharp
// Use EventType enum for O(1) pattern matching
let convertEvent (event: AIEvent) =
    match event.EventType with
    | EventType.UnitCreated -> // Fast enum comparison
        let e = event :?> UnitCreatedEvent
        UnitCreated(e.Frame, e.UnitId, e.BuilderId)
    | EventType.UnitDamaged ->
        let e = event :?> UnitDamagedEvent
        UnitDamaged(e.Frame, e.UnitId, e.AttackerId, e.Damage)
    | EventType.UnitDestroyed ->
        let e = event :?> UnitDestroyedEvent
        UnitDestroyed(e.Frame, e.UnitId, e.AttackerId)
    | EventType.GameUpdate ->
        let e = event :?> UpdateEvent
        GameUpdate(e.Frame)
    | EventType.GameInit ->
        let e = event :?> InitEvent
        GameInit(e.SkirmishAIId, e.SavedGame)
    | EventType.GameRelease ->
        let e = event :?> ReleaseEvent
        GameRelease(e.Reason)
    | _ -> failwith $"Unknown event type: {event.EventType}"
```

### ✅ DO: Type Extensions
```fsharp
// Extend C# types with F# methods
type UnitCreatedEvent with
    member this.ToBAREvent() = UnitCreated(this.Frame, this.UnitId, this.BuilderId)
```

### ❌ DON'T: Runtime Type Checking without EventType
```fsharp
// Avoid: Slower runtime type checking
match event with
| :? UnitCreatedEvent as e -> // Runtime type check on every call
```

### ✅ DO: Design C# APIs with F# in Mind
- Add `EventType` enum to base classes
- Use `IReadOnlyList<T>` instead of arrays
- Provide nullable reference type annotations
- Use `init`-only properties for immutability

### ✅ DO: Leverage F#'s Strengths
- Units of measure for type safety
- Active patterns for domain modeling  
- Computation expressions for complex workflows
- Railway-oriented programming for error handling
- Agent-based concurrency for non-blocking operations
