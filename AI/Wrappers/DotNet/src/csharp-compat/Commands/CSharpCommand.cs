/// C# command types - compatibility layer for F# Command discriminated union
using System.Numerics;

namespace SpringAI.CSharp.Commands
{
    /// <summary>
    /// Base class for C# commands - these get converted to F# Command discriminated union
    /// </summary>
    public abstract class CSharpCommand
    {
        public abstract string CommandType { get; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Build command in C# format
    /// </summary>
    public class BuildCommand : CSharpCommand
    {
        public int BuilderId { get; }
        public string UnitDefName { get; }
        public Vector3 Position { get; }

        public BuildCommand(int builderId, string unitDefName, Vector3 position)
        {
            BuilderId = builderId;
            UnitDefName = unitDefName;
            Position = position;
        }

        public override string CommandType => "Build";

        public override string ToString() => $"Build {UnitDefName} at {Position} with builder {BuilderId}";
    }

    /// <summary>
    /// Move command in C# format
    /// </summary>
    public class MoveCommand : CSharpCommand
    {
        public int UnitId { get; }
        public Vector3 Destination { get; }

        public MoveCommand(int unitId, Vector3 destination)
        {
            UnitId = unitId;
            Destination = destination;
        }

        public override string CommandType => "Move";

        public override string ToString() => $"Move unit {UnitId} to {Destination}";
    }

    /// <summary>
    /// Attack command in C# format
    /// </summary>
    public class AttackCommand : CSharpCommand
    {
        public int AttackerId { get; }
        public int TargetId { get; }

        public AttackCommand(int attackerId, int targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        public override string CommandType => "Attack";

        public override string ToString() => $"Unit {AttackerId} attack unit {TargetId}";
    }

    /// <summary>
    /// Stop command in C# format
    /// </summary>
    public class StopCommand : CSharpCommand
    {
        public int UnitId { get; }

        public StopCommand(int unitId)
        {
            UnitId = unitId;
        }

        public override string CommandType => "Stop";

        public override string ToString() => $"Stop unit {UnitId}";
    }

    /// <summary>
    /// Guard command in C# format
    /// </summary>
    public class GuardCommand : CSharpCommand
    {
        public int UnitId { get; }
        public int TargetId { get; }

        public GuardCommand(int unitId, int targetId)
        {
            UnitId = unitId;
            TargetId = targetId;
        }

        public override string CommandType => "Guard";

        public override string ToString() => $"Unit {UnitId} guard unit {TargetId}";
    }

    /// <summary>
    /// Patrol command in C# format
    /// </summary>
    public class PatrolCommand : CSharpCommand
    {
        public int UnitId { get; }
        public List<Vector3> Positions { get; }

        public PatrolCommand(int unitId, params Vector3[] positions)
        {
            UnitId = unitId;
            Positions = positions.ToList();
        }

        public override string CommandType => "Patrol";

        public override string ToString() => $"Unit {UnitId} patrol {Positions.Count} positions";
    }
}
