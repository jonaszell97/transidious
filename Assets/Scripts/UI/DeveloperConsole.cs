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

        void Start()
        {
            internals = new DeveloperConsoleInternals(this);
            history = new List<string>();

            game = GameController.instance;
            sim = game.sim;

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
            if (gameObject.activeSelf)
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
            GameController.instance.input.DisableControls();
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
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
            sim.SetSimulationSpeed(simSpeed);
            Log($"sim speed set to {sim.simulationSpeed} / 3");
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

        public void HandleSpawnCitiziensCommand(int amount)
        {
            var citiziens = new List<Citizien>();
            sim.SpawnRandomCitiziens(amount, citiziens);

            foreach (var c in citiziens)
            {
                Log($"spawned {c.Name}");
            }
        }

        public void HandleSpawnCarsCommand(int amount)
        {
            sim.SpawnTestCars(amount);
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
            StartCoroutine(SaveManager.LoadSave(game.loadedMap, fileName));
            Log($"loaded save file '{fileName}'");
        }

        public void HandleExportMapCommand(string fileName)
        {
            if (fileName == null)
            {
                fileName = game.loadedMap.name;
            }

            MapExporter.ExportMap(game.loadedMap, fileName);
            Log($"exported map as {fileName}.png");
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
