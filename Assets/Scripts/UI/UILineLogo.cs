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

        /// The input text field.
        [SerializeField] TMP_InputField inputField;

        void Awake()
        {
            if (line != null)
            {
                SetLine(line);
            }
        }

        public void SetColor(Color c)
        {
            backgroundImage.color = c;
        }

        public void SetLine(Line line)
        {
            this.line = line;

            backgroundImage.color = line.color;
            inputField.text = line.name;

            switch (line.type)
            {
                case TransitType.Bus:
                case TransitType.LightRail:
                case TransitType.IntercityRail:
                    backgroundImage.sprite = GameController.instance.roundedRectSprite;
                    backgroundImage.type = UnityEngine.UI.Image.Type.Sliced;
                    break;
                default:
                    backgroundImage.sprite = GameController.instance.squareSprite;
                    backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
                    break;
            }
        }
    }
}