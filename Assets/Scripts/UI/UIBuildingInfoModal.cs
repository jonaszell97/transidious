using UnityEngine;
using TMPro;

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
                    modal.titleInput.text = building.name;
                    return;
                }

                building.name = newName;
            });

            modal.onClose.AddListener(() =>
            {
                this.building = null;
            });

#if DEBUG
            panel.AddItem("Area", "ui:building:area");
            panel.AddItem("Triangles", "Triangles");
            panel.AddItem("Vertices", "Vertices");

            panel.AddClickableItem("Delete", "Delete", Color.red, () =>
            {
                this.building.DeleteMesh();
            });

            panel.AddClickableItem("Simplify", "Simplify", Color.green, () =>
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

            var occupantsItem = panel.GetItem("Occupants");
            occupantsItem.Item1.SetKey(occupantsKey);
            occupantsItem.Item2.text = building.NumInhabitants + " / " + building.capacity;

            this.panel.SetValue("Type", Translator.Get("ui:building:type:" + building.type.ToString()));

#if DEBUG
            this.panel.SetValue("Area", building.area.ToString() + " m²");
            this.panel.SetValue("Triangles", (building.mesh?.triangles.Length ?? 0).ToString());
            this.panel.SetValue("Vertices", (building.mesh?.vertexCount ?? 0).ToString());
#endif
        }
    }
}