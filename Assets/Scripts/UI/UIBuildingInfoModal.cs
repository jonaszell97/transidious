using System.Collections.Generic;
using UnityEngine;
using TMPro;
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

        /// The resident list.
        public UICitizenList residentList;

        /// The current visitor list.
        public UICitizenList visitorList;

#if DEBUG
        private UIInfoPanel debugPanel;
#endif

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();

            residentList.Initialize("ui:building:residents");
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
            panel.AddItem("Occupants", "ui:building:occupants");

#if DEBUG
            debugPanel = Instantiate(ResourceManager.instance.infoPanelCardPrefab, panel.transform.parent)
                .GetComponent<UIInfoPanel>();
            
            debugPanel.Initialize();
            
            debugPanel.AddItem("Area", "ui:building:area");
            debugPanel.AddItem("Triangles", "Triangles");
            debugPanel.AddItem("Vertices", "Vertices");

            debugPanel.AddClickableItem("Delete", "Delete", Color.red, () =>
            {
                this.building.DeleteMesh();
            });

            debugPanel.AddClickableItem("Simplify", "Simplify", Color.green, () =>
            {
                this.building.DeleteMesh();

                var simplifier = new UnityMeshSimplifier.MeshSimplifier(this.building.mesh);
                simplifier.SimplifyMesh(.8f);

                var newMesh = Instantiate(GameController.instance.loadedMap.meshPrefab);
                newMesh.GetComponent<MeshFilter>().sharedMesh = simplifier.ToMesh();
                newMesh.GetComponent<MeshRenderer>().material = GameController.instance.GetUnlitMaterial(this.building.GetColor());
            });
#endif
        }

        public void SetBuilding(Building building)
        {
            this.building = building;
            this.modal.SetTitle(building.name);

            string occupantsKey;
            switch (building.type)
            {
            case Building.Type.Residential:
            default:
                occupantsKey = "ui:building:residents";
                break;
            case Building.Type.Shop:
            case Building.Type.GroceryStore:
            case Building.Type.Hospital:
                occupantsKey = "ui:building:workers";
                break;
            case Building.Type.ElementarySchool:
            case Building.Type.HighSchool:
            case Building.Type.University:
                occupantsKey = "ui:building:students";
                break;
            case Building.Type.Stadium:
                occupantsKey = "ui:building:visitors";
                break;
            }

            residentList.title.SetKey(occupantsKey);

            var occupantsItem = panel.GetItem("Occupants");
            occupantsItem.Item3.SetKey(occupantsKey);
            occupantsItem.Item4.text = $"{building.ResidentCount} / {building.Capacity}";

            this.panel.SetValue("Type", Translator.Get("ui:building:type:" + building.type.ToString()));

            var residents = building.Residents;
            var visitors = building.Visitors;

            if (residents == null && visitors == null)
            {
                residentList.gameObject.SetActive(false);
                visitorList.gameObject.SetActive(false);
            }
            else
            {
                if (residents != null)
                {
                    residentList.gameObject.SetActive(true);
                    residentList.SetCitizens(residents);
                }
                else if (visitors != null)
                {
                    visitorList.gameObject.SetActive(true);
                    visitorList.SetCitizens(visitors);
                }
            }


#if DEBUG
            this.debugPanel.SetValue("Area", building.area.ToString() + " m²");
            this.debugPanel.SetValue("Triangles", (building.mesh?.triangles.Length ?? 0).ToString());
            this.debugPanel.SetValue("Vertices", (building.mesh?.vertexCount ?? 0).ToString());
#endif
        }
    }
}