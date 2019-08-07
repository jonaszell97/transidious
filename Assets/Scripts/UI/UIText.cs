using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIText : MonoBehaviour
    {
        /// The text mesh.
        public TMP_Text textMesh;

        /// The localization key of the displayed text.
        public string localizationKey;

        void Start()
        {
            EventManager.current.RegisterEventListener(this);
            textMesh.text = Translator.Get(localizationKey);
        }

        void OnLanguageChange()
        {
            textMesh.text = Translator.Get(localizationKey);
        }
    }
}