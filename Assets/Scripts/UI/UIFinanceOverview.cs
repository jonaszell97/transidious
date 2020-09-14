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

        /// The tax rate panels.
        public UITaxPanel[] taxRatePanels;
        
        /// The default fare panels.
        public UITaxPanel[] defaultFarePanels;

        /// The loan panels.
        public UILoanPanel[] loanPanels;

        /// Whether or not the game was paused when opening the finance overview.
        private bool _wasPaused;

        /// Initialize.
        public void Initialize()
        {
            _financeController = GameController.instance.financeController;

            const decimal step = .25m;
            for (var i = 0; i < (int) FinanceController.TaxationType._Last; ++i)
            {
                var n = i;
                taxRatePanels[i].plusButton.button.onClick.AddListener(() =>
                {
                    var currRate = _financeController.GetTaxRate((FinanceController.TaxationType) n);
                    var newRate = Math.Clamp(currRate + step, FinanceController.MinTaxRate, FinanceController.MaxTaxRate);
                    _financeController.SetTaxRate((FinanceController.TaxationType) n, newRate);
                    taxRatePanels[n].amount.text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MaxTaxRate))
                    {
                        taxRatePanels[n].plusButton.Disable();
                    }
                    else
                    {
                        taxRatePanels[n].plusButton.Enable();
                    }

                    taxRatePanels[n].minusButton.Enable();
                });
                taxRatePanels[i].minusButton.button.onClick.AddListener(() =>
                {
                    var currRate = _financeController.GetTaxRate((FinanceController.TaxationType) n);
                    var newRate = Math.Clamp(currRate - step, FinanceController.MinTaxRate, FinanceController.MaxTaxRate);
                    _financeController.SetTaxRate((FinanceController.TaxationType) n, newRate);
                    taxRatePanels[n].amount.text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MinTaxRate))
                    {
                        taxRatePanels[n].minusButton.Disable();
                    }
                    else
                    {
                        taxRatePanels[n].minusButton.Enable();
                    }
                    
                    taxRatePanels[n].plusButton.Enable();
                });
            }

            for (var i = 0; i <= (int) TransitType.Ferry; ++i)
            {
                var n = i;
                defaultFarePanels[i].plusButton.button.onClick.AddListener(() =>
                {
                    var currRate = _financeController.GetDefaultFare((TransitType) n);
                    var newRate = Math.Clamp(currRate + step, FinanceController.MinTripFare, FinanceController.MaxTripFare);
                    _financeController.SetDefaultFare((TransitType) n, newRate);
                    defaultFarePanels[n].amount.text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MaxTripFare))
                    {
                        defaultFarePanels[n].plusButton.Disable();
                    }
                    else
                    {
                        defaultFarePanels[n].plusButton.Enable();
                    }
                    
                    defaultFarePanels[n].minusButton.Enable();
                });
                defaultFarePanels[i].minusButton.button.onClick.AddListener(() =>
                {
                    var currRate = _financeController.GetDefaultFare((TransitType) n);
                    var newRate = Math.Clamp(currRate - step, FinanceController.MinTripFare, FinanceController.MaxTripFare);
                    _financeController.SetDefaultFare((TransitType) n, newRate);
                    defaultFarePanels[n].amount.text = Translator.GetCurrency(newRate, true);

                    if (newRate.Equals(FinanceController.MinTripFare))
                    {
                        defaultFarePanels[n].minusButton.Disable();
                    }
                    else
                    {
                        defaultFarePanels[n].minusButton.Enable();
                    }

                    defaultFarePanels[n].plusButton.Enable();
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
        void Activate()
        {
            earnings.text = Translator.GetCurrency(_financeController.earnings, true);
            expenses.text = Translator.GetCurrency(_financeController.expenses, true);

            for (var i = 0; i < (int) FinanceController.TaxationType._Last; ++i)
            {
                taxRatePanels[i].amount.text = Translator.GetCurrency(
                    _financeController.GetTaxRate((FinanceController.TaxationType) i), true);
            }

            for (var i = 0; i <= (int) TransitType.Ferry; ++i)
            {
                var type = (Progress.Unlockable) ((int) Progress.Unlockable.Bus + i);
                var panel = defaultFarePanels[i];
                
                if (GameController.instance.Progress.IsUnlocked(type))
                {
                    panel.amount.gameObject.SetActive(true);
                    panel.nameText.gameObject.SetActive(true);
                    panel.minusButton.gameObject.SetActive(true);
                    panel.plusButton.gameObject.SetActive(true);
                    panel.icon.gameObject.SetActive(true);
                    panel.lockSprite.gameObject.SetActive(false);
                    
                    panel.amount.text = Translator.GetCurrency(
                        _financeController.GetDefaultFare((TransitType) i), true);
                }
                else
                {
                    panel.amount.gameObject.SetActive(false);
                    panel.nameText.gameObject.SetActive(false);
                    panel.minusButton.gameObject.SetActive(false);
                    panel.plusButton.gameObject.SetActive(false);
                    panel.icon.gameObject.SetActive(false);
                    panel.lockSprite.gameObject.SetActive(true);   
                }
            }

            RefreshLoanPanel();
            gameObject.SetActive(true);
        }

        /// Deactivate the finance overview.
        void Deactivate()
        {
            gameObject.SetActive(false);
        }

        /// Toggle the finance overview.
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Deactivate();
                GameController.instance.mainUI.EnableUI();

                if (!_wasPaused)
                {
                    GameController.instance.ExitPause();
                }
            }
            else
            {
                _wasPaused = GameController.instance.EnterPause();
                GameController.instance.mainUI.DisableUI(true, false, true);
                Activate();
            }
        }

        /// Update the finance panel.
        /*void Update()
        {
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
        }*/

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