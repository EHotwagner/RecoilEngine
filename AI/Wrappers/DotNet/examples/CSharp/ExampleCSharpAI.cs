/// Complete example demonstrating F#-first design with C# compatibility
using SpringAI.Core;
using SpringAI.CSharp.AI;
using SpringAI.CSharp.Commands;
using SpringAI.CSharp.Compatibility;
using System.Numerics;

namespace SpringAI.CSharp.Examples
{
    /// <summary>
    /// Example C# AI that uses the F#-first architecture
    /// This demonstrates how C# developers can benefit from F#'s superior type system
    /// while using familiar C# patterns
    /// </summary>
    public class ExampleCSharpAI : BaseCSharpAI
    {
        private readonly List<CSharpCommand> _commandQueue = new();
        private CSharpStrategy _currentStrategy = CSharpStrategy.EconomicExpansion;
        private int _lastPlanFrame = 0;

        public override void OnInit(int aiId, bool savedGame)
        {
            base.OnInit(aiId, savedGame);
            Console.WriteLine($"Example C# AI {aiId} initialized with F# core");
        }

        protected override void ProcessUpdate(int frame, ResourceState resources, Strategy fsharpStrategy)
        {
            // Convert F# strategy to C# enum for easier consumption
            _currentStrategy = TypeConverters.ToCSStrategy(fsharpStrategy);
            
            // Plan every 30 frames (1 second at 30 FPS)
            if (frame - _lastPlanFrame >= 30)
            {
                PlanActions(frame, resources);
                _lastPlanFrame = frame;
            }

            // Execute queued commands
            ExecuteQueuedCommands();
        }

        private void PlanActions(int frame, ResourceState resources)
        {
            var csharpResources = TypeConverters.ToCSResourceInfo(resources);
            var friendlyUnits = GetFriendlyUnits();

            // Plan based on current strategy
            switch (_currentStrategy)
            {
                case CSharpStrategy.EconomicExpansion:
                    PlanEconomicExpansion(csharpResources, friendlyUnits);
                    break;
                    
                case CSharpStrategy.MilitaryBuildup:
                    PlanMilitaryBuildup(csharpResources, friendlyUnits);
                    break;
                    
                case CSharpStrategy.DefensivePosition:
                    PlanDefensiveActions(csharpResources, friendlyUnits);
                    break;
                    
                default:
                    PlanDefaultActions(csharpResources, friendlyUnits);
                    break;
            }
        }

        private void PlanEconomicExpansion(CSharpResourceInfo resources, List<CSharpUnitInfo> units)
        {
            // Get available builders
            var builders = units.Where(u => u.IsBuilder && u.IsAlive).ToList();
            
            // Check what we need
            var solarCollectors = units.Count(u => u.DefName.Contains("solr") && u.IsAlive);
            var metalExtractors = units.Count(u => u.DefName.Contains("mex") && u.IsAlive);
            
            foreach (var builder in builders.Take(3)) // Limit to 3 builders
            {
                // Build solar collectors if we need energy
                if (solarCollectors < 5 && resources.CanAfford(50, 0))
                {
                    var position = FindBuildPosition(builder.Position, "armsolr");
                    if (position.HasValue)
                    {
                        _commandQueue.Add(new BuildCommand(builder.Id, "armsolr", position.Value));
                        solarCollectors++; // Optimistic counting
                        Console.WriteLine($"Queued solar collector build by builder {builder.Id}");
                    }
                }
                // Build metal extractors if we need metal
                else if (metalExtractors < 3 && resources.CanAfford(100, 50))
                {
                    var position = FindMetalSpot(builder.Position);
                    if (position.HasValue)
                    {
                        _commandQueue.Add(new BuildCommand(builder.Id, "armmex", position.Value));
                        metalExtractors++; // Optimistic counting
                        Console.WriteLine($"Queued metal extractor build by builder {builder.Id}");
                    }
                }
            }
        }

        private void PlanMilitaryBuildup(CSharpResourceInfo resources, List<CSharpUnitInfo> units)
        {
            var factories = units.Where(u => u.IsFactory && u.IsAlive).ToList();
            
            foreach (var factory in factories)
            {
                // Build combat units if we have resources
                if (resources.CanAfford(150, 75))
                {
                    // Example: Build a tank
                    _commandQueue.Add(new BuildCommand(factory.Id, "armstump", factory.Position));
                    Console.WriteLine($"Queued tank build at factory {factory.Id}");
                }
            }
        }

        private void PlanDefensiveActions(CSharpResourceInfo resources, List<CSharpUnitInfo> units)
        {
            // Move all combat units to defensive positions
            var combatUnits = units.Where(u => u.IsCombat && u.IsAlive).ToList();
            
            foreach (var unit in combatUnits)
            {
                // Move to a defensive position (example: near commander)
                var commander = units.FirstOrDefault(u => u.IsCommander);
                if (commander != null)
                {
                    var defensivePosition = commander.Position + new Vector3(100, 0, 100);
                    _commandQueue.Add(new MoveCommand(unit.Id, defensivePosition));
                    Console.WriteLine($"Moving unit {unit.Id} to defensive position");
                }
            }
        }

        private void PlanDefaultActions(CSharpResourceInfo resources, List<CSharpUnitInfo> units)
        {
            // Fallback: Just try to build something useful
            var builders = units.Where(u => u.IsBuilder && u.IsAlive).Take(1);
            
            foreach (var builder in builders)
            {
                if (resources.CanAfford(50, 0))
                {
                    var position = FindBuildPosition(builder.Position, "armsolr");
                    if (position.HasValue)
                    {
                        _commandQueue.Add(new BuildCommand(builder.Id, "armsolr", position.Value));
                    }
                }
            }
        }

        private void ExecuteQueuedCommands()
        {
            // Execute commands through F# core (which provides validation and type safety)
            foreach (var command in _commandQueue.Take(5)) // Limit execution rate
            {
                try
                {
                    // Convert C# command to F# command and execute through F# context
                    var fsharpCommand = TypeConverters.ToFSCommand(command);
                    var executed = ExecuteCommand(fsharpCommand);
                    
                    if (executed)
                    {
                        Console.WriteLine($"Successfully executed: {command}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to execute: {command}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing command {command}: {ex.Message}");
                }
            }

            // Remove executed commands
            var toRemove = Math.Min(5, _commandQueue.Count);
            _commandQueue.RemoveRange(0, toRemove);
        }

        private Vector3? FindBuildPosition(Vector3 near, string unitDefName)
        {
            // Simple example: offset from current position
            // Real implementation would use pathfinding and terrain analysis
            var offset = new Vector3(
                (float)(new Random().NextDouble() * 200 - 100),
                0,
                (float)(new Random().NextDouble() * 200 - 100)
            );
            
            var position = near + offset;
            
            // Check if we can build there (this uses F# validation internally)
            if (FSharpContext?.CanBuildAt(position, unitDefName) == true)
            {
                return position;
            }
            
            return null;
        }

        private Vector3? FindMetalSpot(Vector3 near)
        {
            // Simplified metal spot finder
            // Real implementation would query the map for metal deposits
            return FindBuildPosition(near, "armmex");
        }

        public override void OnUnitCreated(int unitId, int builderId)
        {
            base.OnUnitCreated(unitId, builderId);
            
            // Example: If it's a factory, start building units
            var unit = Callback?.GetUnit(unitId);
            if (unit?.IsFactory == true)
            {
                Console.WriteLine($"New factory {unitId} created! Planning production...");
                // Queue up some units to build
                _commandQueue.Add(new BuildCommand(unitId, "armwar", unit.Position));
            }
        }

        public override void OnUnitDestroyed(int unitId, int attackerId)
        {
            base.OnUnitDestroyed(unitId, attackerId);
            
            // Remove any commands for this unit
            _commandQueue.RemoveAll(cmd => 
                (cmd is MoveCommand move && move.UnitId == unitId) ||
                (cmd is AttackCommand attack && attack.AttackerId == unitId) ||
                (cmd is StopCommand stop && stop.UnitId == unitId));
            
            Console.WriteLine($"Removed commands for destroyed unit {unitId}");
        }
    }

    /// <summary>
    /// Advanced example showing how C# can leverage F# computation expressions
    /// through the compatibility layer
    /// </summary>
    public class AdvancedCSharpAI : BaseCSharpAI
    {
        public override void OnInit(int aiId, bool savedGame)
        {
            base.OnInit(aiId, savedGame);
            
            // This C# AI can leverage F# decision-making through the adapter
            Console.WriteLine("Advanced C# AI with F# decision engine initialized");
        }

        protected override void ProcessUpdate(int frame, ResourceState resources, Strategy strategy)
        {
            // Example of using F# computational benefits from C#
            if (frame % 60 == 0) // Every 2 seconds
            {
                AnalyzeGameStateWithFSharpCore(resources);
            }
        }

        private async void AnalyzeGameStateWithFSharpCore(ResourceState resources)
        {
            try
            {
                // This would use F# computation expressions for complex analysis
                // The F# core provides much better error handling and type safety
                
                var units = GetFriendlyUnits();
                var enemyUnits = Callback?.GetEnemyUnits() ?? new List<CSharpUnitInfo>();

                // Example complex decision that benefits from F# type safety
                var threatLevel = AnalyzeThreatLevel(units, enemyUnits);
                var strategy = DetermineStrategy(resources, threatLevel);
                
                Console.WriteLine($"F# analysis: Threat={threatLevel}, Strategy={strategy}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in F# analysis: {ex.Message}");
            }
        }

        private string AnalyzeThreatLevel(List<CSharpUnitInfo> friendlyUnits, List<CSharpUnitInfo> enemyUnits)
        {
            // This kind of complex analysis benefits greatly from F#'s pattern matching
            // and discriminated unions, even when called from C#
            
            var friendlyPower = friendlyUnits.Count(u => u.IsCombat);
            var enemyPower = enemyUnits.Count(u => u.IsCombat);
            
            return (friendlyPower, enemyPower) switch
            {
                var (f, e) when e == 0 => "None",
                var (f, e) when f > e * 2 => "Low", 
                var (f, e) when f > e => "Medium",
                var (f, e) when e > f * 2 => "Critical",
                _ => "High"
            };
        }

        private string DetermineStrategy(ResourceState resources, string threatLevel)
        {
            // Even this C# method benefits from F#'s underlying type system
            return (resources.Metal, resources.Energy, threatLevel) switch
            {
                ( > 500, > 1000, "None" or "Low") => "Expansion",
                ( > 300, > 500, "Medium") => "Balanced",
                (_, _, "High" or "Critical") => "Defense",
                _ => "Economy"
            };
        }
    }
}
