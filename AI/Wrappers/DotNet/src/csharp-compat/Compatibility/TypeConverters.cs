/// Type converters between F# core types and C# compatibility types
using SpringAI.Core;
using SpringAI.CSharp.Commands;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace SpringAI.CSharp.Compatibility
{
    /// <summary>
    /// Converts between F# discriminated unions and C# class hierarchies
    /// This enables C# consumers to work with familiar OOP patterns while
    /// benefiting from F#'s superior type system internally
    /// </summary>
    public static class TypeConverters
    {
        /// <summary>
        /// Convert F# UnitInfo to C# UnitInfo
        /// </summary>
        public static CSharpUnitInfo ToCSUnitInfo(UnitInfo fsharpUnit)
        {
            return new CSharpUnitInfo
            {
                Id = fsharpUnit.Id,
                DefId = fsharpUnit.DefId,
                DefName = fsharpUnit.DefName,
                Position = fsharpUnit.Position,
                Health = fsharpUnit.Health,
                MaxHealth = fsharpUnit.MaxHealth,
                Faction = fsharpUnit.Faction,
                Categories = ListModule.ToArray(fsharpUnit.Categories).ToList(),
                IsAlive = fsharpUnit.IsAlive
            };
        }

        /// <summary>
        /// Convert C# UnitInfo to F# UnitInfo
        /// </summary>
        public static UnitInfo ToFSUnitInfo(CSharpUnitInfo csharpUnit)
        {
            return new UnitInfo
            {
                Id = csharpUnit.Id,
                DefId = csharpUnit.DefId,
                DefName = csharpUnit.DefName,
                Position = csharpUnit.Position,
                Health = csharpUnit.Health,
                MaxHealth = csharpUnit.MaxHealth,
                Faction = csharpUnit.Faction,
                Categories = ListModule.OfSeq(csharpUnit.Categories),
                IsAlive = csharpUnit.IsAlive
            };
        }

        /// <summary>
        /// Convert C# command to F# Command discriminated union
        /// </summary>
        public static Command ToFSCommand(CSharpCommand csharpCommand)
        {
            return csharpCommand switch
            {
                BuildCommand build => Command.NewBuild(build.BuilderId, build.UnitDefName, build.Position),
                MoveCommand move => Command.NewMove(move.UnitId, move.Destination),
                AttackCommand attack => Command.NewAttack(attack.AttackerId, attack.TargetId),
                StopCommand stop => Command.NewStop(stop.UnitId),
                GuardCommand guard => Command.NewGuard(guard.UnitId, guard.TargetId),
                PatrolCommand patrol => Command.NewPatrol(patrol.UnitId, ListModule.OfSeq(patrol.Positions)),
                _ => throw new ArgumentException($"Unknown C# command type: {csharpCommand.GetType().Name}")
            };
        }

        /// <summary>
        /// Convert F# Command to C# command
        /// </summary>
        public static CSharpCommand ToCSCommand(Command fsharpCommand)
        {
            if (fsharpCommand.IsBuild)
            {
                var build = fsharpCommand as Command.Build;
                return new BuildCommand(build!.builderId, build.unitDefName, build.position);
            }
            else if (fsharpCommand.IsMove)
            {
                var move = fsharpCommand as Command.Move;
                return new MoveCommand(move!.unitId, move.destination);
            }
            else if (fsharpCommand.IsAttack)
            {
                var attack = fsharpCommand as Command.Attack;
                return new AttackCommand(attack!.attackerId, attack.targetId);
            }
            else if (fsharpCommand.IsStop)
            {
                var stop = fsharpCommand as Command.Stop;
                return new StopCommand(stop!.unitId);
            }
            else if (fsharpCommand.IsGuard)
            {
                var guard = fsharpCommand as Command.Guard;
                return new GuardCommand(guard!.unitId, guard.targetId);
            }
            else if (fsharpCommand.IsPatrol)
            {
                var patrol = fsharpCommand as Command.Patrol;
                var positions = ListModule.ToArray(patrol!.positions);
                return new PatrolCommand(patrol.unitId, positions);
            }
            else
            {
                throw new ArgumentException($"Unknown F# command type: {fsharpCommand.GetType().Name}");
            }
        }

        /// <summary>
        /// Convert F# ResourceState to C# ResourceInfo
        /// </summary>
        public static CSharpResourceInfo ToCSResourceInfo(ResourceState fsharpResources)
        {
            return new CSharpResourceInfo
            {
                Metal = fsharpResources.Metal,
                Energy = fsharpResources.Energy,
                MetalIncome = fsharpResources.MetalIncome,
                EnergyIncome = fsharpResources.EnergyIncome,
                CurrentFrame = fsharpResources.CurrentFrame
            };
        }

        /// <summary>
        /// Convert C# ResourceInfo to F# ResourceState
        /// </summary>
        public static ResourceState ToFSResourceState(CSharpResourceInfo csharpResources)
        {
            return new ResourceState
            {
                Metal = csharpResources.Metal,
                Energy = csharpResources.Energy,
                MetalIncome = csharpResources.MetalIncome,
                EnergyIncome = csharpResources.EnergyIncome,
                CurrentFrame = csharpResources.CurrentFrame
            };
        }

        /// <summary>
        /// Convert F# Option to C# nullable
        /// </summary>
        public static T? ToNullable<T>(FSharpOption<T> option) where T : class
        {
            return FSharpOption<T>.get_IsNone(option) ? null : option.Value;
        }

        /// <summary>
        /// Convert C# nullable to F# Option
        /// </summary>
        public static FSharpOption<T> ToOption<T>(T? nullable) where T : class
        {
            return nullable == null ? FSharpOption<T>.None : FSharpOption<T>.Some(nullable);
        }

        /// <summary>
        /// Convert F# list to C# list
        /// </summary>
        public static List<T> ToList<T>(FSharpList<T> fsharpList)
        {
            return ListModule.ToArray(fsharpList).ToList();
        }

        /// <summary>
        /// Convert C# list to F# list
        /// </summary>
        public static FSharpList<T> ToFSharpList<T>(IEnumerable<T> csharpList)
        {
            return ListModule.OfSeq(csharpList);
        }

        /// <summary>
        /// Convert F# Result to C# Result-like structure
        /// </summary>
        public static CSharpResult<TSuccess, TError> ToCSResult<TSuccess, TError>(FSharpResult<TSuccess, TError> fsharpResult)
        {
            return fsharpResult.IsOk
                ? CSharpResult<TSuccess, TError>.Success(fsharpResult.ResultValue)
                : CSharpResult<TSuccess, TError>.Failure(fsharpResult.ErrorValue);
        }

        /// <summary>
        /// Convert F# Strategy to C# enum
        /// </summary>
        public static CSharpStrategy ToCSStrategy(Strategy fsharpStrategy)
        {
            if (fsharpStrategy.IsEconomicExpansion) return CSharpStrategy.EconomicExpansion;
            if (fsharpStrategy.IsMilitaryBuildup) return CSharpStrategy.MilitaryBuildup;
            if (fsharpStrategy.IsTechAdvancement) return CSharpStrategy.TechAdvancement;
            if (fsharpStrategy.IsDefensivePosition) return CSharpStrategy.DefensivePosition;
            if (fsharpStrategy.IsAttackMode) return CSharpStrategy.AttackMode;
            
            return CSharpStrategy.Unknown;
        }
    }

    /// <summary>
    /// C# Result-like structure for interop with F# Result
    /// </summary>
    public class CSharpResult<TSuccess, TError>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public TSuccess? SuccessValue { get; }
        public TError? ErrorValue { get; }

        private CSharpResult(bool isSuccess, TSuccess? successValue, TError? errorValue)
        {
            IsSuccess = isSuccess;
            SuccessValue = successValue;
            ErrorValue = errorValue;
        }

        public static CSharpResult<TSuccess, TError> Success(TSuccess value) =>
            new(true, value, default);

        public static CSharpResult<TSuccess, TError> Failure(TError error) =>
            new(false, default, error);

        public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onFailure) =>
            IsSuccess ? onSuccess(SuccessValue!) : onFailure(ErrorValue!);
    }

    /// <summary>
    /// C# equivalent of F# Strategy discriminated union
    /// </summary>
    public enum CSharpStrategy
    {
        Unknown = 0,
        EconomicExpansion = 1,
        MilitaryBuildup = 2,
        TechAdvancement = 3,
        DefensivePosition = 4,
        AttackMode = 5
    }
}
