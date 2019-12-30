using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious
{
    [System.Serializable]
    public class Expense
    {
        public string descriptionKey;
        public decimal amount;

        public Serialization.Expense ToProtobuf()
        {
            return new Serialization.Expense
            {
                Description = descriptionKey,
                Amount = amount.ToProtobuf(),
            };
        }

        public static Expense Deserialize(Serialization.Expense v)
        {
            return new Expense
            {
                descriptionKey = v.Description,
                amount = v.Amount.Deserialize(),
            };
        }
    }

    [System.Serializable]
    public class Earning
    {
        public string descriptionKey;
        public decimal amount;

        public Serialization.Earning ToProtobuf()
        {
            return new Serialization.Earning
            {
                Description = descriptionKey,
                Amount = amount.ToProtobuf(),
            };
        }

        public static Earning Deserialize(Serialization.Earning v)
        {
            return new Earning
            {
                descriptionKey = v.Description,
                amount = v.Amount.Deserialize(),
            };
        }
    }

    public class FinanceController : MonoBehaviour
    {
        /// <summary>
        ///  Reference to the game controller.
        /// </summary>
        public GameController game;

        /// <summary>
        ///  The players current money.
        /// </summary>
        [SerializeField]
        decimal money;

        public decimal Money
        {
            get
            {
                return money;
            }
            set
            {
                this.money = value;
                UpdateFinances();
            }
        }

        /// <summary>
        /// The current hourly earnings.
        /// </summary>
        public decimal earnings;
        List<Earning> earningItems;

        /// <summary>
        /// The current hourly expenses.
        /// </summary>
        public decimal expenses;
        List<Expense> expenseItems;

        /// <summary>
        /// The earning item for taxes.
        /// </summary>
        public Earning taxes;

#if UNITY_EDITOR
        public float startingMoney;
        public float startingEarnings;
        public float startingExpenses;
#endif

        public static readonly decimal MaxMoney = 999_999_999m;
        public static readonly decimal MaxIncome = 999_999_999m;

        /// <summary>
        /// The hourly income.
        /// </summary>
        public decimal Income
        {
            get
            {
                return earnings - expenses;
            }
        }

        public decimal IncomePerTick
        {
            get
            {
                return Income / 60m;
            }
        }

        void Awake()
        {
            earningItems = new List<Earning>();
            expenseItems = new List<Expense>();

            taxes = new Earning
            {
                descriptionKey = "ui:finances:taxes",
                amount = 0m,
            };

            earningItems.Add(taxes);
        }

        void Start()
        {
#if UNITY_EDITOR
            if (money == default)
            {
                money = (decimal)startingMoney;
                expenses = (decimal)startingExpenses;
                earnings = (decimal)startingEarnings;
            }
#endif

            game.mainUI.UpdateFinances();
            GameController.instance.sim.ScheduleEvent(() => this.UpdateFinances());
        }

        public void Earn(decimal amount)
        {
            this.money += amount;
            game.mainUI.UpdateFinances();
        }

        public void Purchase(decimal price)
        {
            Debug.Assert(this.money > price, "not enough funds!");
            this.money -= price;

            game.mainUI.UpdateFinances();
        }

        public void AddEarning(string descriptionKey, decimal amount)
        {
            earningItems.Add(new Earning
            {
                descriptionKey = descriptionKey,
                amount = amount,
            });
        }

        public void AddExpense(string descriptionKey, decimal amount)
        {
            expenseItems.Add(new Expense
            {
                descriptionKey = descriptionKey,
                amount = amount,
            });
        }

        public void UpdateFinances()
        {
            UpdateEarnings();
            UpdateExpenses();
            
            this.money = System.Math.Min(this.money + this.IncomePerTick, MaxMoney);
            game.mainUI.UpdateFinances();
        }

        public void UpdateEarnings()
        {
            earnings = 0m;

            foreach (var item in earningItems)
            {
                earnings += item.amount;
            }
        }

        public void UpdateExpenses()
        {
            expenses = 0m;

            foreach (var item in expenseItems)
            {
                expenses += item.amount;
            }
        }

        public Serialization.Finances ToProtobuf()
        {
            var result = new Serialization.Finances
            {
                Money = money.ToProtobuf(),
                Earnings = earnings.ToProtobuf(),
                Expenses = expenses.ToProtobuf(),
                Taxes = taxes.ToProtobuf(),
            };

            result.ExpenseItems.AddRange(expenseItems.Select(e => e.ToProtobuf()));
            result.EarningItems.AddRange(earningItems.Select(e => e.ToProtobuf()));

            return result;
        }
    }
}