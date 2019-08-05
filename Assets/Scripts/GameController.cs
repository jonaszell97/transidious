using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class GameController : MonoBehaviour
    {
        public enum GameStatus
        {
            /// <summary>
            ///  The game is currently in the main menu.
            /// </summary>
            MainMenu,

            /// <summary>
            ///  The game is currently in play mode.
            /// </summary>
            Playing,

            /// <summary>
            ///  The game is currently paused.
            /// </summary>
            Paused,
        }

        public enum MapEditorMode
        {
            /// The map is in view mode.
            ViewMode,

            /// The map is in edit mode.
            EditMode,

            TransitMode,

            /// The map is in bulldoze mode.
            BulldozeMode,
        }

        /// The current game status.
        public GameStatus status;

        /// The current game play mode.
        public MapEditorMode editorMode;
        public SnapController snapController;
        public SaveManager saveManager;

        /// <summary>
        ///  The currently loaded map.
        /// </summary>
        public Map loadedMap
        {
            get
            {
                return SaveManager.loadedMap;
            }
        }

        public GameObject loadedMapObj;

        /// The map editor instance.
        public MapEditor mapEditor;

        /// The transit controller instance.
        public TransitEditor transitEditor;

        /// The simulation controller instance.
        public SimulationController sim;

        /// <summary>
        ///  The input controller.
        /// </summary>
        public InputController input;

        /// The translator for the current language.
        public Translator lang;

        /// <summary>
        ///  Prefab for creating new maps.
        /// </summary>
        public GameObject mapPrefab;
        public GameObject tooltipPrefab;

        // Only for debugging.
        public OSMImportHelper.Area areaToLoad;

        // *** Shaders ***
        public Shader circleShader;
        public Shader defaultShader;
        public Shader silhouetteDiffuseShader;

        // *** Materials ***
        public Material highlightedMaterial;
        public Dictionary<Tuple<Color, Color>, Material> highlightedMaterials;

        public static Material unlitMaterial;
        public static Dictionary<Color, Material> unlitMaterials;

        public Color highlightColor;
        public Color bulldozeHighlightColor;

        // *** UI elements ***

        /// The UI canvas.
        public Canvas uiCanvas;

        /// <summary>
        /// The UI bulldoze button.
        /// </summary>
        public Button bulldozeButton;

        /// <summary>
        /// The bulldozer cursor texture.
        /// </summary>
        public Texture2D bulldozeCursorTex;

        /// <summary>
        /// The UI edit button.
        /// </summary>
        public Button editButton;

        public Button transitEditorButton;

        /// The street creation cursor sprite.
        public Sprite createStreetSprite;

        /// Prefab for creating sprites.
        public GameObject spritePrefab;

        /// GameObject that renders the creation cursor sprite.
        public GameObject createCursorObj;

        /// The play/pause button.
        public Button playPauseButton;

        /// Sprites for the play/pause button.
        public Sprite playSprite;
        public Sprite pauseSprite;
        public Sprite uiButtonSprite;

        /// The game time text.
        public UnityEngine.UI.Text gameTimeText;

        /// The simulation speed button.
        public Button simSpeedButton;

        /// The simulation speed sprites.
        public Sprite[] simSpeedSprites;

        /// The scale bar.
        public GameObject scaleBar;

        /// The scale text.
        public UnityEngine.UI.Text scaleText;

        /// The traffic light sprites.
        public Sprite[] trafficLightSprites;

        /// The car sprites.
        public Sprite[] carSprites;

        /// The street direction arrow sprite.
        public static Sprite _streetArrowSprite;
        public static Sprite streetArrowSprite
        {
            get
            {
                if (_streetArrowSprite == null)
                {
                    var tex = Resources.Load("Sprites/arrow") as Texture2D;
                    _streetArrowSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));

                }

                return _streetArrowSprite;
            }
        }

        public Sprite squareSprite;
        public Sprite roundedRectSprite;

        /// The citizien info UI.
        public GameObject citizienUI;
        public Image citizienUIHappinessSprite;
        public TMPro.TextMeshProUGUI citizienUINameText;
        public TMPro.TextMeshProUGUI citizienUIHappinessText;
        public TMPro.TextMeshProUGUI citizienUIMoneyText;
        public Image citizienUIDestinationImg;
        public TMPro.TextMeshProUGUI citizienUIDestinationText;
        public Sprite[] happinessSprites;

        static GameController _instance;
        public static GameController instance
        {
            get
            {
                return _instance;
            }
        }

        public static Material GetUnlitMaterial(Color c)
        {
            if (unlitMaterials == null)
            {
                unlitMaterials = new Dictionary<Color, Material>();
            }
            if (unlitMaterial == null)
            {
                unlitMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            if (!unlitMaterials.TryGetValue(c, out Material m))
            {
                m = new Material(unlitMaterial)
                {
                    color = c
                };

                unlitMaterials.Add(c, m);
            }

            return m;
        }

        public Material GetHighlightedMaterial(Color mainColor, Color outlineColor)
        {
            var tup = new Tuple<Color, Color>(outlineColor, mainColor);
            if (!highlightedMaterials.TryGetValue(tup, out Material m))
            {
                m = new Material(highlightedMaterial);
                m.SetColor("_OutlineColor", outlineColor);
                m.SetColor("_Color", mainColor);
                m.SetFloat("_Outline", 1f);

                highlightedMaterials.Add(tup, m);
            }

            return m;
        }

        public Tooltip CreateTooltip(Text text, Color backgroundColor,
                                     MapObject attachedObject = null)
        {
            var obj = Instantiate(tooltipPrefab);
            var tooltip = obj.GetComponent<Tooltip>();
            tooltip.Initialize(this, text, backgroundColor, attachedObject);

            return tooltip;
        }

        public GameObject CreateSprite(Sprite s)
        {
            var obj = Instantiate(spritePrefab);
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = s;

            return obj;
        }

        public GameObject CreateCursorSprite
        {
            get
            {
                if (createCursorObj == null)
                {
                    createCursorObj = CreateSprite(createStreetSprite);
                }

                return createCursorObj;
            }
        }

        public bool Bulldozing
        {
            get
            {
                return editorMode == MapEditorMode.BulldozeMode;
            }
        }
        public bool Editing
        {
            get
            {
                return editorMode == MapEditorMode.EditMode;
            }
        }

        public bool Viewing
        {
            get
            {
                return editorMode == MapEditorMode.ViewMode;
            }
        }

        public bool Paused
        {
            get
            {
                return status == GameStatus.Paused;
            }
        }

        public void LoadMap(OSMImportHelper.Area area)
        {
            SaveManager.LoadSave(this, area.ToString());
            this.status = GameStatus.Playing;
        }

        void RegisterUICallbacks()
        {
            this.bulldozeButton.onClick.AddListener(OnUIBulldozeButtonPressed);
            this.editButton.onClick.AddListener(OnUIEditButtonPressed);
            this.transitEditorButton.onClick.AddListener(OnUITransitEditorButtonClick);

            this.playPauseButton.onClick.AddListener(OnPlayPauseClick);
            this.simSpeedButton.onClick.AddListener(OnSimSpeedClick);
        }

        void Awake()
        {
            GameController._instance = this;

            this.status = GameStatus.MainMenu;
            this.lang = new Translator("en_US");
            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);

            this.circleShader = Resources.Load("Shaders/CircleShader") as Shader;
            this.defaultShader = Shader.Find("Unlit/Color");
            this.silhouetteDiffuseShader = Resources.Load("Shaders/SilhouettedDiffuse") as Shader;

            this.highlightedMaterials = new Dictionary<Tuple<Color, Color>, Material>();

            this.highlightColor = new Color(96f / 255f, 208f / 255f, 230f / 255f);
            this.bulldozeHighlightColor = new Color(189f / 255f, 41f / 255f, 56f / 255f);

            this.citizienUI.SetActive(false);
            RegisterUICallbacks();
        }

        // Use this for initialization
        void Start()
        {
            LoadMap(areaToLoad);
            this.snapController = new SnapController(this);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Color GetDefaultSystemColor(TransitType system)
        {
            return Map.defaultLineColors[system];
        }

        public string GetSystemName(TransitType system)
        {
            switch (system)
            {
                default:
                case TransitType.Bus: return Translator.Get("transit:bus");
                case TransitType.Tram: return Translator.Get("transit:tram");
                case TransitType.Subway: return Translator.Get("transit:subway");
                case TransitType.LightRail: return Translator.Get("transit:lightrail");
                case TransitType.IntercityRail: return Translator.Get("transit:intercity");
                case TransitType.Ferry: return Translator.Get("transit:ferry");
            }
        }

        void EnterViewMode()
        {
            switch (editorMode)
            {
                case MapEditorMode.BulldozeMode:
                    ExitBulldozeMode();
                    break;
                case MapEditorMode.EditMode:
                    ExitEditMode();
                    break;
                default:
                    break;
            }

            this.editorMode = MapEditorMode.ViewMode;
        }

        void OnUIBulldozeButtonPressed()
        {
            if (editorMode == MapEditorMode.BulldozeMode)
            {
                EnterViewMode();
            }
            else
            {
                EnterViewMode();
                EnterBulldozeMode();
            }
        }

        void EnterBulldozeMode()
        {
            var halfSize = bulldozeCursorTex.width * .5f;
            Cursor.SetCursor(bulldozeCursorTex, new Vector2(halfSize, halfSize), CursorMode.Auto);

            this.editorMode = MapEditorMode.BulldozeMode;
        }

        void ExitBulldozeMode()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            this.editorMode = MapEditorMode.ViewMode;
        }

        void OnUIEditButtonPressed()
        {
            if (editorMode == MapEditorMode.EditMode)
            {
                EnterViewMode();
            }
            else
            {
                EnterViewMode();
                EnterEditMode();
            }
        }

        void EnterEditMode()
        {
            if (!Paused)
                EnterPause();

            this.editorMode = MapEditorMode.EditMode;
            this.mapEditor.gameObject.SetActive(true);
            this.mapEditor.Activate();
            this.loadedMap.SetBackgroundColor(new Color(148f / 255f, 213f / 255f, 255f / 255f));
            this.loadedMap.buildingMesh.gameObject.SetActive(false);
            this.loadedMap.natureMesh.gameObject.SetActive(false);

            var dist = input.renderingDistance;
            this.input.UpdateRenderingDistance();

            if (dist != input.renderingDistance)
            {
                loadedMap.UpdateScale();
                loadedMap.UpdateTextScale();
            }

            this.playPauseButton.enabled = false;
            this.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        void ExitEditMode()
        {
            ExitPause();

            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);
            this.mapEditor.Deactivate();
            this.loadedMap.ResetBackgroundColor();
            this.loadedMap.buildingMesh.gameObject.SetActive(true);
            this.loadedMap.natureMesh.gameObject.SetActive(true);

            var dist = input.renderingDistance;
            this.input.UpdateRenderingDistance();

            if (dist != input.renderingDistance)
            {
                loadedMap.UpdateScale();
                loadedMap.UpdateTextScale();
            }

            if (createCursorObj != null)
            {
                createCursorObj.SetActive(false);
            }

            this.playPauseButton.enabled = true;
            this.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        void EnterTransitMode()
        {
            if (!Paused)
                EnterPause();

            this.editorMode = MapEditorMode.TransitMode;
            this.transitEditor.Activate();

            var dist = input.renderingDistance;
            this.input.UpdateRenderingDistance();

            if (dist != input.renderingDistance)
            {
                loadedMap.UpdateScale();
                loadedMap.UpdateTextScale();
            }

            this.playPauseButton.enabled = false;
            this.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        void ExitTransitMode()
        {
            ExitPause();

            this.editorMode = MapEditorMode.ViewMode;
            this.transitEditor.Deactivate();

            var dist = input.renderingDistance;
            this.input.UpdateRenderingDistance();

            if (dist != input.renderingDistance)
            {
                loadedMap.UpdateScale();
                loadedMap.UpdateTextScale();
            }

            this.playPauseButton.enabled = true;
            this.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        void OnPlayPauseClick()
        {
            if (status == GameStatus.Paused)
            {
                ExitPause();
            }
            else
            {
                EnterPause();
            }
        }

        void EnterPause()
        {
            this.status = GameStatus.Paused;
            this.playPauseButton.GetComponent<Image>().sprite = playSprite;
            this.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        void ExitPause()
        {
            if (Editing)
            {
                return;
            }

            this.status = GameStatus.Playing;
            this.playPauseButton.GetComponent<Image>().sprite = pauseSprite;
            this.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        void OnUITransitEditorButtonClick()
        {
            switch (this.editorMode)
            {
                case MapEditorMode.ViewMode:
                    EnterTransitMode();
                    break;
                case MapEditorMode.TransitMode:
                    ExitTransitMode();
                    break;
                default:
                    break;
            }
        }

        void OnSimSpeedClick()
        {
            if (Paused)
            {
                ExitPause();
            }

            var newSimSpeed = (sim.simulationSpeed + 1) % 3;
            sim.simulationSpeed = newSimSpeed;

            this.simSpeedButton.GetComponent<Image>().sprite = simSpeedSprites[newSimSpeed];
        }

        public void MouseDownStreetSegment(StreetSegment seg)
        {
            if (input.IsPointerOverUIElement())
            {
                return;
            }

            if (transitEditor.active)
            {

            }
            else if (Bulldozing)
            {
                seg.DeleteSegment();
            }
            else if (seg.highlighted)
            {
                seg.highlighted = false;
                seg.ResetBorderColor(input.renderingDistance);
            }
            else if (Viewing)
            {
                seg.highlighted = true;
                seg.HighlightBorder(input.controller.Bulldozing);
            }

#if DEBUG
            if (sim.trafficSim.manualTrafficLightControl)
            {
                seg.startTrafficLight?.Switch();
                seg.endTrafficLight?.Switch();
            }
#endif
        }

        static readonly float endSnapThreshold = 5f * Map.Meters;

        Vector3 GetClosestPointToCursor(StreetSegment seg)
        {
            var cursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var minDist = float.PositiveInfinity;
            var minPt = Vector3.zero;

            for (int i = 1; i < seg.positions.Count; ++i)
            {
                var p0 = seg.positions[i - 1];
                var p1 = seg.positions[i];

                var closestPt = Math.NearestPointOnLine(p0, p1, cursorPos);
                var sqrDist = (closestPt - cursorPos).sqrMagnitude;

                if (sqrDist < minDist)
                {
                    minDist = sqrDist;
                    minPt = closestPt;
                }
            }

            var distanceFromStart = (seg.positions.First() - minPt).magnitude;
            if (distanceFromStart < endSnapThreshold)
            {
                return seg.positions.First();
            }

            var distanceFromEnd = (seg.positions.Last() - minPt).magnitude;
            if (distanceFromEnd < endSnapThreshold)
            {
                return seg.positions.Last();
            }

            return minPt;
        }

        void SnapMouseToPath(StreetSegment seg)
        {
            Cursor.visible = false;

            var closestPt = GetClosestPointToCursor(seg);
            var cursorObj = CreateCursorSprite;
            cursorObj.SetActive(true);

            cursorObj.transform.position = new Vector3(closestPt.x, closestPt.y, Map.Layer(MapLayer.Cursor));
            mapEditor.hoveredStreetSegment = seg;
        }

        public void MouseOverStreetSegment(StreetSegment seg)
        {
            switch (editorMode)
            {
                case GameController.MapEditorMode.BulldozeMode:
                    seg.UpdateBorderColor(bulldozeHighlightColor);
                    break;
                case GameController.MapEditorMode.ViewMode:
                    seg.UpdateBorderColor(highlightColor);
                    break;
                case GameController.MapEditorMode.EditMode:
                    SnapMouseToPath(seg);
                    break;
            }
        }

        public void MouseExitStreetSegment(StreetSegment seg)
        {
            if (!seg.highlighted && seg.outlineMeshObj != null)
            {
                seg.ResetBorderColor(input.renderingDistance);
            }

            Cursor.visible = true;

            if (Editing)
            {
                var cursorObj = CreateCursorSprite;
                cursorObj.SetActive(false);

                mapEditor.hoveredStreetSegment = null;
            }
        }
    }
}
