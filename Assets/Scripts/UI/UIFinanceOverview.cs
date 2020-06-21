using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIFinanceOverview : MonoBehaviour
    {
        /// Reference to the finance controller.
        private FinanceController _financeController;

        /// The earnings text.
        public TMP_Text earnings;

        /// The expenses text.
        public TMP_Text expenses;

        /// The tax rate text fields.
        public TMP_Text[] taxRates;

        /// The tax rate increase buttons.
        public UIButton[] taxRateIncButtons;

        /// The tax rate decrease buttons.
        public UIButton[] taxRateDecButtons;
        
        /// The fare text fields.
        public TMP_Text[] defaultFares;

        /// The fare increase buttons.
        public UIButton[] fareIncButtons;

        /// The fare decrease buttons.
        public UIButton[] fareDecButtons;

        /// The loan panels.
        public UILoanPanel[] loanPanels;

        /// Initialize.
        public void Initialize()
        {
            _financeController = GameController.instance.financeController;

            for (var i = 0; i < (int) FinanceController.TaxationType._Last; ++i)
            {
                var n = i;
                taxRateIncButtons[i].button.onClick.AddListener(() =>
                {
                    const decimal step = .25m;
                    var currRate = _financeController.GetTaxRate((FinanceController.TaxationType) n);
                    var newRate = Math.Clamp(currRate + step, FinanceController.MinTaxRate, FinanceController.MaxTaxRate);
                    _financeController.SetTaxRate((FinanceController.TaxationType) n, newRate);
                    taxRates[n].text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MaxTaxRate))
                    {
                        taxRateIncButtons[n].Disable();
                    }
                    else
                    {
                        taxRateIncButtons[n].Enable();
                    }

                    taxRateDecButtons[n].Enable();
                });
                taxRateDecButtons[i].button.onClick.AddListener(() =>
                {
                    const decimal step = .25m;
                    var currRate = _financeController.GetTaxRate((FinanceController.TaxationType) n);
                    var newRate = Math.Clamp(currRate - step, FinanceController.MinTaxRate, FinanceController.MaxTaxRate);
                    _financeController.SetTaxRate((FinanceController.TaxationType) n, newRate);
                    taxRates[n].text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MinTaxRate))
                    {
                        taxRateDecButtons[n].Disable();
                    }
                    else
                    {
                        taxRateDecButtons[n].Enable();
                    }
                    
                    taxRateIncButtons[n].Enable();
                });
            }

            for (var i = 0; i <= (int) TransitType.Ferry; ++i)
            {
                var n = i;
                fareIncButtons[i].button.onClick.AddListener(() =>
                {
                    const decimal step = .25m;
                    var currRate = _financeController.GetDefaultFare((TransitType) n);
                    var newRate = Math.Clamp(currRate + step, FinanceController.MinTripFare, FinanceController.MaxTripFare);
                    _financeController.SetDefaultFare((TransitType) n, newRate);
                    defaultFares[n].text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MaxTripFare))
                    {
                        fareIncButtons[n].Disable();
                    }
                    else
                    {
                        fareIncButtons[n].Enable();
                    }
                    
                    fareDecButtons[n].Enable();
                });
                fareDecButtons[i].button.onClick.AddListener(() =>
                {
                    const decimal step = .25m;
                    var currRate = _financeController.GetDefaultFare((TransitType) n);
                    var newRate = Math.Clamp(currRate - step, FinanceController.MinTripFare, FinanceController.MaxTripFare);
                    _financeController.SetDefaultFare((TransitType) n, newRate);
                    defaultFares[n].text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MinTripFare))
                    {
                        fareDecButtons[n].Disable();
                    }
                    else
                    {
                        fareDecButtons[n].Enable();
                    }

                    fareIncButtons[n].Enable();
                });
            }

            for (var i = 0; i < _financeController.availableLoans.Length; ++i)
            {
                var n = i;
                var loan = _financeController.availableLoans[i];
                var panel = loanPanels[i];

                panel.amount.text = Translator.GetCurrency(loan.Amount, true, false, 0);
                panel.interest.text = $"{loan.Interest} %";
                panel.payments.text = loan.Payments.ToString();
                panel.rate.text = Translator.GetCurrency(loan.Rate, true, false, 0);

                panel.takeLoanButton.button.onClick.AddListener(() =>
                {
                    _financeController.TakeLoan(loan);
                    RefreshLoanPanel();
                });

                panel.paybackButton.button.onClick.AddListener(() =>
                {
                    _financeController.PaybackLoan(n);
                    RefreshLoanPanel();
                });
            }
        }

        /// Activate the finance overview.
        public void Activate()
        {
            earnings.text = Translator.GetCurrency(_financeController.earnings, true);
            expenses.text = Translator.GetCurrency(_financeController.expenses, true);

            for (var i = 0; i < (int) FinanceController.TaxationType._Last; ++i)
            {
                taxRates[i].text = Translator.GetCurrency(
                    _financeController.GetTaxRate((FinanceController.TaxationType) i), true);
            }

            for (var i = 0; i <= (int) TransitType.Ferry; ++i)
            {
                defaultFares[i].text = Translator.GetCurrency(
                    _financeController.GetDefaultFare((TransitType) i), true);
            }

            RefreshLoanPanel();
            gameObject.SetActive(true);
        }

        /// Deactivate the finance overview.
        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        /// Toggle the finance overview.
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }

        /// Update the finance panel.
        void Update()
        {
            if (GameController.instance.Paused)
            {
                return;
            }
            
            earnings.text = Translator.GetCurrency(_financeController.earnings, true);
            expenses.text = Translator.GetCurrency(_financeController.expenses, true);

            for (var i = 0; i < _financeController.availableLoans.Length; ++i)
            {
                var takenLoan = _financeController.activeLoans[i];
                var panel = loanPanels[i];

                if (takenLoan == null)
                {
                    continue;
                }

                panel.balance.text = Translator.GetCurrency(takenLoan.Balance, true);

                if (_financeController.Money >= takenLoan.Balance)
                {
                    panel.paybackButton.Enable();
                }
                else
                {
                    panel.paybackButton.Disable();
                }
            }
        }

        /// Refresh the loan panel.
        void RefreshLoanPanel()
        {
            for (var i = 0; i < _financeController.availableLoans.Length; ++i)
            {
                var takenLoan = _financeController.activeLoans[i];
                var panel = loanPanels[i];

                if (takenLoan == null)
                {
                    panel.takeLoanButton.gameObject.SetActive(true);
                    panel.balance.gameObject.SetActive(false);
                    panel.balanceLabel.gameObject.SetActive(false);
                    panel.paybackButton.gameObject.SetActive(false);
                }
                else
                {
                    panel.takeLoanButton.gameObject.SetActive(false);
                    panel.balance.gameObject.SetActive(true);
                    panel.balanceLabel.gameObject.SetActive(true);
                    panel.paybackButton.gameObject.SetActive(true);
                    panel.balance.text = Translator.GetCurrency(takenLoan.Balance, true);

                    if (_financeController.Money >= takenLoan.Balance)
                    {
                        panel.paybackButton.Enable();
                    }
                    else
                    {
                        panel.paybackButton.Disable();
                    }
                }
            }

            loanPanels[0].lockOverlay
                .SetActive(!GameController.instance.Progress.IsUnlocked(Progress.Unlockable.SmallLoan));
            loanPanels[1].lockOverlay
                .SetActive(!GameController.instance.Progress.IsUnlocked(Progress.Unlockable.MediumLoan));
            loanPanels[2].lockOverlay
                .SetActive(!GameController.instance.Progress.IsUnlocked(Progress.Unlockable.BigLoan));
        }
    }
}