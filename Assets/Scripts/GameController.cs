using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Transidious
{
    public enum MapDisplayMode
    {
        Day,
        Night,
    }

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

            /// The map is being loaded.
            Loading,

            /// A map is being imported.
            ImportingMap,

            /// The game controller is disabled.
            Disabled,
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

        /// The current map display mode.
        public MapDisplayMode displayMode;

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

        /// Reference to the finance controller.
        public FinanceController financeController;

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

        /// Prefab for line renderers.
        public GameObject lineRendererPrefab;

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
        /// The main ui.
        public MainUI mainUI;

        /// The main loading screen.
        public UILoadingScreen loadingScreen;

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

        /// GameObject that renders the creation cursor sprite.
        public GameObject createCursorObj;

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
                                     DynamicMapObject attachedObject = null)
        {
            var obj = Instantiate(tooltipPrefab);
            var tooltip = obj.GetComponent<Tooltip>();
            tooltip.Initialize(this, text, backgroundColor, attachedObject);

            return tooltip;
        }

        public GameObject CreateCursorSprite
        {
            get
            {
                if (createCursorObj == null)
                {
                    createCursorObj = SpriteManager.CreateSprite(createStreetSprite);
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

        public bool ImportingMap
        {
            get
            {
                return status == GameStatus.ImportingMap;
            }
        }

        public bool Loading
        {
            get
            {
                return status == GameStatus.Loading;
            }
        }

        public System.Collections.IEnumerator LoadMap(OSMImportHelper.Area area)
        {
            this.status = GameStatus.Loading;
            loadingScreen.gameObject.SetActive(true);

            yield return SaveManager.LoadSave(this, area.ToString());
            
            this.status = GameStatus.Playing;
            loadingScreen.gameObject.SetActive(false);

            // #if UNITY_EDITOR
            //             UnityEditor.EditorApplication.ExitPlaymode();
            //             UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            //                 UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/" +
            //                 loadedMap.name + ".unity", true);
            //             UnityEditor.EditorApplication.EnterPlaymode();
            // #endif
        }

        void RegisterUICallbacks()
        {
            this.bulldozeButton.onClick.AddListener(OnUIBulldozeButtonPressed);
            this.editButton.onClick.AddListener(OnUIEditButtonPressed);
            this.transitEditorButton.onClick.AddListener(OnUITransitEditorButtonClick);
        }

        void Awake()
        {
            GameController._instance = this;

            if (status == GameStatus.Disabled || status == GameStatus.ImportingMap)
            {
                gameObject.SetActive(false);
                return;
            }

            this.lang = new Translator("en_US");
            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);

            this.circleShader = Resources.Load("Shaders/CircleShader") as Shader;
            this.defaultShader = Shader.Find("Unlit/Color");
            this.silhouetteDiffuseShader = Resources.Load("Shaders/SilhouettedDiffuse") as Shader;

            this.highlightedMaterials = new Dictionary<Tuple<Color, Color>, Material>();

            this.highlightColor = new Color(96f / 255f, 208f / 255f, 230f / 255f);
            this.bulldozeHighlightColor = new Color(189f / 255f, 41f / 255f, 56f / 255f);

            RegisterUICallbacks();
        }

        // Use this for initialization
        void Start()
        {
            input.DisableControls();

            this.snapController = new SnapController(this);

            if (areaToLoad != OSMImportHelper.Area.Default)
            {
                StartCoroutine(LoadMap(areaToLoad));
            }

            input.EnableControls();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetLanguage(string newLang)
        {
            this.lang = new Translator(newLang);
            EventManager.current.TriggerEvent("LanguageChange");
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

            var dist = input.renderingDistance;
            this.input.UpdateRenderingDistance();

            if (dist != input.renderingDistance)
            {
                loadedMap.UpdateScale();
                loadedMap.UpdateTextScale();
            }

            mainUI.playPauseButton.enabled = false;
            mainUI.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        void ExitEditMode()
        {
            ExitPause();

            this.editorMode = MapEditorMode.ViewMode;
            this.mapEditor.gameObject.SetActive(false);
            this.mapEditor.Deactivate();
            this.loadedMap.ResetBackgroundColor();

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

            mainUI.playPauseButton.enabled = true;
            mainUI.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
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

            mainUI.playPauseButton.enabled = false;
            mainUI.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
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

            mainUI.playPauseButton.enabled = true;
            mainUI.playPauseButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        public void EnterPause()
        {
            this.status = GameStatus.Paused;
            mainUI.playPauseButton.GetComponent<Image>().sprite = SpriteManager.instance.playSprite;
            mainUI.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
        }

        public void ExitPause()
        {
            if (Editing)
            {
                return;
            }

            this.status = GameStatus.Playing;
            mainUI.playPauseButton.GetComponent<Image>().sprite = SpriteManager.instance.pauseSprite;
            mainUI.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
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
    }
}
