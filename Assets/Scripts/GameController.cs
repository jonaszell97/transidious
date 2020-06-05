using UnityEngine;
using UnityEngine.Events;
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
            ///  The game is currently in the main menu.
            MainMenu,

            ///  The game is currently in play mode.
            Playing,

            ///  The game is currently paused.
            Paused,

            /// The map is being loaded.
            Loading,

            /// A map is being imported.
            ImportingMap,

            /// The game controller is disabled.
            Disabled,
        }

        /// The current game status.
        public GameStatus status;

        /// The game settings.
        public Settings settings;

        /// The current map display mode.
        public MapDisplayMode displayMode;

        public SnapController snapController;
        public SaveManager saveManager;

        ///  The currently loaded map.
        public Map loadedMap => SaveManager.loadedMap;

        public GameObject loadedMapObj;

        /// The transit controller instance.
        public TransitEditor transitEditor;

        /// The simulation controller instance.
        public SimulationController sim;

        /// Reference to the finance controller.
        public FinanceController financeController;

        ///  The input controller.
        public InputController input;

        /// The router instance.
        public Router router;

        /// The translator for the current language.
        public Translator lang;

        ///  Prefab for creating tooltips.
        public GameObject tooltipPrefab;

        /// Prefab for line renderers.
        public GameObject lineRendererPrefab;

        /// Prefab for color gradients.
        public GameObject colorGradientPrefab;

        /// Callback to be executed once the game is loaded.
        public UnityEvent onLoad;
        
        /// Whether or not pausing / unpausing is blocked.
        private bool _pauseBlocked = false;

#if DEBUG
        public string missionToLoad;
        public string saveFileToLoad;
        public QualitySettings.QualityLevel qualityLevel = QualitySettings.QualityLevel.High;
#endif

#if UNITY_EDITOR
        // Only for debugging.
        public OSMImportHelper.Area areaToLoad;
#endif

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
        /// The main ui.
        public MainUI mainUI;

        /// The main loading screen.
        public UILoadingScreen loadingScreen;

        /// The UI canvas.
        public Canvas uiCanvas;

        /// Mask of active click events for map objects.
        public MapObjectKind activeMouseDownEvents = MapObjectKind.All;

        /// Mask of active click events for map objects.
        public MapObjectKind activeMouseOverEvents = MapObjectKind.All;

        /// The street creation cursor sprite.
        public Sprite createStreetSprite;

        /// GameObject that renders the creation cursor sprite.
        public GameObject createCursorObj;

        /// The developer console instance.
        public DeveloperConsole developerConsole;

        static GameController _instance;
        public static GameController instance => _instance;

        public Material GetUnlitMaterial(Color c)
        {
            if (unlitMaterials == null)
            {
                unlitMaterials = new Dictionary<Color, Material>();
            }

            if (!unlitMaterials.TryGetValue(c, out Material m))
            {
                var html = ColorUtility.ToHtmlStringRGBA(c);
                m = Resources.Load($"Materials/SolidColors/{html}") as Material;

#if UNITY_EDITOR
                if (m == null)
                {
                    m = new Material(unlitMaterial)
                    {
                        color = c,
                    };

                    string materialPath = $"Assets/Resources/Materials/SolidColors";
                    UnityEditor.AssetDatabase.CreateAsset(m, $"{materialPath}/{html}.mat");
                    UnityEditor.AssetDatabase.SaveAssets();
                }
#endif

                unlitMaterials.Add(c, m);
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

        public bool MouseDownActive(MapObjectKind k)
        {
            return activeMouseDownEvents.HasFlag(k);
        }

        public bool MouseOverActive(MapObjectKind k)
        {
            return activeMouseOverEvents.HasFlag(k);
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

        public bool Playing => status == GameStatus.Playing;

        public bool Paused => status == GameStatus.Paused;

        public bool ImportingMap => status == GameStatus.ImportingMap;

        public bool Loading => status == GameStatus.Loading;

#if DEBUG
        public int numRandomLines = 50;

        public void LoadMap(string area)
        {
            var map = SaveManager.CreateMap(this, area);
            StartCoroutine(LoadMapAsync(map));
        }
#endif

        public void LoadMission(string mission)
        {
            StartCoroutine(LoadMissionAsync(mission));
        }
        
        System.Collections.IEnumerator LoadMissionAsync(string missionToLoad)
        {
            GameController._instance = this;

            this.status = GameStatus.Loading;
            loadingScreen.gameObject.SetActive(true);

            var mission = Mission.FromFile(missionToLoad);
            var map = mission.LoadMap();

            loadingScreen.SetText("Loading Map...");
            yield return SaveManager.LoadSave(this, map, saveFileToLoad);

            loadingScreen.SetText("Spawning Citizens...");
            yield return mission.Load();

            loadingScreen.SetText("Initializing Routes...");
            transitEditor.InitOverlappingRoutes();
            
            loadingScreen.SetText("Initializing Router...");
            router.Initialize(map);

            this.status = GameStatus.Paused;
            this.onLoad.Invoke();
            
            Debug.Log($"Residential: {map.buildingCapacity[Tuple.Create(Building.Type.Residential, OccupancyKind.Resident)]}");
            Debug.Log($"Kindergarden: {map.buildingCapacity[Tuple.Create(Building.Type.Kindergarden, OccupancyKind.Student)]}");
            Debug.Log($"ElementarySchool: {map.buildingCapacity[Tuple.Create(Building.Type.ElementarySchool, OccupancyKind.Student)]}");
            Debug.Log($"HighSchool: {map.buildingCapacity[Tuple.Create(Building.Type.HighSchool, OccupancyKind.Student)]}");

            Debug.Log($"Industrial: {map.buildingCapacity[Tuple.Create(Building.Type.Industrial, OccupancyKind.Worker)]}");
            Debug.Log($"Office: {map.buildingCapacity[Tuple.Create(Building.Type.Office, OccupancyKind.Worker)]}");

            this.status = GameStatus.Playing;
            loadingScreen.gameObject.SetActive(false);
            input.EnableControls();
        }

        System.Collections.IEnumerator LoadMapAsync(Map map)
        {
            GameController._instance = this;

            this.status = GameStatus.Loading;
            loadingScreen.gameObject.SetActive(true);

            yield return SaveManager.LoadSave(this, map, saveFileToLoad);

            loadingScreen.SetText("Initializing Router...");
            router.Initialize(map);
            
            this.status = GameStatus.Playing;
            this.onLoad.Invoke();

            loadingScreen.gameObject.SetActive(false);
            input.EnableControls();

#if DEBUG
            for (var i = 0; i < numRandomLines; ++i)
            {
                var type = (TransitType)RNG.Next(0, 5);
                var numStops = 0;
                
                if (type == TransitType.Bus)
                {
                    numStops = RNG.Next(10, 50);
                }

                loadedMap.CreateRandomizedLine(type, null, numStops);
            }

            transitEditor.InitOverlappingRoutes();

            if (PlayerPrefs.HasKey("dbg_startup_commands"))
            {
                var cmds = PlayerPrefs.GetString("dbg_startup_commands").Split(';');
                foreach (var cmd in cmds)
                {
                    DeveloperConsole.Run(cmd);
                }
            }
#endif
        }

        void Awake()
        {
            GameController._instance = this;

#if DEBUG
            this.settings = new Settings(this.qualityLevel);
#endif

            RNG.Reseed(-1);
            developerConsole.Initialize();

            if (status == GameStatus.Disabled || status == GameStatus.ImportingMap)
            {
                gameObject.SetActive(false);
                return;
            }

            this.lang = Translator.SetActiveLanguage("en_US");
            this.router = new Router();

            this.onLoad = new UnityEvent();

            this.circleShader = Resources.Load("Shaders/CircleShader") as Shader;
            this.defaultShader = Shader.Find("Unlit/Color");
            this.silhouetteDiffuseShader = Resources.Load("Shaders/SilhouettedDiffuse") as Shader;

            this.highlightedMaterials = new Dictionary<Tuple<Color, Color>, Material>();

            this.highlightColor = new Color(96f / 255f, 208f / 255f, 230f / 255f);
            this.bulldozeHighlightColor = new Color(189f / 255f, 41f / 255f, 56f / 255f);

            this.snapController = new SnapController(this);
        }

        void Start()
        {
            input.DisableControls();

            if (!string.IsNullOrEmpty(missionToLoad))
            {
                LoadMission(missionToLoad);
            }
#if UNITY_EDITOR
            else if (areaToLoad != OSMImportHelper.Area.Default)
            {
                LoadMap(areaToLoad.ToString());
            }
#endif
        }

        public bool SetLanguage(string newLang)
        {
            var translator = Translator.SetActiveLanguage(newLang);
            if (translator == null)
            {
                return true;
            }

            this.lang = translator;
            return false;
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

        public void TogglePause()
        {
            if (Paused)
            {
                ExitPause();
            }
            else
            {
                EnterPause();
            }
        }

        public bool EnterPause(bool block = false)
        {
            _pauseBlocked |= block;

            if (status == GameStatus.Paused)
            {
                return true;
            }

            this.status = GameStatus.Paused;
            mainUI.playPauseButton.GetComponent<Image>().sprite = SpriteManager.instance.playSprite;
            mainUI.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);

            return false;
        }

        public void ExitPause(bool unblock = false)
        {
            if (unblock)
            {
                _pauseBlocked = false;
            }
            else if (_pauseBlocked)
            {
                return;
            }

            this.status = GameStatus.Playing;
            mainUI.playPauseButton.GetComponent<Image>().sprite = SpriteManager.instance.pauseSprite;
            mainUI.simSpeedButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }

        public void UnblockPause()
        {
            _pauseBlocked = false;
        }
    }
}
