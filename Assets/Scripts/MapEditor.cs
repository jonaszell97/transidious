using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class MapEditor : MonoBehaviour
    {
        /// The currently selected editing mode.
        public enum EditingMode
        {
            /// Editing mode used for creating streets.
            StreetCreation,
        }

        /// The game controller.
        public GameController game;

        /// The snap settings for map editing.
        int? snapSettingsId;

        /// The map editing mode.
        EditingMode mode;

        /// The world position of the previous mouse click.
        Vector3 prevClickPosition;

        /// Set to true if an odd number of mouse clicks has occurred.
        bool clickedOnce = false;

        /// The type of street being built.
        Street.Type streetType = Street.Type.Residential;

        /// The currently hovered street segment.
        public StreetSegment hoveredStreetSegment;

        /// The currently selected street segment.
        public StreetSegment selectedStreetSegment;

        /// Object that displays the location of the first click.
        GameObject firstClickMarker;

        /// Mesh used to display the street currently being built.
        GameObject inProgressStreetObj;
        GameObject inProgressStreetObjBorder;

        /// The last mouse position we set the in-progress mesh to.
        Vector3 lastUsedMousePos;

        public void Activate()
        {
            if (this.snapSettingsId == null)
            {
                this.snapSettingsId = game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    new Color(0f, .62f, .8f), // #009fcc8c
                    new Vector3(.7f, .7f, .7f),
                    true,
                    false,
                    false
                );
            }
            else
            {
                game.snapController.EnableSnap(this.snapSettingsId.Value);
            }
        }

        public void Deactivate()
        {
            game.snapController.DisableSnap(this.snapSettingsId.Value);
        }

        void Awake()
        {
            mode = EditingMode.StreetCreation;
        }

        void Start()
        {
            game.input.RegisterEventListener(InputEvent.MouseDown, (MapObject obj) =>
            {
                if (game.editorMode != GameController.MapEditorMode.BulldozeMode)
                {
                    return;
                }

                var s = obj as StreetSegment;
                if (s != null)
                {
                    s.DeleteSegment();
                }
            });
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !game.input.IsPointerOverUIElement())
            {
                if (clickedOnce)
                {
                    HandleClickedTwice();
                    clickedOnce = false;
                }
                else
                {
                    prevClickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    clickedOnce = true;

                    HandleClickedOnce();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // Reset click status.
                clickedOnce = false;
                ResetFirstClickMarker();
            }

            if (clickedOnce)
            {
                Vector3 endPos;
                if (hoveredStreetSegment != null)
                {
                    endPos = game.createCursorObj.transform.position;
                }
                else
                {
                    endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                }

                endPos.z = 0f;

                if (inProgressStreetObj == null)
                {
                    inProgressStreetObj = Instantiate(game.loadedMap.meshPrefab);
                    inProgressStreetObj.transform.SetParent(this.transform);
                    inProgressStreetObj.transform.position =
                        new Vector3(0f, 0f, Map.Layer(MapLayer.Streets));

                    var meshRenderer = inProgressStreetObj.GetComponent<MeshRenderer>();
                    meshRenderer.material = GameController.GetUnlitMaterial(
                        StreetSegment.GetStreetColor(streetType, game.input.renderingDistance));

                    inProgressStreetObjBorder = Instantiate(game.loadedMap.meshPrefab);
                    inProgressStreetObjBorder.transform.SetParent(this.transform);
                    inProgressStreetObjBorder.transform.position =
                        new Vector3(0f, 0f, Map.Layer(MapLayer.StreetOutlines));

                    var borderMeshRenderer = inProgressStreetObjBorder.GetComponent<MeshRenderer>();
                    borderMeshRenderer.material = GameController.GetUnlitMaterial(
                        StreetSegment.GetBorderColor(streetType, game.input.renderingDistance));
                }

                inProgressStreetObj.SetActive(true);
                inProgressStreetObjBorder.SetActive(true);

                var startPos = firstClickMarker.transform.position;
                startPos.z = 0f;

                var streetWidth = StreetSegment.GetStreetWidth(
                    streetType, 2, game.input.renderingDistance);

                var mesh = MeshBuilder.CreateSmoothLine(
                    new List<Vector3> { startPos, endPos },
                    streetWidth, 10, 0f);

                var meshFilter = inProgressStreetObj.GetComponent<MeshFilter>();
                meshFilter.mesh = mesh;

                var borderMesh = MeshBuilder.CreateSmoothLine(
                    new List<Vector3> { startPos, endPos },
                    streetWidth + StreetSegment.GetBorderWidth(streetType,
                        game.input.renderingDistance), 10, 0f);

                var borderMeshFilter = inProgressStreetObjBorder.GetComponent<MeshFilter>();
                borderMeshFilter.mesh = borderMesh;

                lastUsedMousePos = endPos;
            }
        }

        void SetFirstClickMarker(Vector3 pos)
        {
            if (firstClickMarker == null)
            {
                firstClickMarker = game.CreateSprite(game.createStreetSprite);
            }

            firstClickMarker.SetActive(true);
            firstClickMarker.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.Cursor));
        }

        void ResetFirstClickMarker()
        {
            if (firstClickMarker != null)
            {
                firstClickMarker.SetActive(false);
            }

            if (inProgressStreetObj != null)
            {
                inProgressStreetObj.SetActive(false);
                inProgressStreetObjBorder.SetActive(false);
            }
        }

        void HandleClickedOnce()
        {
            switch (mode)
            {
                case EditingMode.StreetCreation:
                    // The user clicked while being snapped to an existing street segment.
                    if (hoveredStreetSegment != null)
                    {
                        selectedStreetSegment = hoveredStreetSegment;
                        SetFirstClickMarker(game.createCursorObj.transform.position);
                    }
                    else
                    {
                        SetFirstClickMarker(prevClickPosition);
                    }

                    break;
            }
        }

        void HandleClickedTwice()
        {
            var startPos = firstClickMarker.transform.position;
            startPos.z = 0f;

            StreetIntersection startIntersection = null;
            if (selectedStreetSegment)
            {
                // Attach to start intersection.
                if (selectedStreetSegment.startIntersection?.position.Equals(startPos) ?? false)
                {
                    startIntersection = selectedStreetSegment.startIntersection;
                }
                // Attach to end intersection.
                else if (selectedStreetSegment.endIntersection?.position.Equals(startPos) ?? false)
                {
                    startIntersection = selectedStreetSegment.endIntersection;
                }
            }

            // Create new intersection.
            if (startIntersection == null)
            {
                startIntersection = game.loadedMap.CreateIntersection(startPos);

                if (selectedStreetSegment != null)
                {
                    selectedStreetSegment.street.SplitSegment(
                        selectedStreetSegment, startIntersection);
                }
            }

            Vector3 endPos;
            if (hoveredStreetSegment)
            {
                endPos = game.createCursorObj.transform.position;
            }
            else
            {
                endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            endPos.z = 0f;

            StreetIntersection endIntersection = null;
            if (hoveredStreetSegment)
            {
                // Attach to start intersection.
                if (hoveredStreetSegment.startIntersection?.position.Equals(endPos) ?? false)
                {
                    endIntersection = hoveredStreetSegment.startIntersection;
                }
                // Attach to end intersection.
                else if (hoveredStreetSegment.endIntersection?.position.Equals(endPos) ?? false)
                {
                    endIntersection = hoveredStreetSegment.endIntersection;
                }
            }

            // Create new intersection.
            if (endIntersection == null)
            {
                endIntersection = game.loadedMap.CreateIntersection(endPos);

                if (hoveredStreetSegment != null)
                {
                    hoveredStreetSegment.street.SplitSegment(
                        hoveredStreetSegment, endIntersection);
                }
            }

            var street = game.loadedMap.CreateStreet("<new street>", streetType,
                                                     true, false, 50, 2);

            street.AddSegment(new List<Vector3> { startPos, endPos }, startIntersection, endIntersection);
            street.CreateTextMeshes();
            street.segments.First().UpdateScale(game.input.renderingDistance);
            street.segments.First().UpdateTextScale(game.input.renderingDistance);

            ResetFirstClickMarker();
            selectedStreetSegment = null;
        }
    }
}