/// F#-first AI interface and implementations
namespace SpringAI.Core

open System
open ActivePatterns

/// Primary F# AI interface
type IAI =
    /// Handle game events using F# discriminated unions
    abstract member HandleEvent: GameEvent -> unit
    
    /// Plan next actions based on current state
    abstract member PlanActions: ResourceState -> Decision<Command> list
    
    /// Initialize AI with game context
    abstract member Initialize: IGameContext -> unit
    
    /// Shutdown AI with reason
    abstract member Shutdown: int -> unit
    
    /// Get current AI strategy
    abstract member GetStrategy: unit -> Strategy

/// Base F# AI implementation with common functionality
[<AbstractClass>]
type BaseFSharpAI(context: IGameContext) =
    let mutable currentStrategy = EconomicExpansion
    let mutable gameState = context.GetResources()
    
    /// Update internal state
    member this.UpdateState() =
        gameState <- context.GetResources()
    
    /// Get current game state
    member this.GameState = gameState
    
    /// Get game context
    member this.Context = context
    
    /// Strategy selection using F# pattern matching
    member this.SelectStrategy() =
        match gameState.CurrentFrame, gameState with
        | EarlyGame, ResourcePoor -> EconomicExpansion
        | EarlyGame, ResourceModerate -> EconomicExpansion
        | MidGame, ResourceRich -> MilitaryBuildup
        | LateGame, _ -> TechAdvancement
        | _, _ -> DefensivePosition
    
    /// Abstract methods for derived classes
    abstract member ProcessGameStart: int * bool -> unit
    abstract member ProcessFrameUpdate: int<frame> -> unit
    abstract member ProcessUnitCreated: int * int -> unit
    abstract member ProcessUnitDamaged: int * int * float32 -> unit
    abstract member ProcessUnitDestroyed: int * int -> unit
    abstract member ProcessGameEnd: int -> unit
    abstract member CreateBuildPlan: ResourceState -> Decision<Command> list
    
    /// Default implementations
    default this.ProcessGameStart(aiId, savedGame) = 
        printfn "F# AI %d started (saved: %b)" aiId savedGame
    
    default this.ProcessFrameUpdate(frame) = 
        this.UpdateState()
        currentStrategy <- this.SelectStrategy()
    
    default this.ProcessUnitCreated(unitId, builderId) = 
        printfn "Unit %d created by %d" unitId builderId
    
    default this.ProcessUnitDamaged(unitId, attackerId, damage) = 
        printfn "Unit %d damaged by %d (%.1f damage)" unitId attackerId damage
    
    default this.ProcessUnitDestroyed(unitId, attackerId) = 
        printfn "Unit %d destroyed by %d" unitId attackerId
    
    default this.ProcessGameEnd(reason) = 
        printfn "Game ended with reason: %d" reason
      interface IAI with
        member this.HandleEvent event =
            match event with
            | GameStarted(aiId, savedGame) -> this.ProcessGameStart(aiId, savedGame)
            | FrameUpdate(frame) -> this.ProcessFrameUpdate(frame)
            | UnitCreated(unitId, builderId, frame) -> this.ProcessUnitCreated(unitId, builderId)
            | UnitDamaged(unitId, attackerId, damage, frame) -> this.ProcessUnitDamaged(unitId, attackerId, damage)
            | UnitDestroyed(unitId, attackerId, frame) -> this.ProcessUnitDestroyed(unitId, attackerId)
            | GameEnded(reason) -> this.ProcessGameEnd(reason)
        
        member this.PlanActions resourceState = this.CreateBuildPlan(resourceState)
        member this.Initialize(context) = ()  // Override in derived classes
        member this.Shutdown(reason) = this.ProcessGameEnd(reason)
        member this.GetStrategy() = currentStrategy

/// Computation expression for AI decision making
type AIDecisionBuilder() =
    member _.Bind(result: Result<'a, string>, continuation: 'a -> Result<'b, string>) =
        match result with
        | Ok value -> continuation value
        | Error msg -> Error msg
    
    member _.Return(value: 'a) = Ok value
    member _.ReturnFrom(result: Result<'a, string>) = result
    
    member _.Zero() = Ok ()
    
    member _.Delay(f: unit -> Result<'a, string>) = f
    member _.Run(f: unit -> Result<'a, string>) = f()

/// Global AI decision computation expression
let aiDecision = AIDecisionBuilder()

/// F# modules for common AI operations
module AICommands =
    /// Execute a command safely
    let execute (context: IGameContext) command =
        context.ExecuteCommand command
    
    /// Execute multiple commands
    let executeMany (context: IGameContext) commands =
        commands 
        |> List.map (context.ExecuteCommand)
        |> List.fold (fun acc result -> 
            match acc, result with
            | Ok messages, Ok msg -> Ok (msg :: messages)
            | Error err, _ -> Error err
            | _, Error err -> Error err) (Ok [])
    
    /// Build command with validation
    let buildAt (context: IGameContext) builderId unitDefName position =
        if context.CanBuildAt(position, unitDefName) then
            Ok (Build(builderId, unitDefName, position))
        else
            Error $"Cannot build {unitDefName} at {position}"

module AIStrategy =
    /// Determine game phase based on frame and resources
    let getGamePhase frame resources =
        match frame with
        | f when f < 1800<frame> -> EarlyGame f
        | f when f < 7200<frame> -> MidGame f
        | f -> LateGame f
    
    /// Assess economic state
    let assessEconomicState (resources: ResourceState) =
        let metalRatio = resources.Metal / (resources.MetalIncome + 1.0f<metal>)
        let energyRatio = resources.Energy / (resources.EnergyIncome + 1.0f<energy>)
        
        match metalRatio, energyRatio with
        | m, e when m > 500.0f && e > 1000.0f -> "ResourceRich"
        | m, e when m > 100.0f && e > 200.0f -> "ResourceModerate"
        | _ -> "ResourcePoor"
    
    /// Build order DSL evaluation
    let rec evaluateBuildCondition context condition =
        match condition with
        | MetalReaches target -> 
            let current = context.GetResources().Metal
            current >= target
        | EnergyReaches target ->
            let current = context.GetResources().Energy
            current >= target
        | FrameReaches target ->
            let current = context.GetCurrentFrame()
            current >= target
        | UnitsOfType(unitDefName, count) ->
            let units = context.GetFriendlyUnits()
            let matchingUnits = units |> List.filter (fun u -> u.DefName = unitDefName)
            List.length matchingUnits >= count
        | CustomCondition predicate ->
            predicate (context.GetResources())

/// Example F# AI implementation using the framework
type ExampleFSharpAI(context: IGameContext) =
    inherit BaseFSharpAI(context)
    
    let mutable buildQueue: BuildStep list = []
    
    override this.Initialize(context) =
        // Set up initial build order using F# DSL
        buildQueue <- [
            BuildUnit("armcom", 1, 10)  // Commander first
            WaitFor(MetalReaches 100.0f<metal>)
            BuildUnit("armlab", 1, 9)   // Vehicle lab
            Parallel [
                BuildUnit("armsolr", 3, 5)  // Solar collectors
                BuildUnit("armmex", 2, 8)   // Metal extractors
            ]
            WaitFor(EnergyReaches 200.0f<energy>)
            BuildUnit("armvp", 1, 7)    // Vehicle plant
        ]
    
    override this.CreateBuildPlan resourceState =
        // Process build queue and create decisions
        let availableBuilders = 
            this.Context.GetFriendlyUnits()
            |> List.filter (fun u -> u.Categories |> List.contains "BUILDER")
        
        // Use computation expression for decision making
        let decisions = aiDecision {
            let! validated = 
                if List.isEmpty availableBuilders then
                    Error "No builders available"
                else
                    Ok availableBuilders
            
            return validated
                |> List.mapi (fun i builder ->
                    { Action = Build(builder.Id, "armsolr", builder.Position)
                      Priority = 5
                      Reason = "Economic expansion"
                      RequiredResources = Some { resourceState with Metal = 50.0f<metal> }
                      EstimatedDuration = Some 300<frame> })
        }
        
        match decisions with
        | Ok plans -> plans
        | Error _ -> []

/// Active patterns for unit classification
module ActivePatterns =
    /// Pattern for resource classification
    let (|ResourcePoor|ResourceModerate|ResourceRich|) (resources: ResourceState) =
        let metalScore = resources.Metal / 100.0f<metal>
        let energyScore = resources.Energy / 200.0f<energy>
        let totalScore = metalScore + energyScore
        
        if totalScore < 2.0f then ResourcePoor
        elif totalScore < 6.0f then ResourceModerate
        else ResourceRich
    
    /// Pattern for game phase
    let (|EarlyGame|MidGame|LateGame|) frame =
        match frame with
        | f when f < 1800<frame> -> EarlyGame
        | f when f < 7200<frame> -> MidGame  
        | _ -> LateGame
    
    /// Pattern for unit role
    let (|Builder|Factory|Combat|Economic|Commander|) (unit: UnitInfo) =
        let categories = unit.Categories
        if categories |> List.contains "COMMANDER" then Commander
        elif categories |> List.contains "BUILDER" then Builder
        elif categories |> List.contains "FACTORY" then Factory
        elif categories |> List.contains "WEAPON" then Combat
        else Economic
    
    /// Pattern for threat level
    let (|NoThreat|LowThreat|ModeraThreat|HighThreat|) (assessment: ThreatAssessment) =
        match assessment.Level with
        | ThreatLevel.None -> NoThreat
        | ThreatLevel.Low -> LowThreat
        | ThreatLevel.Medium -> ModeraThreat
        | ThreatLevel.High | ThreatLevel.Critical -> HighThreat
        | _ -> NoThreat
            | GameStarted(aiId, savedGame) -> 
                this.ProcessGameStart(aiId, savedGame)
            | FrameUpdate frame -> 
                this.ProcessFrameUpdate(frame)
            | UnitCreated(unitId, builderId, _) -> 
                this.ProcessUnitCreated(unitId, builderId)
            | UnitDamaged(unitId, attackerId, damage, _) -> 
                this.ProcessUnitDamaged(unitId, attackerId, damage)
            | UnitDestroyed(unitId, attackerId, _) -> 
                this.ProcessUnitDestroyed(unitId, attackerId)
            | GameEnded reason -> 
                this.ProcessGameEnd(reason)
        
        member this.PlanActions resources =
            this.CreateBuildPlan(resources)
        
        member this.Initialize context =
            printfn "F# AI initialized"
        
        member this.Shutdown reason =
            printfn "F# AI shutting down: %d" reason
        
        member this.GetStrategy() =
            currentStrategy

/// Example concrete F# AI implementation
type ExampleFSharpAI(context: IGameContext) =
    inherit BaseFSharpAI(context)
    
    let mutable buildQueue = []
    let mutable myUnits = []
    
    override this.ProcessUnitCreated(unitId, builderId) =
        base.ProcessUnitCreated(unitId, builderId)
        
        match context.GetUnit(unitId) with
        | Some unit -> 
            myUnits <- unit :: myUnits
            match unit with
            | Commander -> this.HandleCommanderCreated(unit)
            | Builder -> this.HandleBuilderCreated(unit)
            | Factory -> this.HandleFactoryCreated(unit)
            | Combat -> this.HandleCombatUnitCreated(unit)
            | Economic -> this.HandleEconomicUnitCreated(unit)
            | Other -> printfn "Unknown unit type: %s" unit.DefName
        | None -> ()
    
    override this.CreateBuildPlan(resources: ResourceState) : Decision<Command> list =
        match resources with
        | ResourcePoor ->
            this.PlanEconomicExpansion(resources)
        | ResourceModerate ->
            this.PlanBalancedDevelopment(resources)
        | ResourceRich ->
            this.PlanMilitaryExpansion(resources)
    
    member private this.PlanEconomicExpansion(resources: ResourceState) : Decision<Command> list =
        let builders = GameContext.getUnitsByCategory context "BUILDER"
        match builders with
        | [] -> []
        | builder :: _ ->
            [
                {
                    Action = Build(builder.Id, "armsolar", Vector3.Zero)
                    Priority = 100
                    Reason = "Need energy production"
                    RequiredResources = Some resources
                    EstimatedDuration = Some 300<frame>
                }
            ]
    
    member private this.PlanBalancedDevelopment(resources: ResourceState) : Decision<Command> list =
        // Implement balanced build strategy
        []
    
    member private this.PlanMilitaryExpansion(resources: ResourceState) : Decision<Command> list =
        // Implement military build strategy
        []
    
    member private this.HandleCommanderCreated(commander: UnitInfo) =
        printfn "Commander created - securing base"
    
    member private this.HandleBuilderCreated(builder: UnitInfo) =
        printfn "Builder created - assigning construction tasks"
    
    member private this.HandleFactoryCreated(factory: UnitInfo) =
        printfn "Factory created - planning production"
    
    member private this.HandleCombatUnitCreated(combatUnit: UnitInfo) =
        printfn "Combat unit created - adding to army"
    
    member private this.HandleEconomicUnitCreated(economicUnit: UnitInfo) =
        printfn "Economic unit created - optimizing production"

/// F# Agent-based AI for concurrent processing
type AgentBasedAI(context: IGameContext) =
    
    type AIMessage =
        | ProcessEvent of GameEvent
        | GetStrategy of AsyncReplyChannel<Strategy>
        | UpdateStrategy of Strategy
        | GetStatus of AsyncReplyChannel<string>
    
    let agent = MailboxProcessor<AIMessage>.Start(fun inbox ->
        let rec loop strategy gameState = async {
            let! message = inbox.Receive()
            
            match message with
            | ProcessEvent event ->
                match event with
                | FrameUpdate frame ->
                    let newState = context.GetResources()
                    return! loop strategy newState
                | _ ->
                    return! loop strategy gameState
            
            | GetStrategy reply ->
                reply.Reply(strategy)
                return! loop strategy gameState
            
            | UpdateStrategy newStrategy ->
                return! loop newStrategy gameState
            
            | GetStatus reply ->
                let status = sprintf "Strategy: %A, Frame: %A" strategy gameState.CurrentFrame
                reply.Reply(status)
                return! loop strategy gameState
        }
        
        let initialState = context.GetResources()
        loop EconomicExpansion initialState
    )
    
    interface IAI with
        member this.HandleEvent event =
            agent.Post(ProcessEvent event)
        
        member this.PlanActions resources =
            // Get strategy from agent and plan accordingly
            let strategy = agent.PostAndReply(GetStrategy)
            match strategy with
            | EconomicExpansion -> this.PlanEconomic(resources)
            | MilitaryBuildup -> this.PlanMilitary(resources)
            | _ -> []
        
        member this.Initialize context =
            printfn "Agent-based F# AI initialized"
        
        member this.Shutdown reason =
            printfn "Agent-based F# AI shutting down: %d" reason
        
        member this.GetStrategy() =
            agent.PostAndReply(GetStrategy)
    
    member private this.PlanEconomic(resources: ResourceState) : Decision<Command> list =
        []
    
    member private this.PlanMilitary(resources: ResourceState) : Decision<Command> list =
        []
    
    member this.UpdateStrategyAsync(strategy: Strategy) =
        agent.Post(UpdateStrategy strategy)
    
    member this.GetStatusAsync() =
        agent.PostAndAsyncReply(GetStatus)
