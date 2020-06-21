using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIButton : MonoBehaviour
    {
        /// The button component.
        public Button button;

        /// The text component.
        public UIText text;

        /// The image component.
        public Image image;

        /// Enable the button.
        public void Enable()
        {
            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, 1f);
            button.enabled = true;

            if (text != null)
            {
                text.textMesh.color = Color.white;
            }
        }

        /// Disable the button.
        public void Disable()
        {
            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, .4f);
            button.enabled = false;

            if (text != null)
            {
                text.textMesh.color = new Color(.7f, .7f, .7f);
            }
        }
    }
}