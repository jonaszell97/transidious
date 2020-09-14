using UnityEngine;
using System;

namespace Transidious
{
    /// Types of update hooks that are available for tasks.
    public enum ObjectiveUpdateHook
    {
        /// Update after every tick of game time.
        EveryTick,

        /// Update after every minute of game time.
        EveryMinute,

        /// Update after the start of a new day.
        EveryDay,

        /// Update after every monetary change.
        MonetaryChange,

        /// Update after line change.
        TransitChange,
    }

    [Serializable]
    public struct Objective
    {
        public enum Type
        {
            /// Completed when reaching a certain amount of money.
            TotalMoney = 0,

            /// Completed when reaching a certain amount of monthly income.
            MonthlyIncome = 1,

            /// Completed when a certain amount of average citizen happiness is reached.
            AvgHappiness = 2,

            /// Completed when reaching a certain amount of total passengers.
            TotalPassengers = 3,

            /// Completed when reaching a certain percentage of saved trips.
            TripsSaved = 4,
        }

        public enum Status
        {
            InProgress,
            Completed,
            Failed,
        }

        public Objective(Type type, Decimal requirement, DateTime? deadline = null)
        {
            this.type = type;
            this.requirement = (float)requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        public Objective(Type type, int requirement, DateTime? deadline = null)
        {
            this.type = type;
            this.requirement = requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        public Objective(Type type, float requirement, DateTime? deadline = null)
        {
            this.type = type;
            this.requirement = requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        /// The type of the objective.
        public Type type;

        /// The status of the objective.
        public Status status;

        /// The time by which the objective has to be completed.
        public DateTime? deadline;

        /// The value of the requirement; exact meaning is dependent on objective type.
        public float requirement;

        /// Whether or not the objective is completed.
        public bool IsCompleted => status == Status.Completed;

        /// Whether or not the objective can still be completed.
        public bool IsFailed => status == Status.Failed;

        /// The progress of the objective, between 0 and 1.
        public float Progress
        {
            get
            {
                switch (type)
                {
                    case Type.TotalMoney:
                        return ((float)financeController.Money / requirement);
                    default:
                        Debug.LogError("unsupported requirement type");
                        return 0f;
                }
            }
        }

        SimulationController simulationController => GameController.instance.sim;

        private FinanceController financeController => GameController.instance.financeController;

        /// Update the progress of the objective.
        public bool UpdateProgress()
        {
            if (status != Status.InProgress)
            {
                return false;
            }

            if (deadline.HasValue && simulationController.GameTime >= deadline.Value)
            {
                status = Status.Failed;
                return true;
            }

            if (Progress >= 1.0f)
            {
                status = Status.Completed;
                return false;
            }

            return false;
        }
    }

    public abstract class Task
    {
        /// The type of task.
        public enum TaskType
        {
            /// Connect a part of the city to the transit system.
            ConnectToTransit,
            
            /// Connect a part of the city with another part.
            CreateConnection,
            
            /// Build a specified number of stops for a specific system.
            BuildStops,
            
            /// Build a new line of a specific system.
            BuildLine,
            
            /// Have a certain amount of money in the bank.
            TotalMoney,
            
            /// Maintain a specified average happiness level.
            MaintainAvgHappiness,
        }

        /// The task type.
        public TaskType Type;
        
        /// Start time of the task.
        public DateTime StartTime;

        /// The task's deadline.
        public DateTime Deadline;

        /// The reward for this task.
        public decimal Reward;

        /// The update hooks of this objective.
        public ObjectiveUpdateHook[] UpdateHooks;

        /// Subclass C'tor.
        protected Task(TaskType type, DateTime deadline, decimal reward, ObjectiveUpdateHook[] updateHooks)
        {
            Type = type;
            Deadline = deadline;
            Reward = reward;
            UpdateHooks = updateHooks;

            StartTime = GameController.instance.sim.GameTime;
        }

        /// Estimate the task progress in %.
        public abstract float EstimateProgress(GameController game);

        /// Check whether or not the task has failed (if it can fail at all).
        public virtual bool IsFailed(GameController game)
        {
            return false;
        }
    }
    
    public class ConnectToTransitTask : Task
    {
        /// The map object that should be connected.
        public IMapObject MapObject;

        /// C'tor.
        public ConnectToTransitTask(IMapObject obj, DateTime deadline, decimal reward)
            : base(TaskType.ConnectToTransit, deadline, reward, new [] {ObjectiveUpdateHook.TransitChange})
        {
            MapObject = obj;
        }

        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            return 0f;
        }
    }

    public class CreateConnectionTask : Task
    {
        /// The map object from which a connection should be created.
        public IMapObject OriginMapObject;

        /// The map object to which a connection should be created.
        public IMapObject DestinationMapObject;

        /// C'tor.
        public CreateConnectionTask(IMapObject origin, IMapObject destination, DateTime deadline, decimal reward)
            : base(TaskType.CreateConnection, deadline, reward, new [] {ObjectiveUpdateHook.TransitChange})
        {
            OriginMapObject = origin;
            DestinationMapObject = destination;
        }
        
        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            return 0f;
        }
    }

    public class BuildStopsTask : Task
    {
        /// The transit system to build the stops for.
        public TransitType System;
        
        /// The number of stops to build.
        public int NumStops;

        /// C'tor.
        public BuildStopsTask(TransitType system, int numStops, DateTime deadline, decimal reward)
            : base(TaskType.BuildStops, deadline, reward, new[] {ObjectiveUpdateHook.TransitChange})
        {
            System = system;
            NumStops = numStops;
        }
        
        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            return 0f;
        }
    }

    public class BuildLineTask : Task
    {
        /// The transit system to build the line of.
        public TransitType System;

        /// C'tor.
        public BuildLineTask(TransitType system, DateTime deadline, decimal reward)
            : base(TaskType.BuildLine, deadline, reward, new[] {ObjectiveUpdateHook.TransitChange})
        {
            System = system;
        }

        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            return 0f;
        }
    }
    
    public class TotalMoneyTask : Task
    {
        /// The amount of money to save.
        public decimal Amount;

        /// C'tor.
        public TotalMoneyTask(decimal amount, DateTime deadline, decimal reward)
            : base(TaskType.TotalMoney, deadline, reward, new[] {ObjectiveUpdateHook.MonetaryChange})
        {
            Amount = amount;
        }

        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            return Mathf.Clamp((float)(game.financeController.Money / Amount), 0f, 1f);
        }
    }

    public abstract class TimedTask : Task
    {
        /// Subclass C'tor.
        protected TimedTask(TaskType type, DateTime deadline, decimal reward, ObjectiveUpdateHook[] updateHooks)
            : base(type, deadline, reward, updateHooks)
        {
        }

        /// Estimate the task progress in %.
        public override float EstimateProgress(GameController game)
        {
            var gt = game.sim.GameTime;
            if (gt >= Deadline)
            {
                return 1f;
            }

            var totalInterval = Deadline - StartTime;
            var passedInterval = game.sim.GameTime - StartTime;

            return Mathf.Clamp((float) (passedInterval.TotalSeconds / totalInterval.TotalSeconds), 0f, 1f);
        }
    }
    
    public class MaintainAvgHappinessTask : TimedTask
    {
        /// The happiness level to maintain.
        public float HappinessLevel;

        /// C'tor.
        public MaintainAvgHappinessTask(float happinessLevel, DateTime deadline, decimal reward)
            : base(TaskType.TotalMoney, deadline, reward, new[] {ObjectiveUpdateHook.EveryMinute})
        {
            HappinessLevel = happinessLevel;
        }

        /// Check whether or not the task has failed (if it can fail at all).
        public override bool IsFailed(GameController game)
        {
            return false;
        }
    }
}