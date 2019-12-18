using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIFeatureInfoModal : MonoBehaviour
    {
        /// The feature currently displayed by the modal.
        public NaturalFeature feature;

        /// Reference to the modal component.
        public UIModal modal;

        /// The info panel.
        public UIInfoPanel panel;

        void Start()
        {
            var maxCharacters = 100;
            modal.titleInput.interactable = true;

            modal.titleInput.onValidateInput = (string text, int charIndex, char addedChar) =>
            {
                if (text.Length + 1 >= maxCharacters)
                {
                    return '\0';
                }

                return addedChar;
            };

            modal.titleInput.onSubmit.AddListener((string newName) =>
            {
                if (newName.Length == 0 || newName.Length > maxCharacters)
                {
                    modal.titleInput.text = feature.name;
                    return;
                }

                feature.name = newName;
            });

            modal.onClose.AddListener(() =>
            {
                this.feature = null;
            });

#if DEBUG
            panel.AddItem("Area", "ui:feature:area");
            panel.AddItem("Triangles", "Triangles");
#endif
        }

        public void SetFeature(NaturalFeature feature)
        {
            this.feature = feature;
            this.modal.SetTitle(feature.name);

            this.panel.SetValue("Occupants", feature.visitors + " / " + feature.capacity);
            this.panel.SetValue("Type", Translator.Get("ui:feature:type:" + feature.type.ToString()));

#if DEBUG
            this.panel.SetValue("Area", feature.area.ToString() + " m²");
            this.panel.SetValue("Triangles", (feature.mesh?.triangles.Length ?? 0).ToString());
#endif
        }
    }
}