using UnityEngine;
using System;

namespace Transidious
{
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
}