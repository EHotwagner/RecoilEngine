using System;

namespace SpringAI.Events
{
    /// <summary>
    /// Base class for all AI events sent from the engine
    /// </summary>
    public abstract class AIEvent
    {
        /// <summary>
        /// The frame number when this event occurred
        /// </summary>
        public int Frame { get; internal set; }

        /// <summary>
        /// Handle this event with the specified AI
        /// </summary>
        /// <param name="ai">The AI instance to handle the event</param>
        public abstract void Handle(IAI ai);
    }

    /// <summary>
    /// Event sent when the AI is initialized
    /// </summary>
    public class InitEvent : AIEvent
    {
        public int SkirmishAIId { get; internal set; }
        public bool SavedGame { get; internal set; }

        public override void Handle(IAI ai)
        {
            ai.OnInit(SkirmishAIId, SavedGame);
        }
    }

    /// <summary>
    /// Event sent every game frame (typically 30 times per second)
    /// </summary>
    public class UpdateEvent : AIEvent
    {
        public override void Handle(IAI ai)
        {
            ai.OnUpdate(Frame);
        }
    }

    /// <summary>
    /// Event sent when a unit is created (starts construction)
    /// </summary>
    public class UnitCreatedEvent : AIEvent
    {
        public int UnitId { get; internal set; }
        public int BuilderId { get; internal set; }

        public override void Handle(IAI ai)
        {
            ai.OnUnitCreated(UnitId, BuilderId);
        }
    }

    /// <summary>
    /// Event sent when a unit is damaged
    /// </summary>
    public class UnitDamagedEvent : AIEvent
    {
        public int UnitId { get; internal set; }
        public int AttackerId { get; internal set; }
        public float Damage { get; internal set; }
        public System.Numerics.Vector3 Direction { get; internal set; }
        public int WeaponDefId { get; internal set; }
        public bool Paralyzer { get; internal set; }

        public override void Handle(IAI ai)
        {
            ai.OnUnitDamaged(UnitId, AttackerId, Damage, Direction, WeaponDefId, Paralyzer);
        }
    }

    /// <summary>
    /// Event sent when a unit is destroyed
    /// </summary>
    public class UnitDestroyedEvent : AIEvent
    {
        public int UnitId { get; internal set; }
        public int AttackerId { get; internal set; }
        public int WeaponDefId { get; internal set; }

        public override void Handle(IAI ai)
        {
            ai.OnUnitDestroyed(UnitId, AttackerId, WeaponDefId);
        }
    }

    /// <summary>
    /// Event sent when the AI should release resources and shut down
    /// </summary>
    public class ReleaseEvent : AIEvent
    {
        public int Reason { get; internal set; }

        public override void Handle(IAI ai)
        {
            ai.OnRelease(Reason);
        }
    }
}
