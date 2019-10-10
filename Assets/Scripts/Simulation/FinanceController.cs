using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious
{

    public class FinanceController : MonoBehaviour
    {
        /// <summary>
        ///  Reference to the game controller.
        /// </summary>
        public GameController game;

        /// <summary>
        ///  The players current money.
        /// </summary>
        public decimal money;

        /// <summary>
        /// The current daily earnings.
        /// </summary>
        public decimal earnings;

        /// <summary>
        /// The current daily expenses.
        /// </summary>
        public decimal expenses;

#if UNITY_EDITOR
        public float startingMoney;
        public float startingEarnings;
        public float startingExpenses;
#endif

        public static readonly decimal MaxMoney = 999_999_999m;
        public static readonly decimal MaxIncome = 999_999_999m;

        /// <summary>
        /// The daily income.
        /// </summary>
        public decimal Income
        {
            get
            {
                return earnings - expenses;
            }
        }

        void Start()
        {
#if UNITY_EDITOR
            money = (decimal)startingMoney;
            expenses = (decimal)startingExpenses;
            earnings = (decimal)startingEarnings;
#endif

            game.mainUI.UpdateFinances();
            GameController.instance.sim.ScheduleEvent(() => this.UpdateFinances());
        }

        void Update()
        {

        }

        public void UpdateFinances()
        {
            this.money = System.Math.Min(this.money + this.Income, MaxMoney);
            game.mainUI.UpdateFinances();
        }
    }
}