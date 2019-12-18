using UnityEngine;
using System;

namespace Transidious
{
    [Serializable]
    public struct Objective
    {
        public enum Type
        {
            /// <summary>
            /// Completed when reaching a certain amount of money. 
            /// </summary>
            TotalMoney = 0,

            /// <summary>
            /// Completed when reaching a certain amount of monthly income. 
            /// </summary>
            MonthlyIncome = 1,

            /// <summary>
            /// Completed when a certain amount of average citizien happiness is reached.
            /// </summary>
            AvgHappiness = 2,

            /// <summary>
            /// Completed when reaching a certain amount of total passengers.
            /// </summary>
            TotalPassengers = 3,

            /// <summary>
            /// Completed when reaching a certain percentage of saved trips.
            /// </summary>
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
            this.requirement = requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        public Objective(Type type, int requirement, DateTime? deadline = null)
        {
            this.type = type;
            this.requirement = (decimal)requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        public Objective(Type type, float requirement, DateTime? deadline = null)
        {
            this.type = type;
            this.requirement = (decimal)requirement;
            this.status = Status.InProgress;
            this.deadline = deadline;
        }

        /// <summary>
        /// The type of the objective.
        /// </summary>
        public Type type { get; }

        /// <summary>
        /// The status of the objective.
        /// </summary>
        public Status status { get; set; }

        /// <summary>
        /// The time by which the objective has to be completed.
        /// </summary>
        public DateTime? deadline;

        /// <summary>
        /// The value of the requirement; exact meaning is dependent on objective type.
        /// </summary>
        public decimal requirement;

        /// <summary>
        /// Whether or not the objective is completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return status == Status.Completed;
            }
        }

        /// <summary>
        /// Whether or not the objective can still be completed.
        /// </summary>
        public bool IsFailed
        {
            get
            {
                return status == Status.Failed;
            }
        }

        /// <summary>
        /// The progress of the objective, between 0 and 1.
        /// </summary>
        public float Progress
        {
            get
            {
                switch (type)
                {
                    case Type.TotalMoney:
                        return (float)(financeController.Money / requirement);
                    default:
                        Debug.LogError("unsupported requirement type");
                        return 0f;
                }
            }
        }

        SimulationController simulationController
        {
            get
            {
                return GameController.instance.sim;
            }
        }

        FinanceController financeController
        {
            get
            {
                return GameController.instance.financeController;
            }
        }

        /// <summary>
        /// Update the progress of the objective.
        /// </summary>
        /// <returns>True if the objective has failed.</returns>
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