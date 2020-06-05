using System;
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

        /// The occupancy lists.
        public UICitizenList[] occupancyLists;

#if DEBUG
        private UIInfoPanel debugPanel;
#endif

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            
            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                occupancyLists[i].Initialize($"ui:map_object:{(OccupancyKind)i}");
            }
            
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

            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                var k = (OccupancyKind) i;
                panel.AddItem(k.ToString(), $"ui:map_object:{k}s");
            }

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

            var defaultKind = feature.GetDefaultOccupancyKind();
            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                var k = (OccupancyKind) i;
                var occ = feature.GetOccupancyCount(k);

                var key = k.ToString();
                if (occ == 0 && k != defaultKind)
                {
                    panel.HideItem(key);
                    continue;
                }

                panel.ShowItem(key);
                panel.SetValue(key, $"{occ} / {feature.GetCapacity(k)}");
            }

            this.panel.SetValue("Type", Translator.Get("ui:feature:type:" + feature.type.ToString()));

            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                var kind = (OccupancyKind) i;
                var numOccupants = feature.GetOccupancyCount(kind);

                if (numOccupants == 0)
                {
                    occupancyLists[i].gameObject.SetActive(false);
                    continue;
                }
                
                occupancyLists[i].gameObject.SetActive(true);
                occupancyLists[i].SetCitizens(feature.GetOccupants(kind));
            }

#if DEBUG
            this.debugPanel.SetValue("Area", feature.area + " m²");
            this.debugPanel.SetValue("Triangles", (feature.mesh?.triangles.Length ?? 0).ToString());
#endif
        }
    }
}