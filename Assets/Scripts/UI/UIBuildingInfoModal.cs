using UnityEngine;
using Transidious.UI;

namespace Transidious
{
    public class UIBuildingInfoModal : MonoBehaviour
    {
        /// The building currently displayed by the modal.
        public Building building;

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
                    modal.titleInput.text = building.name;
                    return;
                }

                building.name = newName;
            });

            modal.onClose.AddListener(() =>
            {
                this.building = null;
            });

            panel.AddItem("Type", "ui:building:type");

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
                Utility.DrawCircle(building.Centroid, 2f, 2f, Color.red);
            });

            debugPanel.AddClickableItem("VisualCenter", "Visual Center", Color.white, () =>
            {
                Utility.DrawCircle(building.VisualCenter, 2f, 2f, Color.blue);
            });

            debugPanel.AddClickableItem("CalcVisualCenter", "Calculate Visual Center", Color.white, () =>
            {
                var pslg = new PSLG();
                pslg.AddOrderedVertices(building.outlinePositions[0]);

                for (var i = 1; i < building.outlinePositions.Length; ++i)
                {
                    pslg.AddHole(building.outlinePositions[i]);
                }

                var visualCenter = Polylabel.GetVisualCenter(pslg, 1f, true);
                Utility.DrawCircle(visualCenter, 2f, 2f, Color.green);
            });
            
            debugPanel.AddItem("Area", "ui:building:area");
            debugPanel.AddItem("Triangles", "Triangles");
            debugPanel.AddItem("Vertices", "Vertices");
#endif
        }

        public void SetBuilding(Building building)
        {
            this.building = building;
            this.modal.SetTitle(building.name);

            var defaultKind = building.GetDefaultOccupancyKind();
            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                var k = (OccupancyKind) i;
                var occ = building.GetOccupancyCount(k);

                var key = k.ToString();
                if (occ == 0 && k != defaultKind)
                {
                    panel.HideItem(key);
                    continue;
                }

                panel.ShowItem(key);
                panel.SetValue(key, $"{occ} / {building.GetCapacity(k)}");
            }

            this.panel.SetValue("Type", Translator.Get("ui:building:type:" + building.type.ToString()));

            for (var i = 0; i < (int) OccupancyKind._Last; ++i)
            {
                var kind = (OccupancyKind) i;
                var numOccupants = building.GetOccupancyCount(kind);

                if (numOccupants == 0)
                {
                    occupancyLists[i].gameObject.SetActive(false);
                    continue;
                }
                
                occupancyLists[i].gameObject.SetActive(true);
                occupancyLists[i].SetCitizens(building.GetOccupants(kind));
            }

#if DEBUG
            this.debugPanel.SetValue("Area", $"{building.area} m²");
            this.debugPanel.SetValue("Triangles", (building.mesh?.triangles.Length ?? 0).ToString());
            this.debugPanel.SetValue("Vertices", (building.mesh?.vertexCount ?? 0).ToString());
#endif
        }
    }
}