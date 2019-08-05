using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIResizableInput : UIInput
    {
        [SerializeField] TMP_InputField inputField;
        [SerializeField] TMP_Text text;

        protected override void Awake()
        {
            base.Awake(inputField);

            inputField.SetTextWithoutNotify(text.text);
            inputField.pointSize = text.fontSize;
            inputField.textComponent.characterSpacing = text.characterSpacing;
            inputField.textComponent.margin = text.margin;
            inputField.textComponent.alignment = text.alignment & ~(TextAlignmentOptions.Left)
                & ~(TextAlignmentOptions.Right) & ~(TextAlignmentOptions.Justified)
                & ~(TextAlignmentOptions.Flush) | TextAlignmentOptions.Center;

            inputField.onValueChanged.AddListener((string value) => {
                text.text = value;
            });
        }
    }
}