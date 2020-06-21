using System;
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
        public class Loan
        {
            /// The name of the loan.
            public readonly string Name;

            /// The amount of money of the loan.
            public readonly decimal Amount;

            /// The interest rate of the loan (in %).
            public readonly int Interest;

            /// The amount of monthly payments.
            public readonly int Payments;
            
            /// The total amount to pay.
            public decimal PaybackAmount => Amount * (1m + (decimal) ((float) Interest / 100f));

            /// The rate per period.
            public decimal Rate => PaybackAmount / Payments;

            /// C'tor.
            public Loan(string name, decimal amount, int interest, int payments)
            {
                Name = name;
                Amount = amount;
                Interest = interest;
                Payments = payments;
            }
        }

        public class ActiveLoan
        {
            /// Reference to the loan prototype.
            public readonly Loan LoanPrototype;

            /// The balance left to pay.
            public decimal Balance;

            /// The balance to pay per tick.
            public readonly decimal PaymentPerTick;

            /// C'tor.
            public ActiveLoan(Loan loanPrototype)
            {
                LoanPrototype = loanPrototype;
                Balance = loanPrototype.PaybackAmount;
                PaymentPerTick = loanPrototype.Rate / 60m;
            }
        }

        ///  Reference to the game controller.
        public GameController game;

        ///  The players current money.
        [SerializeField] decimal money;

        public decimal Money
        {
            get => money;
            set
            {
                this.money = value;
                game.mainUI.UpdateFinances();
            }
        }

        /// The current hourly earnings.
        public decimal earnings;
        List<Earning> _earningItems;

        /// The current hourly expenses.
        public decimal expenses;
        List<Expense> _expenseItems;

        /// The available loans.
        public Loan[] availableLoans;

        /// The taken loans.
        public ActiveLoan[] activeLoans;

        /// The earning item for taxes.
        public Earning taxes;

        public enum TaxationType
        {
            /// Tax rate per citizen.
            Residential = 0,
            
            /// Tax rate for a shop.
            Shops,
            
            /// Tax rate for an office.
            Office,
            
            /// Tax rate for industrial buildings.
            Industrial,

            /// Tax rate for other buildings.
            Other,

            /// Marker.
            _Last,
        }

        /// The current tax rates.
        public decimal[] taxRates;

        /// The current default fares for transit lines.
        public decimal[] defaultFares;

        /// The maximum valid tax rate.
        public static readonly decimal MaxTaxRate = 50m;

        /// The minimum valid tax rate.
        public static readonly decimal MinTaxRate = 0m;

        /// The maximum valid trip fare.
        public static readonly decimal MaxTripFare = 30m;

        /// The minimum valid trip fare.
        public static readonly decimal MinTripFare = 0m;

#if UNITY_EDITOR
        public float startingMoney;
        public float startingEarnings;
        public float startingExpenses;
#endif

        public static readonly decimal MaxMoney = 999_999_999m;
        public static readonly decimal MaxIncome = 999_999_999m;

        /// The hourly income.
        public decimal Income => earnings - expenses;

        public decimal IncomePerTick => Income / 60m;

        void Awake()
        {
            _earningItems = new List<Earning>();
            _expenseItems = new List<Expense>();

            taxes = new Earning
            {
                descriptionKey = "ui:finances:taxes",
                amount = 0m,
            };

            _earningItems.Add(taxes);

            InitializeTaxRates();
            InitializeDefaultFares();

            availableLoans = new[]
            {
                new Loan("ui:finances:small_loan", 100_000m, 3, 100),
                new Loan("ui:finances:medium_loan", 500_000m, 5, 200),
                new Loan("ui:finances:big_loan", 1_000_000m, 7, 250),
            };

            activeLoans = new ActiveLoan[availableLoans.Length];
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
            GameController.instance.sim.ScheduleEvent(this.UpdateFinances);
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
            _earningItems.Add(new Earning
            {
                descriptionKey = descriptionKey,
                amount = amount,
            });
        }

        public void AddExpense(string descriptionKey, decimal amount)
        {
            _expenseItems.Add(new Expense
            {
                descriptionKey = descriptionKey,
                amount = amount,
            });
        }

        public void UpdateFinances()
        {
            UpdateEarnings();
            UpdateExpenses();
            UpdateLoans();

            this.money = System.Math.Min(this.money + this.IncomePerTick, MaxMoney);
            game.mainUI.UpdateFinances();
        }

        public void UpdateEarnings()
        {
            earnings = 0m;

            foreach (var item in _earningItems)
            {
                earnings += item.amount;
            }
        }

        public void UpdateExpenses()
        {
            expenses = 0m;

            foreach (var item in _expenseItems)
            {
                expenses += item.amount;
            }
        }

        void UpdateLoans()
        {
            for (var i = 0; i < activeLoans.Length; ++i)
            {
                var loan = activeLoans[i];
                if (loan == null)
                {
                    continue;
                }

                loan.Balance -= loan.PaymentPerTick;

                if (loan.Balance <= 0m)
                {
                    activeLoans[i] = null;
                    _expenseItems.RemoveAll(e => e.descriptionKey == loan.LoanPrototype.Name);
                }
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

            result.ExpenseItems.AddRange(_expenseItems.Select(e => e.ToProtobuf()));
            result.EarningItems.AddRange(_earningItems.Select(e => e.ToProtobuf()));

            return result;
        }

        void InitializeTaxRates()
        {
            taxRates = new[]
            {
                5m,  // Residential
                10m, // Shops
                15m, // Offices
                10m, // Industrial
                10m, // Other
            };
        }

        void InitializeDefaultFares()
        {
            defaultFares = new[]
            {
                Line.GetDefaultTripFare(TransitType.Bus),
                Line.GetDefaultTripFare(TransitType.Tram),
                Line.GetDefaultTripFare(TransitType.Subway),
                Line.GetDefaultTripFare(TransitType.IntercityRail),
                Line.GetDefaultTripFare(TransitType.Ferry),
            };
        }

        public decimal GetTaxRate(TaxationType type)
        {
            Debug.Assert(type != TaxationType._Last);
            return taxRates[(int) type];
        }
        
        public void SetTaxRate(TaxationType type, decimal rate)
        {
            Debug.Assert(type != TaxationType._Last && rate >= MinTaxRate && rate <= MaxTaxRate);
            taxRates[(int) type] = rate;
        }

        public decimal GetDefaultFare(TransitType type)
        {
            return defaultFares[(int) type];
        }
        
        public void SetDefaultFare(TransitType type, decimal rate)
        {
            Debug.Assert(rate >= MinTripFare && rate <= MaxTripFare);
            defaultFares[(int) type] = rate;
        }

        public void TakeLoan(Loan loan)
        {
            var idx = Array.IndexOf(availableLoans, loan);
            Debug.Assert(idx != -1 && activeLoans[idx] == null);

            activeLoans[idx] = new ActiveLoan(loan);
            AddExpense(loan.Name, loan.Rate);

            Earn(loan.Amount);
        }

        public void PaybackLoan(int n)
        {
            var activeLoan = activeLoans[n];
            Purchase(activeLoan.Balance);

            activeLoans[n] = null;
            _expenseItems.RemoveAll(e => e.descriptionKey == activeLoan.LoanPrototype.Name);
        }
    }
}