/// Active patterns for F# pattern matching in BAR AI
module BAR.AI.FSharp.ActivePatterns

open System
open SpringAI
open BAR.AI.FSharp.Types

/// Active pattern for unit classification
let (|Commander|Builder|Factory|Combat|Economic|Unknown|) (unit: Unit) =
    let categories = unit.Categories |> Set.ofSeq
    
    if categories.Contains("COMMANDER") then
        Commander(unit.Health * 1.0f<hp>, unit.MaxHealth * 1.0f<hp>)
    elif categories.Contains("BUILDER") then
        let buildSpeed = 1.0f // Would need to get from unit def
        let metalCost = unit.MetalCost * 1.0f<metal>
        Builder(buildSpeed, metalCost)
    elif categories.Contains("FACTORY") then
        let factoryType = 
            if categories.Contains("KBOT") then "KBOT"
            elif categories.Contains("VEHICLE") then "VEHICLE"
            elif categories.Contains("AIRCRAFT") then "AIRCRAFT"
            elif categories.Contains("SHIP") then "SHIP"
            else "UNKNOWN"
        Factory(factoryType, 1.0f)
    elif categories.Contains("WEAPON") then
        let damage = unit.MaxDamage * 1.0f<dps>
        let range = unit.MaxRange * 1.0f<elmo>
        Combat(damage, range)
    elif categories.Contains("ENERGY") then
        Economic(EnergyGenerator, unit.EnergyMake)
    elif categories.Contains("METAL") then
        Economic(MetalExtractor, unit.MetalMake)
    else
        Unknown

/// Active pattern for game phase based on frame
let (|EarlyGame|MidGame|LateGame|) (frame: int<frame>) =
    let frameCount = int frame
    match frameCount with
    | f when f < 1800 -> EarlyGame(frame)    // First 60 seconds at 30 FPS
    | f when f < 9000 -> MidGame(frame)      // 1-5 minutes
    | _ -> LateGame(frame)                   // 5+ minutes

/// Active pattern for resource availability
let (|ResourcePoor|ResourceModerate|ResourceRich|) (resources: ResourceState) =
    let metalAmount = float32 resources.Metal
    let energyAmount = float32 resources.Energy
    
    match metalAmount, energyAmount with
    | m, e when m < 200.0f || e < 500.0f -> ResourcePoor
    | m, e when m < 1000.0f || e < 2000.0f -> ResourceModerate  
    | _ -> ResourceRich

/// Active pattern for faction detection
let (|ARM|COR|Unknown|) (unitDefName: string) =
    if unitDefName.StartsWith("arm", StringComparison.OrdinalIgnoreCase) then ARM
    elif unitDefName.StartsWith("cor", StringComparison.OrdinalIgnoreCase) then COR
    else Unknown

/// Active pattern for unit urgency based on health
let (|Critical|Damaged|Healthy|) (unit: Unit) =
    let healthPercent = unit.Health / unit.MaxHealth
    match healthPercent with
    | h when h < 0.2f -> Critical
    | h when h < 0.6f -> Damaged
    | _ -> Healthy

/// Active pattern for threat level assessment
let (|LowThreat|MediumThreat|HighThreat|CriticalThreat|) (enemyCount: int, distance: float32<elmo>) =
    let distanceValue = float32 distance
    match enemyCount, distanceValue with
    | count, dist when count > 10 && dist < 500.0f -> CriticalThreat
    | count, dist when count > 5 && dist < 1000.0f -> HighThreat
    | count, dist when count > 2 && dist < 1500.0f -> MediumThreat
    | _ -> LowThreat

/// Active pattern for build priority
let (|Critical|High|Medium|Low|) (unitDefName: string, currentUnits: Unit list, resources: ResourceState) =
    let unitCount = currentUnits |> List.filter (fun u -> u.DefName = unitDefName) |> List.length
    let metalAmount = float32 resources.Metal
    let energyAmount = float32 resources.Energy
    
    match unitDefName, unitCount, metalAmount, energyAmount with
    // Critical: Essential early game units
    | ("armsolar" | "corsolar"), count, _, energy when count < 3 && energy < 1000.0f -> Critical
    | ("armmex" | "cormex"), count, metal, _ when count < 5 && metal < 500.0f -> Critical
    | ("armcom" | "corcom"), 0, _, _ -> Critical
    
    // High: Important production and basic military
    | ("armlab" | "corlab"), 0, metal, energy when metal > 200.0f && energy > 500.0f -> High
    | ("armvp" | "corvp"), count, metal, energy when count < 2 && metal > 500.0f && energy > 1000.0f -> High
    
    // Medium: Expansion and advanced units
    | name, _, metal, energy when name.Contains("adv") && metal > 1000.0f && energy > 2000.0f -> Medium
    
    // Low: Everything else
    | _ -> Low

/// Active pattern for economic balance
let (|MetalStarved|EnergyStarved|Balanced|Overflow|) (resources: ResourceState) =
    let metalRatio = (float32 resources.Metal) / max 1.0f (float32 resources.MetalIncome)
    let energyRatio = (float32 resources.Energy) / max 1.0f (float32 resources.EnergyIncome)
    
    match metalRatio, energyRatio with
    | m, _ when m < 10.0f -> MetalStarved
    | _, e when e < 10.0f -> EnergyStarved
    | m, e when m > 200.0f && e > 200.0f -> Overflow
    | _ -> Balanced

/// Active pattern for terrain type (would need map analysis)
let (|LandMap|WaterMap|MixedMap|) (mapName: string) =
    // Simplified terrain detection - in reality would analyze map data
    if mapName.Contains("Sea") || mapName.Contains("Ocean") || mapName.Contains("Naval") then
        WaterMap
    elif mapName.Contains("Desert") || mapName.Contains("Mountain") || mapName.Contains("Land") then
        LandMap  
    else
        MixedMap

/// Active pattern for combat readiness
let (|ReadyToAttack|NeedsReinforcement|DefensiveOnly|) (combatUnits: Unit list, position: Position) =
    let totalFirepower = 
        combatUnits
        |> List.sumBy (fun u -> u.MaxDamage)
    
    let unitCount = List.length combatUnits
    
    match totalFirepower, unitCount with
    | power, count when power > 500.0f && count > 10 -> ReadyToAttack
    | power, count when power > 200.0f && count > 5 -> NeedsReinforcement
    | _ -> DefensiveOnly

/// Partial active pattern for optional unit retrieval
let (|ValidUnit|_|) (callback: IGameCallback) (unitId: int) =
    match callback.GetUnit(unitId) with
    | null -> None
    | unit -> Some unit

/// Partial active pattern for build site validation
let (|ValidBuildSite|_|) (callback: IGameCallback) (position: Vector3) (unitDefName: string) =
    // Simplified build site validation
    if callback.CanBuildAt(position, unitDefName) then
        Some position
    else
        None

/// Active pattern for command execution results
let (|Success|Failure|) (result: bool * string) =
    match result with
    | true, message -> Success message
    | false, error -> Failure error
