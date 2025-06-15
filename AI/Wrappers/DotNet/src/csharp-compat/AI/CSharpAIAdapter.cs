/// Bridge between C# and F# AI interfaces
using SpringAI.Core;
using SpringAI.CSharp.Events;
using SpringAI.CSharp.Compatibility;
using Microsoft.FSharp.Collections;

namespace SpringAI.CSharp.AI
{
    /// <summary>
    /// Adapter that bridges C# AI interface to F# core implementation
    /// This allows C# developers to use traditional callback-style APIs
    /// while the core logic runs on the superior F# types
    /// </summary>
    public class CSharpAIAdapter : ICSharpAI
    {
        private readonly IAI _fsharpAI;
        private readonly IGameContext _fsharpContext;
        private readonly ICSharpGameCallback _csharpCallback;

        public CSharpAIAdapter(IAI fsharpAI, IGameContext fsharpContext, ICSharpGameCallback csharpCallback)
        {
            _fsharpAI = fsharpAI;
            _fsharpContext = fsharpContext;
            _csharpCallback = csharpCallback;
        }

        public void OnInit(int aiId, bool savedGame)
        {
            // Convert C# call to F# event
            var fsharpEvent = GameEvent.NewGameStarted(aiId, savedGame);
            _fsharpAI.HandleEvent(fsharpEvent);
            _fsharpAI.Initialize(_fsharpContext);
        }

        public void OnUpdate(int frame)
        {
            // Convert C# call to F# event
            var fsharpEvent = GameEvent.NewFrameUpdate(frame);
            _fsharpAI.HandleEvent(fsharpEvent);

            // Get F# AI decisions and execute them
            var resources = _fsharpContext.GetResources();
            var decisions = _fsharpAI.PlanActions(resources);
            
            foreach (var decision in decisions)
            {
                ExecuteDecision(decision);
            }
        }

        public void OnUnitCreated(int unitId, int builderId)
        {
            var fsharpEvent = GameEvent.NewUnitCreated(unitId, builderId, _csharpCallback.GetCurrentFrame());
            _fsharpAI.HandleEvent(fsharpEvent);
        }

        public void OnUnitDamaged(int unitId, int attackerId, float damage)
        {
            var fsharpEvent = GameEvent.NewUnitDamaged(unitId, attackerId, damage, _csharpCallback.GetCurrentFrame());
            _fsharpAI.HandleEvent(fsharpEvent);
        }

        public void OnUnitDestroyed(int unitId, int attackerId)
        {
            var fsharpEvent = GameEvent.NewUnitDestroyed(unitId, attackerId, _csharpCallback.GetCurrentFrame());
            _fsharpAI.HandleEvent(fsharpEvent);
        }

        public void OnRelease(int reason)
        {
            var fsharpEvent = GameEvent.NewGameEnded(reason);
            _fsharpAI.HandleEvent(fsharpEvent);
            _fsharpAI.Shutdown(reason);
        }

        private void ExecuteDecision(Decision<Command> decision)
        {
            var result = _fsharpContext.ExecuteCommand(decision.Action);
            
            // Log the result
            switch (result)
            {
                case { IsOk: true }:
                    Console.WriteLine($"Command executed: {decision.Reason} - {result.ResultValue}");
                    break;
                case { IsError: true }:
                    Console.WriteLine($"Command failed: {decision.Reason} - {result.ErrorValue}");
                    break;
            }
        }
    }

    /// <summary>
    /// Base class for C# AI implementations that use F# core
    /// Provides a familiar C# interface while leveraging F# types internally
    /// </summary>
    public abstract class BaseCSharpAI : ICSharpAI
    {
        protected ICSharpGameCallback Callback { get; private set; } = null!;
        protected IGameContext FSharpContext { get; private set; } = null!;

        private IAI? _fsharpCore;

        public virtual void OnInit(int aiId, bool savedGame)
        {
            // Derived classes should override and call base.OnInit
            Console.WriteLine($"C# AI {aiId} initialized (saved: {savedGame})");
        }

        public virtual void OnUpdate(int frame)
        {
            // Get F# context and make decisions
            if (_fsharpCore != null && FSharpContext != null)
            {
                var resources = FSharpContext.GetResources();
                var strategy = _fsharpCore.GetStrategy();
                
                // Let derived class handle the logic
                ProcessUpdate(frame, resources, strategy);
            }
        }

        public virtual void OnUnitCreated(int unitId, int builderId)
        {
            Console.WriteLine($"Unit {unitId} created by {builderId}");
        }

        public virtual void OnUnitDamaged(int unitId, int attackerId, float damage)
        {
            Console.WriteLine($"Unit {unitId} damaged by {attackerId} ({damage:F1} damage)");
        }

        public virtual void OnUnitDestroyed(int unitId, int attackerId)
        {
            Console.WriteLine($"Unit {unitId} destroyed by {attackerId}");
        }

        public virtual void OnRelease(int reason)
        {
            Console.WriteLine($"C# AI shutting down: {reason}");
            _fsharpCore?.Shutdown(reason);
        }

        /// <summary>
        /// Initialize the C# AI with F# context
        /// </summary>
        protected void InitializeWithFSharpCore(ICSharpGameCallback callback, IGameContext fsharpContext, IAI fsharpCore)
        {
            Callback = callback;
            FSharpContext = fsharpContext;
            _fsharpCore = fsharpCore;
        }

        /// <summary>
        /// Override this to process frame updates with F# types
        /// </summary>
        protected virtual void ProcessUpdate(int frame, ResourceState resources, Strategy strategy)
        {
            // Default implementation - derived classes should override
        }

        /// <summary>
        /// Execute F# command through C# callback
        /// </summary>
        protected bool ExecuteCommand(Command command)
        {
            if (FSharpContext == null) return false;
            
            var result = FSharpContext.ExecuteCommand(command);
            return result.IsOk;
        }

        /// <summary>
        /// Get F# units as C# types
        /// </summary>
        protected List<CSharpUnitInfo> GetFriendlyUnits()
        {
            if (FSharpContext == null) return new List<CSharpUnitInfo>();
            
            return FSharpContext.GetFriendlyUnits()
                .Select(TypeConverters.ToCSUnitInfo)
                .ToList();
        }
    }
}
