using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Transidious
{
    public class DeveloperConsole : MonoBehaviour
    {
        enum ArgumentType
        {
            Integer,
            FloatingPoint,
            String,
            Switch,
        }

        DeveloperConsoleInternals internals;
        [SerializeField] Button runButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject commandPrefab;
        [SerializeField] GameObject scrollViewContent;
        [SerializeField] TMP_InputField commandInput;

        GameController game;
        SimulationController sim;

        public static DeveloperConsole instance;
        public static string currentCommand;

        List<string> history;
        int currentHistoryEntry = -1;

        public void Initialize()
        {
            instance = this;

            internals = new DeveloperConsoleInternals(this);
            history = new List<string>();

            game = GameController.instance;
            sim = game.sim;
        }

        void Start()
        {
            this.closeButton.onClick.AddListener(() =>
            {
                this.Deactivate();
            });

            this.clearButton.onClick.AddListener(() =>
            {
                this.scrollViewContent.RemoveAllChildren();
            });

            this.commandInput.onSubmit.AddListener((string cmd) =>
            {
                Run(cmd);
                this.commandInput.SetTextWithoutNotify(string.Empty);
            });

            this.runButton.onClick.AddListener(() =>
            {
                Run(this.commandInput.text);
                this.commandInput.SetTextWithoutNotify(string.Empty);
            });
        }

        void Update()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            if (history.Count == 0)
            {
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != commandInput.gameObject)
            {
                return;
            }

            var update = false;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                update = true;
                currentHistoryEntry = Mathf.Clamp(currentHistoryEntry + 1, -1, history.Count - 1);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                update = true;
                currentHistoryEntry = Mathf.Clamp(currentHistoryEntry - 1, -1, history.Count - 1);
            }

            if (update)
            {
                if (currentHistoryEntry == -1)
                {
                    this.commandInput.SetTextWithoutNotify(string.Empty);
                }
                else
                {
                    this.commandInput.SetTextWithoutNotify(history[history.Count - 1 - this.currentHistoryEntry]);
                }
            }
        }

        public void Toggle()
        {
            if (game.mainUI.developerConsole.activeSelf)
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }

        public void Activate()
        {
            game.mainUI.developerConsole.SetActive(true);
            GameController.instance.input.DisableControls();
        }

        public void Deactivate()
        {
            game.mainUI.developerConsole.SetActive(false);
            GameController.instance.input.EnableControls();
        }

        public static void Run(string rawCmd)
        {
            instance.RunImpl(rawCmd);
        }

        void RunImpl(string rawCmd)
        {
            currentHistoryEntry = -1;

            if (string.IsNullOrEmpty(rawCmd))
            {
                return;
            }

            history.Add(rawCmd);
            internals.ParseCommand(rawCmd);
        }

        public void HandleHelpCommand(string cmd)
        {
            if (cmd == "-")
            {
                Log(internals.GetCommandList());
            }
            else
            {
                Log(internals.GetHelp(cmd));
            }
        }

        public void HandleExitCommand()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void HandlePauseCommand()
        {
            game.EnterPause();
        }

        public void HandleUnpauseCommand()
        {
            game.ExitPause();
        }

        public void HandleSetSimSpeedCommand(int simSpeed)
        {
            sim.SetSimulationSpeed((SimulationController.SimulationSpeed)simSpeed);
            Log($"sim speed set to {sim.simulationSpeed}");
        }

        public void HandleSetTimeCommand(string timeStr)
        {
            var values = timeStr.Split(':');
            if (values.Length != 2)
            {
                Log("time format HH:MM expected");
                return;
            }

            if (!int.TryParse(values[0], out int hour) || hour >= 24)
            {
                Log("time format HH:MM expected");
                return;
            }
            if (!int.TryParse(values[1], out int minute) || minute >= 60)
            {
                Log("time format HH:MM expected");
                return;
            }

            var gt = sim.GameTime;
            var dt = new System.DateTime(
                gt.Year, gt.Month, gt.Day,
                hour, minute, gt.Second,
                gt.Millisecond);

            sim.SetGameTime(dt);
            Log($"game time set to {dt}");
        }

        public void HandleSetLangCommand(string lang)
        {
            if (game.SetLanguage(lang))
            {
                Log($"unknown language {lang}");
            }
            else
            {
                Log($"language set to {lang}");
            }
        }

        public void HandleEarnCommand(decimal amount)
        {
            game.financeController.Earn(amount);
            Log($"earned {Translator.GetCurrency(amount, true, false)}");
        }

        public void HandleSpendCommand(decimal amount)
        {
            game.financeController.Purchase(amount);
            Log($"spent {Translator.GetCurrency(amount, true, false)}");
        }

        public void HandleSpawnCitizensCommand(int amount)
        {
            var citizens = new List<Citizen>();
            sim.SpawnRandomCitizens(amount, citizens);

            foreach (var c in citizens)
            {
                Log($"spawned {c.Name}");
            }

            if (amount == 1)
            {
                game.input.SetZoomLevel(InputController.minZoom);
                game.input.MoveTowards(citizens.First().Home.centroid);
            }
        }

        public void HandleSpawnCarsCommand(int amount)
        {
            StartCoroutine(sim.SpawnTestCars(amount));
            Log($"spawned {amount} cars");
        }

        public void HandleCreateRandomLineCommand(string type, int stops, string name)
        {
            TransitType transitType;
            switch (type)
            {
                case "bus": transitType = TransitType.Bus; break;
                case "tram": transitType = TransitType.Tram; break;
                case "subway": transitType = TransitType.Subway; break;
                case "lightrail": transitType = TransitType.IntercityRail; break;
                case "ferry": transitType = TransitType.Ferry; break;
                default:
                    Log("invalid transit type");
                    return;
            }

            var line = game.loadedMap.CreateRandomizedLine(transitType, name, stops);
            Log($"created {line.type} line {line.name} with {line.stops.Count} stops");
        }

        public void HandleSaveCommand(string fileName)
        {
            SaveManager.SaveMapData(game.loadedMap, fileName);
            Log($"saved as '{fileName}'");
        }

        public void HandleLoadCommand(string fileName)
        {
            StartCoroutine(SaveManager.LoadSave(game.loadedMap, fileName, true));
            Log($"loaded save file '{fileName}'");
        }

        public void HandleExportMapCommand(string fileName, int resolution)
        {
#if UNITY_EDITOR
            if (fileName == null)
            {
                fileName = game.loadedMap.name;
            }

            var exporter = new MapExporter(game.loadedMap, resolution);
            exporter.ExportMap(fileName);

            Log($"exported map as {fileName}.png");
#endif
        }

        public void HandleSetPrefCommand(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void HandleGetPrefCommand(string key)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                Log($"key {key} not found");
                return;
            }

            Log(PlayerPrefs.GetString(key));
        }

        public void HandleClearPrefCommand(string key)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                Log($"key {key} not found");
                return;
            }

            PlayerPrefs.DeleteKey(key);
        }

        public void HandleUnlockCommand(string item)
        {
            if (!Enum.TryParse(item, out Progress.Unlockable result))
            {
                Log($"invalid unlockable: {item}");
                return;
            }

            GameController.instance.Progress.Unlock(result);
            Log($"unlocked {item}");
        }

        public void HandleAddStartupCommandCommand()
        {
            if (history.Count < 2)
            {
                Log("no commands exected in current session");
                return;
            }

            if (PlayerPrefs.HasKey("dbg_startup_commands"))
            {
                PlayerPrefs.SetString("dbg_startup_commands", PlayerPrefs.GetString("dbg_startup_commands") + ";" + history[history.Count - 2]);
            }
            else
            {
                PlayerPrefs.SetString("dbg_startup_commands", history[history.Count - 2]);
            }

            Log("added startup command");
        }

        public void HandleLocateCommand(string name)
        {
            var citizen = game.sim.citizens.FirstOrDefault(c => c.Value.Name == name).Value;
            if (citizen == null)
            {
                var obj = SaveManager.loadedMap.GetMapObject(name);
                if (obj == null)
                {
                    Log($"'{name}' not found");
                    return;
                }
                
                Log($"located '{name}'");

                game.input.SetZoomLevel(50f);
                game.input.MoveTowards(obj.Centroid);
                obj.ActivateModal();

                return;
            }

            Log($"located '{name}'");

            game.input.SetZoomLevel(50f);
            game.input.MoveTowards(citizen.CurrentPosition);
            citizen.ActivateModal();
        }

        public static void Log(string msg)
        {
            instance.LogImpl(msg);
        }

        void LogImpl(string msg)
        {
            var txt = Instantiate(commandPrefab);
            txt.GetComponent<TMP_Text>().SetText("> " + currentCommand + ": " + msg);
            txt.transform.SetParent(scrollViewContent.transform, false);
        }
    }
}
