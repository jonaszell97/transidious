using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UITooltip : MonoBehaviour
    {
        /// <summary>
        /// The singleton tooltip instance (there can only ever be one tooltip at once).
        /// </summary>
        public static UITooltip instance;

        /// <summary>
        /// The text object.
        /// </summary>
        [SerializeField] TMP_Text text;

        public static void ShowScreen(string message, Vector2 screenPosition)
        {
            instance.text.text = message;
            instance.GetComponent<RectTransform>().anchoredPosition = screenPosition;
            instance.gameObject.SetActive(true);
        }

        public static void Show(string text, Vector2 worldPosition)
        {
            ShowScreen(text, Camera.main.WorldToScreenPoint(worldPosition));
        }

        public static void Hide()
        {
            instance.gameObject.SetActive(false);
        }
    }
}