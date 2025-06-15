using System;
using System.Numerics;
using SpringAI.Events;

namespace SpringAI
{
    /// <summary>
    /// Interface that all Spring AI implementations must implement
    /// </summary>
    public interface IAI
    {
        /// <summary>
        /// Called when the AI is initialized
        /// </summary>
        /// <param name="skirmishAIId">The unique ID of this AI instance</param>
        /// <param name="savedGame">True if this is a saved game being loaded</param>
        void OnInit(int skirmishAIId, bool savedGame);

        /// <summary>
        /// Called every game frame (typically 30 times per second)
        /// </summary>
        /// <param name="frame">The current game frame number</param>
        void OnUpdate(int frame);

        /// <summary>
        /// Called when a unit starts construction
        /// </summary>
        /// <param name="unitId">The ID of the unit being created</param>
        /// <param name="builderId">The ID of the unit doing the building</param>
        void OnUnitCreated(int unitId, int builderId);

        /// <summary>
        /// Called when a unit takes damage
        /// </summary>
        /// <param name="unitId">The ID of the damaged unit</param>
        /// <param name="attackerId">The ID of the attacking unit (-1 if unknown)</param>
        /// <param name="damage">The amount of damage dealt</param>
        /// <param name="direction">The direction the damage came from</param>
        /// <param name="weaponDefId">The weapon definition ID that caused the damage</param>
        /// <param name="paralyzer">True if this was paralysis damage</param>
        void OnUnitDamaged(int unitId, int attackerId, float damage, Vector3 direction, int weaponDefId, bool paralyzer);

        /// <summary>
        /// Called when a unit is destroyed
        /// </summary>
        /// <param name="unitId">The ID of the destroyed unit</param>
        /// <param name="attackerId">The ID of the attacking unit (-1 if unknown)</param>
        /// <param name="weaponDefId">The weapon definition ID that caused the destruction</param>
        void OnUnitDestroyed(int unitId, int attackerId, int weaponDefId);

        /// <summary>
        /// Called when the AI should release resources and shut down
        /// </summary>
        /// <param name="reason">The reason for shutdown (0=unspecified, 1=game ended, etc.)</param>
        void OnRelease(int reason);
    }

    /// <summary>
    /// Abstract base class providing default implementations for AI events
    /// </summary>
    public abstract class BaseAI : IAI
    {
        /// <summary>
        /// The unique ID of this AI instance
        /// </summary>
        protected int SkirmishAIId { get; private set; }

        /// <summary>
        /// The game callback interface for issuing commands and queries
        /// </summary>
        protected IGameCallback Callback { get; private set; }

        /// <summary>
        /// Whether this AI has been initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// The current game frame
        /// </summary>
        protected int CurrentFrame { get; private set; }

        public virtual void OnInit(int skirmishAIId, bool savedGame)
        {
            SkirmishAIId = skirmishAIId;
            IsInitialized = true;
            // TODO: Initialize Callback when available
        }

        public virtual void OnUpdate(int frame)
        {
            CurrentFrame = frame;
        }

        public virtual void OnUnitCreated(int unitId, int builderId)
        {
            // Default: do nothing
        }

        public virtual void OnUnitDamaged(int unitId, int attackerId, float damage, Vector3 direction, int weaponDefId, bool paralyzer)
        {
            // Default: do nothing
        }

        public virtual void OnUnitDestroyed(int unitId, int attackerId, int weaponDefId)
        {
            // Default: do nothing
        }

        public virtual void OnRelease(int reason)
        {
            IsInitialized = false;
        }
    }
}
