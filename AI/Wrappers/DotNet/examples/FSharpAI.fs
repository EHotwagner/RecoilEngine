// F# Example AI demonstrating idiomatic F# usage of the .NET AI wrapper
module BAR.AI.FSharp.Example

open System
open System.Collections.Generic
open SpringAI
open SpringAI.Events

// F#-friendly type definitions
type ResourceState = {
    Metal: float32
    Energy: float32
    MetalIncome: float32
    EnergyIncome: float32
}

type GamePhase = 
    | EarlyGame of frame: int
    | MidGame of frame: int  
    | LateGame of frame: int

type Strategy =
    | EconomicExpansion
    | MilitaryBuildup
    | TechAdvancement
    | DefensivePosition

type UnitRole =
    | Commander of health: float32
    | Builder of efficiency: float32
    | Factory of unitType: string
    | Combat of weaponType: string
    | Economic of resourceType: string

// Active patterns for unit classification
let (|Commander|Builder|Combat|Economic|Other|) (unit: Unit) =
    if unit.Categories.Contains("COMMANDER") then Commander
    elif unit.Categories.Contains("BUILDER") then Builder
    elif unit.Categories.Contains("WEAPON") then Combat
    elif unit.Categories.Contains("ENERGY") || unit.Categories.Contains("METAL") then Economic
    else Other

// Active pattern for game phase detection
let (|EarlyGame|MidGame|LateGame|) frame =
    match frame with
    | f when f < 1800 -> EarlyGame    // First 60 seconds (30 FPS)
    | f when f < 9000 -> MidGame      // 1-5 minutes  
    | _ -> LateGame                   // 5+ minutes

// F# AI implementation
type ExampleFSharpAI() =
    inherit BaseAI()
    
    // Mutable state (could be made immutable with agents)
    let mutable gameState = {
        Metal = 0.0f
        Energy = 0.0f
        MetalIncome = 0.0f
        EnergyIncome = 0.0f
    }
    
    let mutable currentStrategy = EconomicExpansion
    let mutable myUnits = []
    
    // F#-friendly helper functions
    let tryGetUnit unitId =
        match base.Callback.GetUnit(unitId) with
        | null -> None
        | unit -> Some unit
    
    let getUnitsOfType unitType =
        base.Callback.GetFriendlyUnits()
        |> Seq.filter (fun u -> u.DefName = unitType)
        |> List.ofSeq
    
    let countUnitsOfType unitType =
        getUnitsOfType unitType |> List.length
    
    // Resource management with option types
    let canAfford metalCost energyCost =
        gameState.Metal >= metalCost && gameState.Energy >= energyCost
    
    // Strategy selection using pattern matching
    let selectStrategy frame resourceState =
        match frame, resourceState with
        | EarlyGame, { Metal = m; Energy = e } when m < 500.0f || e < 1000.0f ->
            EconomicExpansion
        | MidGame, { MetalIncome = mi; EnergyIncome = ei } when mi > 10.0f && ei > 50.0f ->
            MilitaryBuildup
        | LateGame, _ ->
            TechAdvancement
        | _ ->
            DefensivePosition
    
    // Build order using F# lists and pattern matching
    let getNextBuildTarget strategy =
        match strategy with
        | EconomicExpansion ->
            if countUnitsOfType "armsolar" < 5 then Some "armsolar"
            elif countUnitsOfType "armmex" < 10 then Some "armmex"
            else Some "armlab"
            
        | MilitaryBuildup ->
            if countUnitsOfType "armvp" = 0 then Some "armvp"
            elif countUnitsOfType "armkbot" < 20 then Some "armkbot"
            else Some "armtank"
            
        | TechAdvancement ->
            if countUnitsOfType "armaap" = 0 then Some "armaap"
            else Some "armadvsol"
            
        | DefensivePosition ->
            if countUnitsOfType "armllt" < 5 then Some "armllt"
            else Some "armrl"
    
    // Execute build commands using computation expressions
    let executeBuild unitDefName position =
        match base.Callback.GetUnitDefByName(unitDefName) with
        | null -> Error $"Unknown unit: {unitDefName}"
        | unitDef when not (canAfford unitDef.MetalCost unitDef.EnergyCost) ->
            Error $"Cannot afford {unitDefName}"
        | unitDef ->
            // Find a builder
            let builders = 
                base.Callback.GetFriendlyUnits()
                |> Seq.filter (fun u -> u.Categories.Contains("BUILDER"))
                |> List.ofSeq
            
            match builders with
            | [] -> Error "No builders available"
            | builder :: _ ->
                let buildCmd = {
                    UnitId = builder.Id
                    UnitDefId = unitDef.Id
                    Position = position
                    Options = []
                }
                base.Callback.ExecuteBuildCommand(buildCmd)
                Ok $"Building {unitDefName}"
    
    // Event handling with pattern matching
    override this.OnInit(skirmishAIId, savedGame) =
        base.OnInit(skirmishAIId, savedGame)
        printfn "F# AI initialized for team %d" skirmishAIId
    
    override this.OnUpdate(frame) =
        base.OnUpdate(frame)
        
        // Update resource state
        gameState <- {
            Metal = base.Callback.GetMetal()
            Energy = base.Callback.GetEnergy()
            MetalIncome = base.Callback.GetMetalIncome()
            EnergyIncome = base.Callback.GetEnergyIncome()
        }
        
        // Update strategy based on game phase and resources
        currentStrategy <- selectStrategy frame gameState
        
        // Execute strategy every 30 frames (1 second)
        if frame % 30 = 0 then
            this.ExecuteStrategy()
    
    override this.OnUnitCreated(unitId, builderId) =
        base.OnUnitCreated(unitId, builderId)
        
        match tryGetUnit unitId with
        | Some unit ->
            myUnits <- unit :: myUnits
            printfn "Created unit: %s (ID: %d)" unit.DefName unitId
            
            // Handle unit-specific initialization
            match unit with
            | Commander -> this.HandleCommanderCreated(unit)
            | Builder -> this.HandleBuilderCreated(unit)
            | Combat -> this.HandleCombatUnitCreated(unit)
            | Economic -> this.HandleEconomicUnitCreated(unit)
            | Other -> ()
        | None -> ()
    
    override this.OnUnitDestroyed(unitId, attackerId) =
        base.OnUnitDestroyed(unitId, attackerId)
        
        // Remove from our unit list
        myUnits <- myUnits |> List.filter (fun u -> u.Id <> unitId)
        
        match tryGetUnit attackerId with
        | Some attacker ->
            printfn "Unit %d destroyed by %s" unitId attacker.DefName
        | None ->
            printfn "Unit %d destroyed" unitId
    
    override this.OnEnemyDamaged(unitId, attackerId, damage) =
        base.OnEnemyDamaged(unitId, attackerId, damage)
        // Could implement threat assessment here
        ()
    
    // Private methods for strategy execution
    member private this.ExecuteStrategy() =
        match getNextBuildTarget currentStrategy with
        | Some unitDefName ->
            // Find a good position (simplified)
            let position = Vector3(0.0f, 0.0f, 0.0f) // Could use pathfinding
            
            match executeBuild unitDefName position with
            | Ok message -> printfn "%s" message
            | Error error -> printfn "Build failed: %s" error
        | None ->
            printfn "No build target for current strategy: %A" currentStrategy
    
    member private this.HandleCommanderCreated(commander: Unit) =
        printfn "Commander created - securing area"
        // Could implement commander-specific logic
        ()
    
    member private this.HandleBuilderCreated(builder: Unit) =
        printfn "Builder created - assigning construction tasks"
        // Could implement builder task assignment
        ()
    
    member private this.HandleCombatUnitCreated(combatUnit: Unit) =
        printfn "Combat unit created - adding to army"
        // Could implement army formation and tactics
        ()
    
    member private this.HandleEconomicUnitCreated(economicUnit: Unit) =
        printfn "Economic unit created - optimizing resource production"
        // Could implement economic optimization
        ()

// Example of using the F# AI with agents for concurrent processing
module AgentBasedAI =
    open System.Threading
    
    type AIMessage =
        | ProcessFrame of int
        | HandleUnitEvent of int * string
        | UpdateStrategy of Strategy
        | GetStatus of AsyncReplyChannel<string>
    
    type AgentAI() =
        inherit BaseAI()
        
        let agent = MailboxProcessor<AIMessage>.Start(fun inbox ->
            let rec loop strategy = async {
                let! message = inbox.Receive()
                
                match message with
                | ProcessFrame frame ->
                    // Process frame in background
                    do! Async.Sleep(10) // Simulate processing
                    return! loop strategy
                    
                | HandleUnitEvent(unitId, eventType) ->
                    printfn "Agent handling %s for unit %d" eventType unitId
                    return! loop strategy
                    
                | UpdateStrategy newStrategy ->
                    printfn "Strategy updated to: %A" newStrategy
                    return! loop newStrategy
                    
                | GetStatus reply ->
                    reply.Reply($"Current strategy: {strategy}")
                    return! loop strategy
            }
            loop EconomicExpansion
        )
        
        override this.OnUpdate(frame) =
            base.OnUpdate(frame)
            agent.Post(ProcessFrame frame)
        
        override this.OnUnitCreated(unitId, builderId) =
            base.OnUnitCreated(unitId, builderId)
            agent.Post(HandleUnitEvent(unitId, "Created"))
        
        member this.UpdateStrategyAsync(strategy) =
            agent.Post(UpdateStrategy strategy)
        
        member this.GetStatusAsync() =
            agent.PostAndAsyncReply(GetStatus)

// Example of functional build order DSL
module BuildOrderDSL =
    
    type BuildCondition =
        | MetalReaches of float32
        | EnergyReaches of float32
        | FrameReaches of int
        | UnitsOfType of string * int
    
    type BuildStep =
        | BuildUnit of string * int
        | WaitFor of BuildCondition
        | Parallel of BuildStep list
        | Sequential of BuildStep list
    
    // DSL helper functions
    let buildUnit name count = BuildUnit(name, count)
    let waitForMetal amount = WaitFor(MetalReaches amount)
    let waitForEnergy amount = WaitFor(EnergyReaches amount)
    let waitForUnits unitType count = WaitFor(UnitsOfType(unitType, count))
    let inParallel steps = Parallel steps
    let inSequence steps = Sequential steps
    
    // Example build orders
    let armBasicBuildOrder = inSequence [
        buildUnit "armsolar" 2
        waitForMetal 200.0f
        buildUnit "armlab" 1
        inParallel [
            buildUnit "armsolar" 3
            buildUnit "armkbot" 5
        ]
        waitForEnergy 500.0f
        buildUnit "armvp" 1
    ]
    
    // Build order interpreter (simplified)
    let rec interpretBuildOrder (callback: IGameCallback) = function
        | BuildUnit(name, count) ->
            // Execute build command
            async { return sprintf "Building %d %s" count name }
            
        | WaitFor condition ->
            async {
                // Check condition
                return sprintf "Waiting for condition: %A" condition
            }
            
        | Parallel steps ->
            async {
                let! results = steps |> List.map (interpretBuildOrder callback) |> Async.Parallel
                return String.Join("; ", results)
            }
            
        | Sequential steps ->
            async {
                let mutable results = []
                for step in steps do
                    let! result = interpretBuildOrder callback step
                    results <- result :: results
                return String.Join(" -> ", List.rev results)
            }
