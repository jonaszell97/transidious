using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIIcon : MonoBehaviour
    {
        /// Reference to the image.
        public Image image;

        /// Reference to the button.
        public Button button;

        /// Enable the icon.
        public void Enable()
        {
            button.enabled = true;

            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, 1f);
        }

        /// Disable the icon.
        public void Disable()
        {
            button.enabled = false;

            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, .6f);
        }
    }
}