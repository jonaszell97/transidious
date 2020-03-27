using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class TransitUI : MonoBehaviour
    {
        /// <summary>
        /// Reference to the game controller.
        /// </summary>
        public GameController game;

        /// <summary>
        /// Buttons for the five transit systems.
        /// </summary>
        public GameObject[] transitSystemButtons;

        /// <summary>
        /// The currently selected transit system.
        /// </summary>
        public TransitType? selectedTransitSystem;

        /// <summary>
        /// The game objects containing the lines of the current system.
        /// </summary>
        public GameObject lineOverviewList;

        /// <summary>
        /// Prefab for creating new line entries.
        /// </summary>
        public GameObject transitLineEntryPrefab;

        /// <summary>
        /// Cache of created line entries.
        /// </summary>
        Dictionary<Line, UILineListEntry> transitSystemEntries;

        /// <summary>
        /// Text shown when no lines are available.
        /// </summary>
        public TMP_Text noLineDataText;

        /// <summary>
        /// Input field for filtering lines.
        /// </summary>
        public TMP_InputField lineSearchField;

        /// <summary>
        /// Text field showing how many lines are shown after filtering.
        /// </summary>
        public TMP_Text resultCountText;

        /// <summary>
        /// Total number of lines (for resultCountText).
        /// </summary>
        int totalDisplayedLines;

        /// <summary>
        /// The mini map.
        /// </summary>
        public UIMiniMap miniMap;

        /// <summary>
        /// The button for creating a new line.
        /// </summary>
        public Button createLineButton;

        /// <summary>
        /// The button for editing a line.
        /// </summary>
        public Button editLineButton;

        /// <summary>
        /// The button for deleting a line.
        /// </summary>
        public Button deleteLineButton;

        /// <summary>
        /// Prefab for temporary stops.
        /// </summary>
        public GameObject temporaryStopPrefab;

        /// <summary>
        /// The line builder instances.
        /// </summary>
        public LineBuilder[] lineBuilders;

        /// <summary>
        /// The panel that is shown after completing a line.
        /// </summary>
        public GameObject confirmLineCreationPanel;
        public UIInfoPanel confirmLineCreationInfo;
        public Button confirmButton;
        public Button cancelButton;

        public void Initialize()
        {
            this.lineBuilders = new LineBuilder[]
            {
                new BusLineBuilder(game),
                new TramLineBuilder(game),
            };

            foreach (var builder in this.lineBuilders)
            {
                builder.Initialize();
            }

            var i = 0;
            foreach (var button in transitSystemButtons)
            {
                var system = (TransitType)i;
                button.gameObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (selectedTransitSystem.HasValue && selectedTransitSystem.Value == system)
                    {
                        this.HideTransitSystemOverviewPanel();
                    }
                    else
                    {
                        this.ShowTransitSystemOverviewPanel(system);
                    }
                });

                ++i;
            }

            createLineButton.onClick.AddListener(() =>
            {
                this.HideTransitSystemOverviewPanel(false);
                this.lineBuilders[(int)selectedTransitSystem.Value].StartLineCreation();
            });
        }

        void DrawLine(UILineListEntry entry)
        {
            var worldPath = new List<Vector3>();
            foreach (var route in entry.line.routes)
            {
                if (route.positions != null)
                    worldPath.AddRange(route.positions);
            }

            miniMap.DrawLine(worldPath, entry.line.color);
        }

        void HandleNoLineDataAvailable()
        {
            this.noLineDataText.enabled = true;
            this.editLineButton.interactable = false;
            this.deleteLineButton.interactable = false;
        }

        void HandleLineDataAvailable()
        {
            this.noLineDataText.enabled = false;
            this.editLineButton.interactable = true;
            this.deleteLineButton.interactable = true;
        }

        public void ShowTransitSystemOverviewPanel(TransitType type)
        {
            if (transitSystemEntries == null)
            {
                transitSystemEntries = new Dictionary<Line, UILineListEntry>();
                lineSearchField.onValueChanged.AddListener((string value) =>
                {
                    var searchResults = 0;
                    UILineListEntry firstResult = null;

                    foreach (var line in game.loadedMap.transitLines)
                    {
                        if (line.type != selectedTransitSystem.Value)
                        {
                            continue;
                        }

                        if (!transitSystemEntries.TryGetValue(line, out UILineListEntry entry))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(value) || line.name.Contains(value))
                        {
                            entry.gameObject.SetActive(true);
                            ++searchResults;
                            firstResult = firstResult ?? entry;
                        }
                        else
                        {
                            entry.gameObject.SetActive(false);
                        }
                    }

                    if (firstResult != null)
                    {
                        firstResult.Select();
                        HandleLineDataAvailable();
                    }
                    else
                    {
                        HandleNoLineDataAvailable();
                    }

                    resultCountText.text = "Showing " + searchResults + " / " + totalDisplayedLines + " lines";
                });
            }

            game.input.DisableControls();
            this.selectedTransitSystem = type;

            for (var k = 0; k < transitSystemButtons.Length; ++k)
            {
                var img = transitSystemButtons[k].GetComponent<Image>();
                var txt = transitSystemButtons[k].transform.GetChild(0).GetComponent<TMPro.TMP_Text>();

                if (k == (int)type)
                {
                    img.color = Colors.GetDefaultSystemColor(type);
                    txt.color = Color.white;
                }
                else
                {
                    img.color = Math.ApplyTransparency(Color.black, 164f / 255f);
                    txt.color = Math.ApplyTransparency(Color.white, 164f / 255f);
                }
            }

            foreach (var entry in this.transitSystemEntries)
            {
                entry.Value.gameObject.SetActive(false);
            }

            totalDisplayedLines = 0;

            UILineListEntry firstEntry = null;
            foreach (var line in game.loadedMap.transitLines)
            {
                if (line.type != type)
                {
                    continue;
                }

                ++totalDisplayedLines;

                if (transitSystemEntries.TryGetValue(line, out UILineListEntry entry))
                {
                    entry.gameObject.SetActive(true);
                    if (firstEntry == null)
                    {
                        firstEntry = entry;
                    }

                    continue;
                }

                var obj = Instantiate(transitLineEntryPrefab);
                obj.transform.SetParent(this.lineOverviewList.transform, false);

                entry = obj.GetComponent<UILineListEntry>();
                transitSystemEntries.Add(line, entry);

                entry.SetLine(line);
                entry.onSelect.AddListener(() => this.DrawLine(entry));

                if (firstEntry == null)
                {
                    firstEntry = entry;
                }
            }

            if (firstEntry != null)
            {
                firstEntry.Select();
                HandleLineDataAvailable();
            }
            else
            {
                HandleNoLineDataAvailable();
            }

            resultCountText.text = "Showing " + totalDisplayedLines + " / " + totalDisplayedLines + " lines";
            this.gameObject.SetActive(true);
        }

        public void HideTransitSystemOverviewPanel(bool resetButtons = true)
        {
            if (resetButtons)
            {
                for (var k = 0; k < transitSystemButtons.Length; ++k)
                {
                    var img = transitSystemButtons[k].GetComponent<Image>();
                    var txt = transitSystemButtons[k].transform.GetChild(0).GetComponent<TMPro.TMP_Text>();

                    img.color = Colors.GetDefaultSystemColor((TransitType)k);
                    txt.color = Color.white;
                }

                this.selectedTransitSystem = null;
            }

            game.mainUI.HideConstructionCost();
            this.gameObject.SetActive(false);
            game.input.EnableControls();

            UIInstruction.Hide();
        }
    }
}
