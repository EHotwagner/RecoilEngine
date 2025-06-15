/// C# AI interface - compatibility layer for F# core
using System.Numerics;

namespace SpringAI.CSharp.AI
{
    /// <summary>
    /// Traditional C# AI interface - this is a compatibility layer
    /// The primary API is the F# IAI interface in SpringAI.Core
    /// </summary>
    public interface ICSharpAI
    {
        /// <summary>
        /// Initialize the AI
        /// </summary>
        void OnInit(int aiId, bool savedGame);

        /// <summary>
        /// Called every game frame
        /// </summary>
        void OnUpdate(int frame);

        /// <summary>
        /// Called when a unit is created
        /// </summary>
        void OnUnitCreated(int unitId, int builderId);

        /// <summary>
        /// Called when a unit takes damage
        /// </summary>
        void OnUnitDamaged(int unitId, int attackerId, float damage);

        /// <summary>
        /// Called when a unit is destroyed
        /// </summary>
        void OnUnitDestroyed(int unitId, int attackerId);

        /// <summary>
        /// Called when the AI is being shut down
        /// </summary>
        void OnRelease(int reason);
    }

    /// <summary>
    /// C# game callback interface - wraps F# IGameContext
    /// </summary>
    public interface ICSharpGameCallback
    {
        /// <summary>
        /// Get current metal amount
        /// </summary>
        float GetMetal();

        /// <summary>
        /// Get current energy amount
        /// </summary>
        float GetEnergy();

        /// <summary>
        /// Get metal income rate
        /// </summary>
        float GetMetalIncome();

        /// <summary>
        /// Get energy income rate
        /// </summary>
        float GetEnergyIncome();

        /// <summary>
        /// Get current frame number
        /// </summary>
        int GetCurrentFrame();

        /// <summary>
        /// Get unit by ID (returns null if not found)
        /// </summary>
        CSharpUnitInfo? GetUnit(int unitId);        /// <summary>
        /// Get all friendly units
        /// </summary>
        List<CSharpUnitInfo> GetFriendlyUnits();

        /// <summary>
        /// Get all enemy units
        /// </summary>
        List<CSharpUnitInfo> GetEnemyUnits();

        /// <summary>
        /// Execute a command and return success status
        /// </summary>
        bool ExecuteCommand(CSharpCommand command);

        /// <summary>
        /// Get unit definition by name
        /// </summary>
        CSharpUnitDefinition? GetUnitDef(string defName);

        /// <summary>
        /// Check if building is possible at location
        /// </summary>
        bool CanBuildAt(Vector3 position, string unitDefName);

        /// <summary>
        /// Get map dimensions
        /// </summary>
        (int width, int height) GetMapSize();
    }

    /// <summary>
    /// C# unit information - compatibility wrapper for F# UnitInfo
    /// </summary>
    public class CSharpUnitInfo
    {
        public int Id { get; set; }
        public int DefId { get; set; }
        public string DefName { get; set; } = "";
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public BARFaction Faction { get; set; }
        public List<string> Categories { get; set; } = new();
        public bool IsAlive { get; set; }

        /// <summary>
        /// Health percentage (0.0 - 1.0)
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? Health / MaxHealth : 0;

        /// <summary>
        /// Check if unit has a specific category
        /// </summary>
        public bool HasCategory(string category) => Categories.Contains(category, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Check if unit is a builder
        /// </summary>
        public bool IsBuilder => HasCategory("BUILDER");

        /// <summary>
        /// Check if unit is a factory
        /// </summary>
        public bool IsFactory => HasCategory("FACTORY");

        /// <summary>
        /// Check if unit is a combat unit
        /// </summary>
        public bool IsCombat => HasCategory("WEAPON");

        /// <summary>
        /// Check if unit is the commander
        /// </summary>
        public bool IsCommander => HasCategory("COMMANDER");
    }

    /// <summary>
    /// C# unit definition - compatibility wrapper for F# UnitDefinition
    /// </summary>
    public class CSharpUnitDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DefName { get; set; } = "";
        public float MetalCost { get; set; }
        public float EnergyCost { get; set; }
        public int BuildTime { get; set; }
        public float MaxHealth { get; set; }
        public List<string> Categories { get; set; } = new();
        public BARFaction Faction { get; set; }
        public List<string> CanBuild { get; set; } = new();

        /// <summary>
        /// Check if this unit can build the specified unit
        /// </summary>
        public bool CanBuildUnit(string unitDefName) => CanBuild.Contains(unitDefName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// C# resource information - compatibility wrapper for F# ResourceState
    /// </summary>
    public class CSharpResourceInfo
    {
        public float Metal { get; set; }
        public float Energy { get; set; }
        public float MetalIncome { get; set; }
        public float EnergyIncome { get; set; }
        public int CurrentFrame { get; set; }

        /// <summary>
        /// Check if we can afford the specified costs
        /// </summary>
        public bool CanAfford(float metalCost, float energyCost) => 
            Metal >= metalCost && Energy >= energyCost;

        /// <summary>
        /// Time in frames to accumulate the specified resources
        /// </summary>
        public int FramesToAccumulate(float metalCost, float energyCost)
        {
            var metalFrames = MetalIncome > 0 ? Math.Max(0, (metalCost - Metal) / MetalIncome) : float.MaxValue;
            var energyFrames = EnergyIncome > 0 ? Math.Max(0, (energyCost - Energy) / EnergyIncome) : float.MaxValue;
            return (int)Math.Ceiling(Math.Max(metalFrames, energyFrames));
        }
        IReadOnlyList<CSharpUnitInfo> GetFriendlyUnits();

        /// <summary>
        /// Execute a build command
        /// </summary>
        bool ExecuteBuildCommand(int builderId, string unitDefName, Vector3 position);

        /// <summary>
        /// Execute a move command
        /// </summary>
        bool ExecuteMoveCommand(int unitId, Vector3 destination);

        /// <summary>
        /// Execute an attack command
        /// </summary>
        bool ExecuteAttackCommand(int attackerId, int targetId);
    }

    /// <summary>
    /// C# unit information wrapper - wraps F# UnitInfo
    /// </summary>
    public class CSharpUnitInfo
    {
        public int Id { get; set; }
        public int DefId { get; set; }
        public string DefName { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public BARFactionCS Faction { get; set; }
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
        public bool IsAlive { get; set; }
    }

    /// <summary>
    /// C# version of BARFaction enum
    /// </summary>
    public enum BARFactionCS
    {
        Unknown = 0,
        ARM = 1,
        COR = 2
    }
}
