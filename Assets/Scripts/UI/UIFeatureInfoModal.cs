using UnityEngine;
using Transidious.UI;

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

        /// The current visitor list.
        public UICitizenList visitorList;

#if DEBUG
        private UIInfoPanel debugPanel;
#endif

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            
            visitorList.Initialize("ui:building:visitors");
            
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
            
            debugPanel.AddClickableItem("Centroid", "Centroid", Color.white, () =>
            {
                Utility.DrawCircle(feature.Centroid, 2f, 2f, Color.red);
            });

            debugPanel.AddClickableItem("VisualCenter", "Visual Center", Color.white, () =>
            {
                Utility.DrawCircle(feature.VisualCenter, 2f, 2f, Color.blue);
            });

            debugPanel.AddClickableItem("CalcVisualCenter", "Calculate Visual Center", Color.white, () =>
            {
                var pslg = new PSLG();
                pslg.AddOrderedVertices(feature.outlinePositions[0]);

                for (var i = 1; i < feature.outlinePositions.Length; ++i)
                {
                    pslg.AddHole(feature.outlinePositions[i]);
                }

                var visualCenter = Polylabel.GetVisualCenter(pslg, 1f, true);
                Utility.DrawCircle(visualCenter, 2f, 2f, Color.green);
            });
            
            debugPanel.AddItem("Area", "ui:feature:area");
            debugPanel.AddItem("Triangles", "Triangles");
#endif
        }

        public void SetFeature(NaturalFeature feature)
        {
            this.feature = feature;
            this.modal.SetTitle(feature.name);

            var visitors = feature.GetOccupants(OccupancyKind.Visitor);
            this.panel.SetValue("Occupants", (visitors?.Count ?? 0) + " / " + feature.Capacity);
            this.panel.SetValue("Type", Translator.Get("ui:feature:type:" + feature.type.ToString()));

            if (visitors != null)
            {
                visitorList.gameObject.SetActive(true);
                visitorList.SetCitizens(visitors);
            }
            else
            {
                visitorList.gameObject.SetActive(false);
            }

#if DEBUG
            this.debugPanel.SetValue("Area", feature.area + " m²");
            this.debugPanel.SetValue("Triangles", (feature.mesh?.triangles.Length ?? 0).ToString());
#endif
        }
    }
}