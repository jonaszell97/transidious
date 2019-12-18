using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIInstruction : MonoBehaviour
    {
        /// The singleton instance.
        public static UIInstruction instance;

        /// The text field.
        [SerializeField] UIText text;

        public static void Show(string key)
        {
            instance.text.SetKey(key);
            instance.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            instance.gameObject.SetActive(false);
        }
    }
}