/// F#-friendly wrapper functions for IGameCallback
module BAR.AI.FSharp.GameCallback

open System
open System.Threading.Tasks
open SpringAI
open BAR.AI.FSharp.Types
open BAR.AI.FSharp.ActivePatterns

/// Helper functions that return Option types instead of null
module Safe =
    let getUnit (callback: IGameCallback) unitId =
        match callback.GetUnit(unitId) with
        | null -> None
        | unit -> Some unit
    
    let getUnitDef (callback: IGameCallback) unitDefId =
        match callback.GetUnitDef(unitDefId) with
        | null -> None
        | unitDef -> Some unitDef
    
    let getUnitDefByName (callback: IGameCallback) unitDefName =
        match callback.GetUnitDefByName(unitDefName) with
        | null -> None
        | unitDef -> Some unitDef
    
    let getFeature (callback: IGameCallback) featureId =
        match callback.GetFeature(featureId) with
        | null -> None
        | feature -> Some feature

/// Type-safe resource functions using units of measure
module Resources =
    let getMetal (callback: IGameCallback) : float32<metal> =
        callback.GetMetal() * 1.0f<metal>
    
    let getEnergy (callback: IGameCallback) : float32<energy> =
        callback.GetEnergy() * 1.0f<energy>
    
    let getMetalIncome (callback: IGameCallback) : float32<metal> =
        callback.GetMetalIncome() * 1.0f<metal>
    
    let getEnergyIncome (callback: IGameCallback) : float32<energy> =
        callback.GetEnergyIncome() * 1.0f<energy>
    
    let getCurrentFrame (callback: IGameCallback) : int<frame> =
        callback.GetCurrentFrame() * 1<frame>
    
    let getResourceState (callback: IGameCallback) : ResourceState =
        {
            Metal = getMetal callback
            Energy = getEnergy callback
            MetalIncome = getMetalIncome callback
            EnergyIncome = getEnergyIncome callback
            Frame = getCurrentFrame callback
        }
    
    let canAfford (callback: IGameCallback) (metalCost: float32<metal>) (energyCost: float32<energy>) : bool =
        let currentMetal = getMetal callback
        let currentEnergy = getEnergy callback
        currentMetal >= metalCost && currentEnergy >= energyCost

/// Unit management functions
module Units =
    let getFriendlyUnits (callback: IGameCallback) : UnitInfo list =
        callback.GetFriendlyUnits()
        |> Seq.map (fun unit -> {
            Id = unit.Id
            DefId = unit.DefId
            DefName = unit.DefName
            Classification = 
                match unit with
                | Commander(h, mh) -> UnitClassification.Commander(h, mh)
                | Builder(speed, cost) -> UnitClassification.Builder(speed, cost)
                | Factory(prodType, eff) -> UnitClassification.Factory(prodType, eff)
                | Combat(dmg, rng) -> UnitClassification.CombatUnit(dmg, rng)
                | Economic(resType, eff) -> UnitClassification.EconomicUnit(resType, eff)
                | Unknown -> UnitClassification.Unknown
            Position = Position.fromVector3 unit.Position
            Health = unit.Health * 1.0f<hp>
            MaxHealth = unit.MaxHealth * 1.0f<hp>
            Categories = unit.Categories |> List.ofSeq
            Team = unit.Team
        })
        |> List.ofSeq
    
    let getEnemyUnits (callback: IGameCallback) : UnitInfo list =
        callback.GetEnemyUnits()
        |> Seq.map (fun unit -> {
            Id = unit.Id
            DefId = unit.DefId
            DefName = unit.DefName
            Classification = UnitClassification.Unknown // Limited info for enemies
            Position = Position.fromVector3 unit.Position
            Health = unit.Health * 1.0f<hp>
            MaxHealth = unit.MaxHealth * 1.0f<hp>
            Categories = unit.Categories |> List.ofSeq
            Team = unit.Team
        })
        |> List.ofSeq
    
    let getUnitsOfType (callback: IGameCallback) (unitDefName: string) : UnitInfo list =
        getFriendlyUnits callback
        |> List.filter (fun u -> u.DefName = unitDefName)
    
    let countUnitsOfType (callback: IGameCallback) (unitDefName: string) : int =
        getUnitsOfType callback unitDefName |> List.length
    
    let getUnitsInRadius (callback: IGameCallback) (center: Position) (radius: float32<elmo>) : UnitInfo list =
        getFriendlyUnits callback
        |> List.filter (fun unit -> Position.distance unit.Position center <= radius)
    
    let getClosestUnit (callback: IGameCallback) (position: Position) : UnitInfo option =
        getFriendlyUnits callback
        |> List.sortBy (fun unit -> Position.distance unit.Position position)
        |> List.tryHead

/// Command execution with error handling
module Commands =
    let createBuildCommand (builderId: int) (unitDefName: string) (position: Position) : BuildCommand =
        match Safe.getUnitDefByName (callback) unitDefName with
        | Some unitDef ->
            {
                UnitId = builderId
                UnitDefId = unitDef.Id
                Position = Position.toVector3 position
                Options = []
            }
        | None ->
            failwith $"Unknown unit definition: {unitDefName}"
    
    let executeBuildCommand (callback: IGameCallback) (command: BuildCommand) : CommandResult<string> =
        try
            let success = callback.ExecuteBuildCommand(command)
            if success then
                Success $"Build command executed for unit {command.UnitDefId}"
            else
                Failure "Build command failed"
        with
        | ex -> Failure ex.Message
    
    let moveUnit (callback: IGameCallback) (unitId: int) (position: Position) : CommandResult<string> =
        try
            let moveCmd = {
                UnitId = unitId
                Position = Position.toVector3 position
                Options = []
            }
            let success = callback.ExecuteMoveCommand(moveCmd)
            if success then
                Success $"Unit {unitId} moving to {position}"
            else
                Failure $"Move command failed for unit {unitId}"
        with
        | ex -> Failure ex.Message
    
    let attackUnit (callback: IGameCallback) (attackerId: int) (targetId: int) : CommandResult<string> =
        try
            let attackCmd = {
                UnitId = attackerId
                TargetId = targetId
                Options = []
            }
            let success = callback.ExecuteAttackCommand(attackCmd)
            if success then
                Success $"Unit {attackerId} attacking unit {targetId}"
            else
                Failure $"Attack command failed"
        with
        | ex -> Failure ex.Message

/// Async operations for non-blocking AI
module Async =
    let getUnitsAsync (callback: IGameCallback) : Async<UnitInfo list> =
        async { return Units.getFriendlyUnits callback }
    
    let getResourceStateAsync (callback: IGameCallback) : Async<ResourceState> =
        async { return Resources.getResourceState callback }
    
    let executeBuildCommandAsync (callback: IGameCallback) (command: BuildCommand) : Async<CommandResult<string>> =
        async { return Commands.executeBuildCommand callback command }

/// Computation expression for command chaining
type CommandBuilder(callback: IGameCallback) =
    member _.Bind(result: CommandResult<'a>, f: 'a -> CommandResult<'b>) : CommandResult<'b> =
        match result with
        | Success value -> f value
        | Failure error -> Failure error
    
    member _.Return(value: 'a) : CommandResult<'a> =
        Success value
    
    member _.ReturnFrom(result: CommandResult<'a>) : CommandResult<'a> =
        result

/// Railway-oriented programming helpers
module Result =
    let bind f result =
        match result with
        | Success value -> f value
        | Failure error -> Failure error
    
    let map f result =
        match result with
        | Success value -> Success (f value)
        | Failure error -> Failure error
    
    let mapError f result =
        match result with
        | Success value -> Success value
        | Failure error -> Failure (f error)

/// Higher-level operations combining multiple commands
module Operations =
    let findAndMoveBuilder (callback: IGameCallback) (targetPosition: Position) : CommandResult<string> =
        let commandBuilder = CommandBuilder(callback)
        commandBuilder {
            let builders = Units.getFriendlyUnits callback |> List.filter (fun u -> 
                match u.Classification with
                | UnitClassification.Builder _ -> true
                | _ -> false)
            
            match builders with
            | [] -> return! Failure "No builders available"
            | builder :: _ ->
                return! Commands.moveUnit callback builder.Id targetPosition
        }
    
    let buildUnitSequence (callback: IGameCallback) (buildOrders: (string * Position) list) : CommandResult<string list> =
        let results = 
            buildOrders
            |> List.map (fun (unitDefName, position) ->
                findAndMoveBuilder callback position
                |> Result.bind (fun _ ->
                    match Units.getUnitsOfType callback "builder" |> List.tryHead with
                    | Some builder ->
                        let buildCmd = Commands.createBuildCommand builder.Id unitDefName position
                        Commands.executeBuildCommand callback buildCmd
                    | None -> 
                        Failure "No builders available"))
        
        // Combine results
        let failures = results |> List.choose (function | Failure e -> Some e | _ -> None)
        let successes = results |> List.choose (function | Success s -> Some s | _ -> None)
        
        if List.isEmpty failures then
            Success successes
        else
            Failure (String.Join("; ", failures))

/// Threat assessment functions
module ThreatAssessment =
    let assessThreat (callback: IGameCallback) (position: Position) (radius: float32<elmo>) : ThreatInfo =
        let enemies = Units.getEnemyUnits callback
        let nearbyEnemies = 
            enemies
            |> List.filter (fun enemy -> Position.distance enemy.Position position <= radius)
        
        let totalDamage = 
            nearbyEnemies
            |> List.sumBy (fun enemy -> 
                match enemy.Classification with
                | UnitClassification.CombatUnit(damage, _) -> float32 damage
                | _ -> 0.0f) * 1.0f<dps>
        
        let level = 
            match List.length nearbyEnemies, totalDamage with
            | count, _ when count > 10 -> Critical
            | count, damage when count > 5 || float32 damage > 1000.0f -> High
            | count, damage when count > 2 || float32 damage > 500.0f -> Medium
            | _ -> Low
        
        let closestEnemy = 
            nearbyEnemies
            |> List.sortBy (fun enemy -> Position.distance enemy.Position position)
            |> List.tryHead
        
        {
            Level = level
            Source = closestEnemy |> Option.map (fun e -> e.Position) |> Option.defaultValue position
            Distance = closestEnemy |> Option.map (fun e -> Position.distance e.Position position) |> Option.defaultValue (1000.0f<elmo>)
            EstimatedDamage = totalDamage
            UnitCount = List.length nearbyEnemies
        }
