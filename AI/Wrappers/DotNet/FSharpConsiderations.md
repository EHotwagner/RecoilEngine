# F# Integration Considerations for the .NET AI Wrapper

## Overview

The .NET AI wrapper is designed to be language-agnostic within the .NET ecosystem. F# developers can leverage the wrapper to create sophisticated AI implementations using F#'s functional programming paradigms, pattern matching, and type safety features. This document outlines considerations and best practices for consuming the .NET wrapper from F#.

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

### 4. Advanced F# Patterns

#### Computation Expressions for AI Planning

```fsharp
// Custom computation expression for AI planning
type AIBuilder() =
    member this.Bind(resource, f) = 
        match resource with
        | Some value -> f value
        | None -> []
    
    member this.Return(value) = [value]
    
    member this.Zero() = []

let ai = AIBuilder()

// Usage in AI decision making
let planBuildOrder (callback: GameCallback) = ai {
    let! metal = GameCallback.tryGetMetal callback
    let! energy = GameCallback.tryGetEnergy callback
    
    if metal > 1000.0f && energy > 2000.0f then
        return BuildFactory("armvp")
    else
        return BuildEconomy("armsolar")
}
```

#### Agent-Based Architecture

```fsharp
// F# Agent (MailboxProcessor) for concurrent AI processing
type AIAgent(callback: GameCallback) =
    
    type AgentMessage =
        | ProcessEvent of BAREvent
        | GetState of AsyncReplyChannel<GameState>
        | UpdateStrategy of Strategy
    
    let agent = MailboxProcessor<AgentMessage>.Start(fun inbox ->
        let rec loop state = async {
            let! msg = inbox.Receive()
            match msg with
            | ProcessEvent event ->
                let newState = processEvent state event
                return! loop newState
                
            | GetState reply ->
                reply.Reply(state)
                return! loop state
                
            | UpdateStrategy strategy ->
                let newState = { state with Strategy = strategy }
                return! loop newState
        }
        loop initialState
    )
    
    member this.PostEvent event = agent.Post(ProcessEvent event)
    member this.GetStateAsync() = agent.PostAndAsyncReply(GetState)
```

### 5. Type Provider Considerations

#### Future: F# Type Provider for Game Data

```fsharp
// Conceptual F# type provider for BAR game data
// This would generate types at compile time from BAR's unit definitions

#r "SpringAI.TypeProvider.dll"

open SpringAI.TypeProviders

type BAR = BARTypeProvider<"path/to/BAR.sdd">

// Strongly typed access to BAR units
let armCommander = BAR.Units.ARM.armcom
let corCommander = BAR.Units.COR.corcom

// Type-safe build orders
let armBuildOrder = [
    BAR.Units.ARM.armsolar
    BAR.Units.ARM.armkbot
    BAR.Units.ARM.armvp
]
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
let processUnitsAsync (callback: IGameCallback) = async {
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
let analyzeUnit (unitInfo: struct(int * float32 * Vector3)) =
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

### 8. F# Lens Library Integration for Immutable Updates

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

## Performance Optimizations for F#

### 1. Memoization for Expensive Calculations

```fsharp
// Memoization for expensive AI calculations
let memoize f =
    let cache = System.Collections.Concurrent.ConcurrentDictionary<_, _>()
    fun x -> cache.GetOrAdd(x, f)

// Memoized path finding
let findPathMemoized = memoize (fun (start, target) ->
    // Expensive pathfinding calculation
    calculatePath start target)

// Usage
let path = findPathMemoized (unitPos, targetPos)
```

### 2. Struct Records for High-Performance Scenarios

```fsharp
// Use struct records for frequently allocated types
[<Struct>]
type UnitStats = {
    Health: float32
    MaxHealth: float32
    Damage: float32
    Range: float32
}

// Avoid heap allocation for performance-critical paths
let calculateThreat (stats: UnitStats) distance =
    let effectiveness = stats.Damage / max 1.0f distance
    effectiveness * (stats.Health / stats.MaxHealth)
```

## Advanced Testing Patterns

### 1. Property-Based Testing with FsCheck

```fsharp
open FsCheck
open FsCheck.Xunit

// Property-based testing for AI behavior
[<Property>]
let ``AI should never exceed resource limits`` (metalAmount: NonNegativeInt) (energyAmount: NonNegativeInt) =
    let ai = TestableAI()
    let mockCallback = createMockCallback metalAmount.Get energyAmount.Get
    
    ai.SetCallback(mockCallback)
    ai.ProcessFrame()
    
    // Property: AI should never issue commands that exceed available resources
    let commandCost = ai.GetPendingCommands() |> List.sumBy (fun cmd -> cmd.MetalCost + cmd.EnergyCost)
    commandCost <= float32 (metalAmount.Get + energyAmount.Get)

[<Property>]
let ``Unit classification is consistent`` (unitDef: UnitDef) =
    let classification1 = classifyUnit unitDef
    let classification2 = classifyUnit unitDef
    classification1 = classification2
```

### 2. Behavior-Driven Testing with TickSpec

```fsharp
// BDD testing for AI scenarios
Feature: BAR AI Economic Management

Scenario: Early game resource management
    Given the AI has 100 metal and 100 energy
    When the AI receives an update event
    Then the AI should prioritize energy production
    And the AI should not build expensive units

Scenario: Resource overflow prevention
    Given the AI has 10000 metal and 10000 energy
    When the AI receives an update event  
    Then the AI should stop building economy
    And the AI should start building military units
```

## Conclusion

The .NET AI wrapper can be made highly F#-friendly through careful consideration of:

1. **Type Design**: Using nullable types, immutable structs, and readonly collections
2. **Functional Patterns**: Supporting pattern matching, option types, and functional composition
3. **F# Wrappers**: Creating F#-specific modules that provide idiomatic interfaces
4. **Performance**: Considering F#'s functional nature when designing APIs
5. **Testing**: Supporting F#'s testing frameworks and async workflows
6. **Advanced F# Features**: Active patterns, computation expressions, units of measure
7. **Domain Modeling**: Type-safe domain-specific languages and railway-oriented programming
8. **Reactive Programming**: FRP patterns for event-driven AI behavior
9. **Type Providers**: Compile-time safety for game data (future enhancement)
10. **Property-Based Testing**: Robust testing with FsCheck and behavior-driven development

By following these guidelines, F# developers can leverage their language's full power for AI development, creating robust, type-safe, and maintainable AI implementations for the BAR game engine while maintaining excellent performance and leveraging F#'s unique strengths in functional programming, domain modeling, and correctness.

## Performance and Design Best Practices Summary

### ✅ DO: Efficient Event Conversion
```fsharp
// Use EventType enum for O(1) pattern matching
let convertEvent (event: AIEvent) =
    match event.EventType with
    | EventType.UnitCreated -> // Fast enum comparison
        let e = event :?> UnitCreatedEvent
        UnitCreated(e.Frame, e.UnitId, e.BuilderId)
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
