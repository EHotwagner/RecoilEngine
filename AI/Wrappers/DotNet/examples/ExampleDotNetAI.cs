using System;
using System.Numerics;
using SpringAI;

namespace SpringAI.Examples
{
    /// <summary>
    /// Example .NET AI implementation that demonstrates basic functionality
    /// </summary>
    public class ExampleDotNetAI : BaseAI
    {
        private int updateCount = 0;
        private const int UPDATE_FREQUENCY = 30; // Process every 30 frames (1 second at 30 FPS)

        public override void OnInit(int skirmishAIId, bool savedGame)
        {
            base.OnInit(skirmishAIId, savedGame);
            
            Console.WriteLine($"ExampleDotNetAI initialized! AI ID: {skirmishAIId}, Saved Game: {savedGame}");
            
            // Send a message to announce our presence
            if (Callback != null)
            {
                Callback.SendTextMessage($"Hello! This is .NET AI #{skirmishAIId} speaking!");
            }
        }

        public override void OnUpdate(int frame)
        {
            base.OnUpdate(frame);
            updateCount++;

            // Only process every UPDATE_FREQUENCY frames to avoid spam
            if (updateCount % UPDATE_FREQUENCY == 0)
            {
                ProcessPeriodicUpdate();
            }
        }

        private void ProcessPeriodicUpdate()
        {
            if (Callback == null) return;

            try
            {
                // Get resource information
                float metal = Callback.GetMetal();
                float energy = Callback.GetEnergy();
                
                // Log resource status occasionally
                if (updateCount % (UPDATE_FREQUENCY * 10) == 0) // Every 10 seconds
                {
                    Console.WriteLine($"Frame {CurrentFrame}: Metal={metal:F1}, Energy={energy:F1}");
                }

                // Get our units and give them basic orders
                var friendlyUnits = Callback.GetFriendlyUnits();
                foreach (var unit in friendlyUnits)
                {
                    if (unit.IsAlive)
                    {
                        ProcessUnit(unit);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPeriodicUpdate: {ex.Message}");
            }
        }

        private void ProcessUnit(Unit unit)
        {
            // Example: Simple unit behavior
            // This is just a demonstration - real AIs would have much more sophisticated logic
            
            // If unit health is low, try to retreat (move to a corner)
            if (unit.Health / unit.MaxHealth < 0.3f && Callback != null)
            {
                var retreatPosition = new Vector3(100, 0, 100); // Move to corner
                Callback.MoveUnit(unit.Id, retreatPosition);
                return;
            }

            // Look for enemies to attack
            if (Callback != null)
            {
                var enemies = Callback.GetEnemyUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive)
                    {
                        // Attack the first enemy we find
                        Callback.AttackUnit(unit.Id, enemy.Id);
                        return;
                    }
                }
            }
        }

        public override void OnUnitCreated(int unitId, int builderId)
        {
            Console.WriteLine($"Unit created: {unitId} by builder {builderId}");
            
            // Example: Give newly created units an initial order
            if (Callback != null)
            {
                // Move new units to the center of the map
                int mapWidth = Callback.GetMapWidth();
                int mapHeight = Callback.GetMapHeight();
                var centerPosition = new Vector3(mapWidth / 2f, 0, mapHeight / 2f);
                
                Callback.MoveUnit(unitId, centerPosition);
            }
        }

        public override void OnUnitDamaged(int unitId, int attackerId, float damage, Vector3 direction, int weaponDefId, bool paralyzer)
        {
            Console.WriteLine($"Unit {unitId} damaged by {attackerId}: {damage} damage");
            
            // Example: Counter-attack when damaged
            if (attackerId > 0 && Callback != null)
            {
                Callback.AttackUnit(unitId, attackerId);
            }
        }

        public override void OnUnitDestroyed(int unitId, int attackerId, int weaponDefId)
        {
            Console.WriteLine($"Unit {unitId} destroyed by {attackerId}");
            
            // Example: Send a message when we lose units
            if (Callback != null)
            {
                Callback.SendTextMessage($"Lost unit {unitId}!");
            }
        }

        public override void OnRelease(int reason)
        {
            Console.WriteLine($"AI shutting down. Reason: {reason}");
            base.OnRelease(reason);
        }
    }
}
