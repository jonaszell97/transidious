using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UILineLogo : MonoBehaviour
    {
        /// The transit line this logo is for.
        public Line line;

        /// The background image.
        [SerializeField] Image backgroundImage;

        /// The placeholder text.
        [SerializeField] TMP_Text placeholderText;

        /// The input text field.
        [SerializeField] TMP_InputField inputField;
        
        /// The button component.
        [SerializeField] private Button link;

        void Awake()
        {
            link.onClick.AddListener(() =>
            {
                line.ActivateModal();
            });
        }

        public void SetColor(Color c)
        {
            backgroundImage.color = c;

            var textColor = Math.ContrastColor(c);
            placeholderText.color = textColor;

            if (inputField != null)
            {
                inputField.textComponent.color = textColor;
            }
        }

        string GetTruncatedLineName(Line line)
        {
            return line.name.Length > 4 ? line.name.Substring(0, 4).Trim() : line.name;
        }

        public void SetLine(Line line, bool truncateName = true, bool allowLink = true)
        {
            this.line = line;
            SetColor(line.color);

            if (truncateName)
            {
                if (inputField != null)
                {
                    inputField.text = GetTruncatedLineName(line);
                    placeholderText.text = inputField.text;
                }
                else
                {
                    placeholderText.text = GetTruncatedLineName(line);
                }
            }
            else if (inputField != null)
            {
                inputField.text = line.name;
                placeholderText.text = line.name;
            }
            else
            {
                placeholderText.text = line.name;
            }

            link.enabled = allowLink;

            switch (line.type)
            {
            case TransitType.Bus:
            case TransitType.LightRail:
            case TransitType.IntercityRail:
                backgroundImage.sprite = SpriteManager.instance.roundedRectSprite;
                backgroundImage.type = UnityEngine.UI.Image.Type.Sliced;
                break;
            default:
                backgroundImage.sprite = SpriteManager.instance.squareSprite;
                backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
                break;
            }
        }
    }
}