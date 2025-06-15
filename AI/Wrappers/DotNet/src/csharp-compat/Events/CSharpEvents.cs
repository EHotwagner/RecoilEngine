/// C# compatibility layer for F# core events
using SpringAI.Core;
using Microsoft.FSharp.Core;

namespace SpringAI.CSharp.Events
{
    /// <summary>
    /// C# event wrapper that converts to F# GameEvent
    /// This is a compatibility layer - F# events are the primary API
    /// </summary>
    public abstract class CSharpEvent
    {
        public int Frame { get; set; }
        
        /// <summary>
        /// Convert this C# event to the primary F# GameEvent type
        /// </summary>
        public abstract GameEvent ToFSharpEvent();
    }

    /// <summary>
    /// C# wrapper for initialization events
    /// </summary>
    public class InitEvent : CSharpEvent
    {
        public int AIId { get; set; }
        public bool SavedGame { get; set; }

        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewGameStarted(AIId, SavedGame);
    }

    /// <summary>
    /// C# wrapper for frame update events
    /// </summary>
    public class UpdateEvent : CSharpEvent
    {
        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewFrameUpdate(Frame);
    }

    /// <summary>
    /// C# wrapper for unit creation events
    /// </summary>
    public class UnitCreatedEvent : CSharpEvent
    {
        public int UnitId { get; set; }
        public int BuilderId { get; set; }

        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewUnitCreated(UnitId, BuilderId, Frame);
    }

    /// <summary>
    /// C# wrapper for unit damage events
    /// </summary>
    public class UnitDamagedEvent : CSharpEvent
    {
        public int UnitId { get; set; }
        public int AttackerId { get; set; }
        public float Damage { get; set; }

        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewUnitDamaged(UnitId, AttackerId, Damage, Frame);
    }

    /// <summary>
    /// C# wrapper for unit destruction events
    /// </summary>
    public class UnitDestroyedEvent : CSharpEvent
    {
        public int UnitId { get; set; }
        public int AttackerId { get; set; }

        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewUnitDestroyed(UnitId, AttackerId, Frame);
    }

    /// <summary>
    /// C# wrapper for game end events
    /// </summary>
    public class ReleaseEvent : CSharpEvent
    {
        public int Reason { get; set; }

        public override GameEvent ToFSharpEvent() =>
            GameEvent.NewGameEnded(Reason);
    }
}
