using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UITaxPanel : MonoBehaviour
    {
        /// The icon.
        public Image icon;

        /// The name text.
        public UIText nameText;

        /// The amount text field.
        public TMP_Text amount;

        /// The minus button.
        public UIButton minusButton;

        /// The plus button.
        public UIButton plusButton;

        /// The lock sprite object.
        public Image lockSprite;
    }
}