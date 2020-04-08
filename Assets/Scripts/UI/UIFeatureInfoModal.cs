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

#if DEBUG
        private UIInfoPanel debugPanel;
#endif

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            
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

            panel.AddItem("Type", "ui:feature:type");
            panel.AddItem("Occupants", "ui:building:visitors");

#if DEBUG
            debugPanel = Instantiate(ResourceManager.instance.infoPanelCardPrefab, panel.transform.parent)
                .GetComponent<UIInfoPanel>();
            
            debugPanel.Initialize();
            
            debugPanel.AddItem("Area", "ui:feature:area");
            debugPanel.AddItem("Triangles", "Triangles");
#endif
        }

        public void SetFeature(NaturalFeature feature)
        {
            this.feature = feature;
            this.modal.SetTitle(feature.name);

            this.panel.SetValue("Occupants", feature.Visitors + " / " + feature.Capacity);
            this.panel.SetValue("Type", Translator.Get("ui:feature:type:" + feature.type.ToString()));

#if DEBUG
            this.debugPanel.SetValue("Area", feature.area.ToString() + " m²");
            this.debugPanel.SetValue("Triangles", (feature.mesh?.triangles.Length ?? 0).ToString());
#endif
        }
    }
}