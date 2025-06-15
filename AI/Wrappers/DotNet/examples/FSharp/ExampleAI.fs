/// Complete F# AI example demonstrating data-oriented, high-performance F# AI
module BAR.AI.FSharp.ExampleAI

open System
open System.Numerics
open SpringAI.Core
open SpringAI.Core.DataOrientedTypes
open SpringAI.Core.DataOrientedProcessing
open BAR.AI.FSharp.Types
open BAR.AI.FSharp.ActivePatterns
open BAR.AI.FSharp.GameCallback

/// Example F# AI demonstrating data-oriented, array-based processing
type DataOrientedBARAI(context: IGameContext) =
    inherit BaseFSharpAI(context)
    
    // Immutable state using record types with array data
    let mutable currentState = {
        Metal = 0.0f<metal>
        Energy = 0.0f<energy>
        MetalIncome = 0.0f<metal>
        EnergyIncome = 0.0f<energy>
        Frame = 0<frame>
    }
    
    let mutable worldStateCache: WorldState option = None
    let mutable spatialGridCache: SpatialGrid option = None
    let mutable performanceMetrics = context.GetPerformanceMetrics()
    
    // Array pools for memory efficiency
    let commandPool = Array.zeroCreate<Command> 1000
    let mutable commandPoolIndex = 0
    
    /// Update state using efficient array operations
    override this.ProcessFrameUpdate(frame) =
        base.ProcessFrameUpdate(frame)
        
        // Cache world state for this frame
        worldStateCache <- Some (context.GetWorldState())
        
        // Update spatial grid every 10 frames for efficiency
        if int frame % 10 = 0 then
            let units = context.GetFriendlyUnitsArray()
            spatialGridCache <- Some (buildSpatialGrid units 100.0f<elmo>)
        
        // Update performance metrics every 30 frames
        if int frame % 30 = 0 then
            performanceMetrics <- context.GetPerformanceMetrics()
    
    /// Efficient event batch processing
    override this.ProcessEventBatch(eventBatch: EventBatch) =
        // Process events in batches for optimal performance
        processEventBatch eventBatch.Events this.ProcessSingleEvent
    
    /// Data-oriented build plan creation using world state arrays
    override this.CreateBuildPlanFromWorldState(worldState: WorldState) : CommandBatch =
        let builders = filterUnitsByFlags worldState 0b00000001uy // Builder flag
        
        if Array.isEmpty builders then
            { Commands = [||]; CommandCount = 0; Priority = [||]; FrameToExecute = [||] }
        else
            // Analyze resource needs using array operations
            let resourceAnalysis = this.AnalyzeResourceNeeds(worldState)
            let commands = this.GenerateOptimalCommands(worldState, builders, resourceAnalysis)
            
            {
                Commands = commands
                CommandCount = commands.Length
                Priority = Array.create commands.Length 5
                FrameToExecute = Array.create commands.Length worldState.CurrentFrame
            }
    
    /// Analyze resource needs using efficient array operations
    member private this.AnalyzeResourceNeeds(worldState: WorldState) =
        let myFaction = ARM
        let myUnits = 
            Array.zip worldState.UnitIds worldState.UnitFactions
            |> Array.filter (fun (_, faction) -> faction = myFaction)
            |> Array.map fst
        
        // Count economic vs military units using array operations
        let economicCount = myUnits |> Array.filter (fun _ -> true) |> Array.length // Simplified
        let militaryCount = myUnits |> Array.filter (fun _ -> true) |> Array.length // Simplified
        
        {|
            NeedsMoreMetal = economicCount < 5
            NeedsMoreEnergy = economicCount < 10
            NeedsDefenses = militaryCount < economicCount / 2
            TotalUnits = myUnits.Length
        |}
    
    /// Generate optimal commands using data-oriented approach
    member private this.GenerateOptimalCommands(worldState: WorldState, builders: int array, analysis) =
        let buildCommands = ResizeArray<Command>()
        
        // Use spatial grid for optimal placement
        match spatialGridCache with
        | Some spatialGrid ->
            for builderId in builders |> Array.take (min 5 builders.Length) do
                let builderIndex = Array.findIndex ((=) builderId) worldState.UnitIds
                let builderPos = worldState.UnitPositions.[builderIndex]
                
                // Determine what to build based on analysis
                let unitToBuild = 
                    if analysis.NeedsMoreMetal then "armmex"
                    elif analysis.NeedsMoreEnergy then "armsolar"
                    elif analysis.NeedsDefenses then "armrl"
                    else "armlab"
                
                buildCommands.Add(Build(builderId, unitToBuild, builderPos))
        | None -> ()
        
        buildCommands.ToArray()
    
    /// Threat assessment using spatial data and array operations
    member private this.AssessThreatLevel(worldState: WorldState, position: Vector3) : ThreatAssessment =
        calculateThreatAssessment worldState position 500.0f<elmo>
    
    /// Batch unit management using array operations
    member private this.ManageUnitsBatch(worldState: WorldState) : Command array =
        let commands = ResizeArray<Command>()
        
        // Find idle units using bit flags
        let idleUnits = filterUnitsByFlags worldState 0b00000010uy // Idle flag
        
        // Assign tasks to idle units efficiently
        for unitId in idleUnits |> Array.take (min 20 idleUnits.Length) do
            let unitIndex = Array.findIndex ((=) unitId) worldState.UnitIds
            let unitPos = worldState.UnitPositions.[unitIndex]
            
            // Simple patrol command for now
            commands.Add(Patrol(unitId, [unitPos; Vector3(unitPos.X + 100.0f, unitPos.Y, unitPos.Z + 100.0f)]))
        
        commands.ToArray()
    
    /// Strategy selection using pattern matching and data analysis
    member private this.SelectStrategyFromWorldState(worldState: WorldState) =
        let analysis = this.AnalyzeResourceNeeds(worldState)
        let threatAssessment = this.AssessThreatLevel(worldState, Vector3.Zero)
        
        match worldState.CurrentFrame, analysis, threatAssessment.Level with
        | frame, _, _ when frame < 1800<frame> -> EconomicExpansion
        | frame, analysis, ThreatLevel.Low when analysis.TotalUnits > 20 -> MilitaryBuildup
        | _, _, level when level >= ThreatLevel.Medium -> DefensivePosition
        | frame, _, _ when frame > 7200<frame> -> TechAdvancement
        | _ -> EconomicExpansion
            this.RequestBuild "armsolar" 2
        | Balanced ->
            this.ProcessNormalEconomy()
        | Overflow ->
            this.ConvertToMilitary()
    
    /// Military planning using F# lists and pattern matching
    let planMilitaryActions () =
        let combatUnits = 
            Units.getFriendlyUnits base.Callback
            |> List.filter (fun u -> 
                match u.Classification with
                | UnitClassification.CombatUnit _ -> true
                | _ -> false)
        
        let commanderPos = 
            Units.getFriendlyUnits base.Callback
            |> List.tryFind (fun u -> 
                match u.Classification with
                | UnitClassification.Commander _ -> true
                | _ -> false)
            |> Option.map (fun u -> u.Position)
            |> Option.defaultValue Position.zero
        
        match combatUnits, commanderPos with
        | ReadyToAttack -> this.ExecuteAttack()
        | NeedsReinforcement -> this.BuildMoreUnits()
        | DefensiveOnly -> this.SetupDefenses()
    
    /// Threat assessment using active patterns
    let assessCurrentThreats () =
        let commanderPos = 
            Units.getFriendlyUnits base.Callback
            |> List.tryFind (fun u -> u.DefName.Contains("com"))
            |> Option.map (fun u -> u.Position)
            |> Option.defaultValue Position.zero
        
        ThreatAssessment.assessThreat base.Callback commanderPos 1000.0f<elmo>
    
    /// Build execution using railway-oriented programming
    let executeBuildOrder unitDefName count =
        command {
            let! builders = 
                match Units.getUnitsOfType base.Callback "builder" with
                | [] -> Failure "No builders available"
                | builders -> Success builders
            
            let! buildSite = 
                let pos = Position.zero // Simplified - would use pathfinding
                Success pos
            
            let! unitDef =
                match Safe.getUnitDefByName base.Callback unitDefName with
                | Some def -> Success def
                | None -> Failure $"Unknown unit: {unitDefName}"
            
            let! affordCheck =
                let metalCost = unitDef.MetalCost * 1.0f<metal>
                let energyCost = unitDef.EnergyCost * 1.0f<energy>
                if Resources.canAfford base.Callback metalCost energyCost then
                    Success ()
                else
                    Failure $"Cannot afford {unitDefName}"
            
            let builder = List.head builders
            let buildCmd = Commands.createBuildCommand builder.Id unitDefName buildSite
            return! Commands.executeBuildCommand base.Callback buildCmd
        }
    
    /// Override AI events with F# pattern matching
    override this.OnInit(skirmishAIId, savedGame) =
        base.OnInit(skirmishAIId, savedGame)
        printfn "Functional F# AI initialized for team %d" skirmishAIId
        
        // Initialize with early game strategy
        currentStrategy <- EconomicExpansion
    
    override this.OnUpdate(frame) =
        base.OnUpdate(frame)
        
        // Update current state immutably
        currentState <- Resources.getResourceState base.Callback
        
        // Assess threats
        let threats = assessCurrentThreats()
        threatLevel <- threats.Level
        
        // Update strategy based on current conditions
        currentStrategy <- selectStrategy currentState (Resources.getCurrentFrame base.Callback) threats
        
        // Execute strategy every 30 frames (1 second at 30 FPS)
        if (int currentState.Frame) % 30 = 0 then
            this.ExecuteCurrentStrategy()
    
    override this.OnUnitCreated(unitId, builderId) =
        base.OnUnitCreated(unitId, builderId)
        
        match Safe.getUnit base.Callback unitId with
        | Some unit ->
            match unit with
            | Commander(health, maxHealth) ->
                printfn "Commander created with health %A/%A" health maxHealth
                this.SecureCommander(unit)
                
            | Builder(speed, cost) ->
                printfn "Builder created with build speed %A" speed
                this.AssignBuilderTasks(unit)
                
            | Factory(prodType, efficiency) ->
                printfn "Factory created: %s with efficiency %A" prodType efficiency
                this.QueueProduction(unit)
                
            | Combat(damage, range) ->
                printfn "Combat unit created: damage %A, range %A" damage range
                this.AddToArmy(unit)
                
            | Economic(resType, efficiency) ->
                printfn "Economic unit created: %A with efficiency %A" resType efficiency
                
            | Unknown ->
                printfn "Unknown unit type created: %s" unit.DefName
        | None -> ()
    
    override this.OnUnitDestroyed(unitId, attackerId) =
        base.OnUnitDestroyed(unitId, attackerId)
        
        // Assess if we lost something critical
        match Safe.getUnit base.Callback unitId with
        | Some unit when unit.DefName.Contains("com") ->
            printfn "CRITICAL: Commander destroyed!"
            currentStrategy <- DefensivePosition
        | Some unit ->
            printfn "Unit destroyed: %s" unit.DefName
        | None -> ()
    
    override this.OnEnemyDamaged(unitId, attackerId, damage) =
        base.OnEnemyDamaged(unitId, attackerId, damage)
        
        // Update threat assessment when we successfully damage enemies
        match Safe.getUnit base.Callback unitId with
        | Some enemy ->
            match enemy with
            | Critical ->
                printfn "Critically damaged enemy unit %s" enemy.DefName
            | Damaged ->
                printfn "Damaged enemy unit %s" enemy.DefName  
            | Healthy -> ()
        | None -> ()
    
    /// Private methods for strategy execution
    member private this.ExecuteCurrentStrategy() =
        match currentStrategy with
        | EconomicExpansion -> this.ProcessEconomicExpansion()
        | MilitaryBuildup -> this.ProcessMilitaryBuildup()
        | TechAdvancement -> this.ProcessTechAdvancement()
        | DefensivePosition -> this.ProcessDefensiveStrategy()
        | AttackMode target -> this.ProcessAttackStrategy(target)
    
    member private this.ProcessEconomicExpansion() =
        // Build order using F# pattern matching
        let buildQueue = [
            ("armsolar", 3, if (int currentState.Energy) < 1000 then 100 else 50)
            ("armmex", 5, if (int currentState.Metal) < 500 then 100 else 50)
            ("armlab", 1, 75)
        ]
        
        buildQueue
        |> List.sortByDescending (fun (_, _, priority) -> priority)
        |> List.iter (fun (unitName, count, _) ->
            let currentCount = Units.countUnitsOfType base.Callback unitName
            if currentCount < count then
                this.RequestBuild unitName 1
        )
    
    member private this.ProcessMilitaryBuildup() =
        // Focus on combat units
        let militaryQueue = [
            ("armvp", 1)    // Vehicle plant
            ("armkbot", 10) // Basic bots
            ("armpw", 20)   // Peewees
        ]
        
        militaryQueue
        |> List.iter (fun (unitName, targetCount) ->
            let currentCount = Units.countUnitsOfType base.Callback unitName
            if currentCount < targetCount then
                this.RequestBuild unitName 1
        )
    
    member private this.ProcessTechAdvancement() =
        // Advanced technology
        let techQueue = [
            ("armaap", 1)      // Advanced aircraft plant
            ("armadvsol", 3)   // Advanced solar
            ("armfus", 1)      // Fusion reactor
        ]
        
        techQueue
        |> List.iter (fun (unitName, targetCount) ->
            let currentCount = Units.countUnitsOfType base.Callback unitName
            if currentCount < targetCount then
                this.RequestBuild unitName 1
        )
    
    member private this.ProcessDefensiveStrategy() =
        // Build defensive structures
        let defenseQueue = [
            ("armllt", 5)   // Light laser towers
            ("armrl", 3)    // Rocket launchers
            ("armrad", 2)   // Radar
        ]
        
        defenseQueue
        |> List.iter (fun (unitName, targetCount) ->
            let currentCount = Units.countUnitsOfType base.Callback unitName
            if currentCount < targetCount then
                this.RequestBuild unitName 1
        )
    
    member private this.ProcessAttackStrategy(target: Position) =
        // Coordinate attack on target
        let combatUnits = 
            Units.getFriendlyUnits base.Callback
            |> List.filter (fun u -> 
                match u.Classification with
                | UnitClassification.CombatUnit _ -> true
                | _ -> false)
        
        // Move combat units to target
        combatUnits
        |> List.iter (fun unit ->
            Commands.moveUnit base.Callback unit.Id target |> ignore
        )
    
    member private this.RequestBuild unitDefName count =
        for i = 1 to count do
            match executeBuildOrder unitDefName 1 with
            | Success message -> printfn "%s" message
            | Failure error -> printfn "Build failed: %s" error
    
    member private this.SecureCommander(commander: Unit) =
        // Move commander to safe position
        let safePos = Position.zero // Would calculate safe position
        Commands.moveUnit base.Callback commander.Id safePos |> ignore
    
    member private this.AssignBuilderTasks(builder: Unit) =
        // Assign construction tasks to builder
        printfn "Assigning tasks to builder %d" builder.Id
    
    member private this.QueueProduction(factory: Unit) =
        // Queue unit production in factory
        printfn "Queueing production in factory %d" factory.Id
    
    member private this.AddToArmy(combatUnit: Unit) =
        // Add unit to military formations
        printfn "Adding unit %d to army" combatUnit.Id

/// Agent-based AI using F# MailboxProcessor for concurrent processing
type AgentBasedFSharpAI() =
    inherit BaseAI()
    
    type AIMessage =
        | ProcessFrame of int<frame>
        | HandleUnitCreated of int * int
        | HandleUnitDestroyed of int * int
        | UpdateStrategy of Strategy
        | GetStatus of AsyncReplyChannel<string>
        | ExecuteCommand of (unit -> CommandResult<string>)
    
    let aiAgent = MailboxProcessor<AIMessage>.Start(fun inbox ->
        let rec loop strategy state = async {
            let! message = inbox.Receive()
            
            match message with
            | ProcessFrame frame ->
                // Process game frame in background
                let newState = { state with Frame = frame }
                return! loop strategy newState
                
            | HandleUnitCreated(unitId, builderId) ->
                printfn "Agent: Unit %d created by %d" unitId builderId
                return! loop strategy state
                
            | HandleUnitDestroyed(unitId, attackerId) ->
                printfn "Agent: Unit %d destroyed by %d" unitId attackerId
                return! loop strategy state
                
            | UpdateStrategy newStrategy ->
                printfn "Agent: Strategy updated to %A" newStrategy
                return! loop newStrategy state
                
            | GetStatus reply ->
                let status = sprintf "Strategy: %A, Frame: %A" strategy state.Frame
                reply.Reply(status)
                return! loop strategy state
                
            | ExecuteCommand cmdFunc ->
                match cmdFunc() with
                | Success msg -> printfn "Agent: Command succeeded - %s" msg
                | Failure err -> printfn "Agent: Command failed - %s" err
                return! loop strategy state
        }
        
        let initialState = {
            Metal = 0.0f<metal>
            Energy = 0.0f<energy>
            MetalIncome = 0.0f<metal>
            EnergyIncome = 0.0f<energy>
            Frame = 0<frame>
        }
        
        loop EconomicExpansion initialState
    )
    
    override this.OnUpdate(frame) =
        base.OnUpdate(frame)
        aiAgent.Post(ProcessFrame (frame * 1<frame>))
    
    override this.OnUnitCreated(unitId, builderId) =
        base.OnUnitCreated(unitId, builderId)
        aiAgent.Post(HandleUnitCreated(unitId, builderId))
    
    override this.OnUnitDestroyed(unitId, attackerId) =
        base.OnUnitDestroyed(unitId, attackerId)
        aiAgent.Post(HandleUnitDestroyed(unitId, attackerId))
    
    member this.UpdateStrategyAsync(strategy: Strategy) =
        aiAgent.Post(UpdateStrategy strategy)
    
    member this.GetStatusAsync() =
        aiAgent.PostAndAsyncReply(GetStatus)
    
    member this.ExecuteCommandAsync(command: unit -> CommandResult<string>) =
        aiAgent.Post(ExecuteCommand command)
