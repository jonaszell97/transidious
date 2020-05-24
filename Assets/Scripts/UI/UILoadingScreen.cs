using TMPro;
using UnityEngine;

namespace Transidious
{
    public class UILoadingScreen : MonoBehaviour
    {
        /// The current loading stage text.
        [SerializeField] private TMP_Text loadingScreenText;

        /// Set the current loading screen text.
        public void SetText(string key)
        {
            loadingScreenText.text = Translator.Get(key);
        }
    }
}