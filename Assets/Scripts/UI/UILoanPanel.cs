using TMPro;
using UnityEngine;

namespace Transidious.UI
{
    public class UILoanPanel : MonoBehaviour
    {
        /// The amount text field.
        public TMP_Text amount;
        
        /// The interest text field.
        public TMP_Text interest;
        
        /// The rate text field.
        public TMP_Text rate;
        
        /// The payments text field.
        public TMP_Text payments;

        /// The take loan button.
        public UIButton takeLoanButton;

        /// The lock overlay.
        public GameObject lockOverlay;

        /// The balance text field.
        public TMP_Text balance;
        
        /// The balance label text field.
        public TMP_Text balanceLabel;

        /// The payback button.
        public UIButton paybackButton;
    }
}