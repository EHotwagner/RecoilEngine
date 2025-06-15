using System;
using System.Collections.Generic;
using System.Numerics;

namespace SpringAI
{
    /// <summary>
    /// Interface for game state queries and issuing commands
    /// </summary>
    public interface IGameCallback
    {
        // Unit management
        /// <summary>
        /// Get information about a specific unit
        /// </summary>
        Unit GetUnit(int unitId);

        /// <summary>
        /// Get all friendly units
        /// </summary>
        IEnumerable<Unit> GetFriendlyUnits();

        /// <summary>
        /// Get all enemy units
        /// </summary>
        IEnumerable<Unit> GetEnemyUnits();

        /// <summary>
        /// Get the position of a unit
        /// </summary>
        Vector3 GetUnitPosition(int unitId);

        /// <summary>
        /// Get the health of a unit
        /// </summary>
        float GetUnitHealth(int unitId);

        // Commands
        /// <summary>
        /// Give an order to a unit
        /// </summary>
        void GiveOrder(int unitId, Command command);

        /// <summary>
        /// Stop a unit
        /// </summary>
        void StopUnit(int unitId);

        /// <summary>
        /// Move a unit to a position
        /// </summary>
        void MoveUnit(int unitId, Vector3 position);

        /// <summary>
        /// Attack a target with a unit
        /// </summary>
        void AttackUnit(int unitId, int targetId);

        // Resource management
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

        // Map information
        /// <summary>
        /// Get map width in world units
        /// </summary>
        int GetMapWidth();

        /// <summary>
        /// Get map height in world units
        /// </summary>
        int GetMapHeight();

        /// <summary>
        /// Get elevation at a specific position
        /// </summary>
        float GetElevation(float x, float z);

        // Messaging
        /// <summary>
        /// Send a message to all players
        /// </summary>
        void SendTextMessage(string message);
    }

    /// <summary>
    /// Represents a game unit
    /// </summary>
    public class Unit
    {
        public int Id { get; set; }
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public int UnitDefId { get; set; }
        public int TeamId { get; set; }
        public bool IsAlive { get; set; }
    }

    /// <summary>
    /// Represents a command that can be given to units
    /// </summary>
    public abstract class Command
    {
        public abstract int GetCommandId();
    }

    /// <summary>
    /// Move command
    /// </summary>
    public class MoveCommand : Command
    {
        public Vector3 Position { get; set; }

        public MoveCommand(Vector3 position)
        {
            Position = position;
        }

        public override int GetCommandId()
        {
            return 10; // COMMAND_MOVE - actual value from AISCommands.h
        }
    }

    /// <summary>
    /// Attack command
    /// </summary>
    public class AttackCommand : Command
    {
        public int TargetId { get; set; }

        public AttackCommand(int targetId)
        {
            TargetId = targetId;
        }

        public override int GetCommandId()
        {
            return 20; // COMMAND_ATTACK - actual value from AISCommands.h
        }
    }

    /// <summary>
    /// Stop command
    /// </summary>
    public class StopCommand : Command
    {
        public override int GetCommandId()
        {
            return 0; // COMMAND_STOP - actual value from AISCommands.h
        }
    }
}
