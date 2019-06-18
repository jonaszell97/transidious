using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

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

            /// The map is in bulldoze mode.
            BulldozeMode,
        }

        /// The current game status.
        public GameStatus status;

        /// The current game play mode.
        public MapEditorMode editorMode;

        /// <summary>
        ///  The currently loaded map.
        /// </summary>
        public Map loadedMap;
        public GameObject loadedMapObj;

        /// The map editor instance.
        public MapEditor mapEditor;

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

        // Only for debugging.
        public OSMImportHelper.Area areaToLoad;
        public bool reload = false;

        // *** Shaders ***
        public Shader circleShader;
        public Shader defaultShader;
        public Shader silhouetteDiffuseShader;

        // *** Materials ***
        public Material highlightedMaterial;
        public Dictionary<Tuple<Color, Color>, Material> highlightedMaterials;

        public Material unlitMaterial;
        public Dictionary<Color, Material> unlitMaterials;

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
        public Sprite streetArrowSprite;

        /// The citizien info UI.
        public GameObject citizienUI;
        public Image citizienUIHappinessSprite;
        public TMPro.TextMeshProUGUI citizienUINameText;
        public TMPro.TextMeshProUGUI citizienUIHappinessText;
        public TMPro.TextMeshProUGUI citizienUIMoneyText;
        public Image citizienUIDestinationImg;
        public TMPro.TextMeshProUGUI citizienUIDestinationText;
        public Sprite[] happinessSprites;

        public Material GetUnlitMaterial(Color c)
        {
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

        public void LoadMap(OSMImportHelper.Area area, bool reload = false)
        {
            Destroy(loadedMapObj);

            var mapObj = Instantiate(mapPrefab);
            this.loadedMapObj = mapObj;

            var map = mapObj.GetComponent<Map>();
            map.input = input;

            if (reload)
            {
                var importer = new OSMImporter(map, area);
                importer.ImportArea();
            }
            else
            {
                var importer = new MapLoader(map, area);
                importer.ImportArea();
            }

            this.loadedMap = map;
            this.status = GameStatus.Playing;
        }

        void RegisterUICallbacks()
        {
            this.bulldozeButton.onClick.AddListener(OnUIBulldozeButtonPressed);
            this.editButton.onClick.AddListener(OnUIEditButtonPressed);

            this.playPauseButton.onClick.AddListener(OnPlayPauseClick);
            this.simSpeedButton.onClick.AddListener(OnSimSpeedClick);
        }

        void Awake()
        {
            this.status = GameStatus.MainMenu;
            this.lang = new Translator("en_US");
            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);

            this.circleShader = Resources.Load("Shaders/CircleShader") as Shader;
            this.defaultShader = Shader.Find("Unlit/Color");
            this.silhouetteDiffuseShader = Resources.Load("Shaders/SilhouettedDiffuse") as Shader;

            this.highlightedMaterials = new Dictionary<Tuple<Color, Color>, Material>();
            this.unlitMaterials = new Dictionary<Color, Material>();

            this.highlightColor = new Color(96f / 255f, 208f / 255f, 230f / 255f);
            this.bulldozeHighlightColor = new Color(189f / 255f, 41f / 255f, 56f / 255f);

            this.citizienUI.SetActive(false);
            RegisterUICallbacks();
        }

        // Use this for initialization
        void Start()
        {
            LoadMap(areaToLoad, reload);
        }

        // Update is called once per frame
        void Update()
        {

        }

        // private int toolbarInt = 0;
        // private string[] toolbarStrings = { "Charlottenburg", "CharlottenburgWilmersdorf", "Saarbruecken", "Mitte", "Spandau", "Berlin" };

        //  void OnGUI()
        // {
        //     var previous = toolbarInt;
        //     toolbarInt = GUI.Toolbar(new Rect(25, 25, 500, 30), toolbarInt, toolbarStrings);
        //     reload = GUI.Toggle(new Rect(525, 25, 10, 10), reload, new GUIContent("reload"));

        //     if (previous == toolbarInt)
        //         return;

        //     var value = System.Enum.TryParse(toolbarStrings[toolbarInt], out OSMImportHelper.Area area);
        //     LoadMap(area, reload);
        // }

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

            this.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        void ExitEditMode()
        {
            ExitPause();

            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);
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
    }
}
