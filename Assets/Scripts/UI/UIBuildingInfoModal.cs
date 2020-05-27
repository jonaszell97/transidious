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
            OccupancyKind occupancyKind;

            switch (building.type)
            {
            case Building.Type.Residential:
                occupantsKey = "ui:building:residents";
                occupancyKind = OccupancyKind.Resident;
                break;
            case Building.Type.Shop:
            case Building.Type.GroceryStore:
            case Building.Type.Hospital:
            case Building.Type.Office:
            case Building.Type.Industrial:
                occupantsKey = "ui:building:workers";
                occupancyKind = OccupancyKind.Worker;
                break;
            case Building.Type.ElementarySchool:
            case Building.Type.HighSchool:
            case Building.Type.University:
                occupantsKey = "ui:building:students";
                occupancyKind = OccupancyKind.Student;
                break;
            default:
                occupantsKey = "ui:building:visitors";
                occupancyKind = OccupancyKind.Visitor;
                break;
            }

            var occupantsItem = panel.GetItem("Occupants");
            occupantsItem.Key.SetKey(occupantsKey);
            occupantsItem.Value.text =
                $"{building.GetOccupancyCount(occupancyKind)} / {building.GetCapacity(occupancyKind)}";

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