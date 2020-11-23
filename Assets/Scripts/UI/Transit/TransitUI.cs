using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class TransitUI : MonoBehaviour
    {
        /// Reference to the game controller.
        public GameController game;

        /// Buttons for the five transit systems.
        public GameObject[] transitSystemButtons;

        /// The lock icons for the transit systems.
        public Image[] transitSystemLocks;

        /// The currently selected transit system.
        public TransitType? selectedTransitSystem;

        /// The game objects containing the lines of the current system.
        public GameObject lineOverviewList;

        /// Prefab for creating new line entries.
        public GameObject transitLineEntryPrefab;

        /// Cache of created line entries.
        Dictionary<Line, UILineListEntry> transitSystemEntries;

        /// Text shown when no lines are available.
        public TMP_Text noLineDataText;

        /// Input field for filtering lines.
        public TMP_InputField lineSearchField;

        /// Text field showing how many lines are shown after filtering.
        public TMP_Text resultCountText;

        /// Total number of lines (for resultCountText).
        int totalDisplayedLines;

        /// The mini map.
        public UIMiniMap miniMap;

        /// The button for creating a new line.
        public Button createLineButton;

        /// The button for editing a line.
        public Button editLineButton;

        /// The button for deleting a line.
        public Button deleteLineButton;

        /// Prefab for temporary stops.
        public GameObject temporaryStopPrefab;

        /// The line builder instances.
        public LineBuilder[] lineBuilders;

        /// The currently active line builder.
        public LineBuilder activeBuilder;

        /// The panel that is shown after completing a line.
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
                new SubwayLineBuilder(game),
                null,
                new FerryLineBuilder(game), 
            };

            foreach (var builder in this.lineBuilders)
            {
                builder?.Initialize();
            }

            var i = 0;
            foreach (var button in transitSystemButtons)
            {
                var system = (TransitType)i;
                button.gameObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (activeBuilder != null)
                    {
                        activeBuilder.EndLineCreation();
                    }

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
                Debug.Assert(selectedTransitSystem.HasValue);
                this.HideTransitSystemOverviewPanel(false);

                activeBuilder = lineBuilders[(int) selectedTransitSystem.Value];
                activeBuilder.StartLineCreation();
            });
        }

        public void Activate()
        {
            for (var i = 0; i <= (int) TransitType.Ferry; ++i)
            {
                var type = (Progress.Unlockable) ((int) Progress.Unlockable.Bus + i);
                if (game.Progress.IsUnlocked(type))
                {
                    transitSystemButtons[i].GetComponent<Image>().color = Colors.GetDefaultSystemColor((TransitType) i);
                    transitSystemButtons[i].GetComponent<Button>().enabled = true;
                    transitSystemButtons[i].transform.GetChild(0).gameObject.SetActive(true);
                    transitSystemLocks[i].gameObject.SetActive(false);
                }
                else
                {
                    transitSystemButtons[i].GetComponent<Image>().color = new Color(43f / 255f, 43f / 255f, 43f / 255f);
                    transitSystemButtons[i].GetComponent<Button>().enabled = false;
                    transitSystemButtons[i].transform.GetChild(0).gameObject.SetActive(false);
                    transitSystemLocks[i].gameObject.SetActive(true);
                }
            }
        }

        void DrawLine(UILineListEntry entry)
        {
            var worldPath = new List<Vector2>();
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
            this.miniMap.gameObject.SetActive(false);
            this.editLineButton.interactable = false;
            this.deleteLineButton.interactable = false;
        }

        void HandleLineDataAvailable()
        {
            this.noLineDataText.enabled = false;
            this.miniMap.gameObject.SetActive(true);
            this.editLineButton.interactable = true;
            this.deleteLineButton.interactable = true;
        }

        public void ShowTransitSystemOverviewPanel(TransitType type)
        {
            miniMap.Initialize();
            
            if (transitSystemEntries == null)
            {
                transitSystemEntries = new Dictionary<Line, UILineListEntry>();
                lineSearchField.onValueChanged.AddListener(value =>
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

                if (k != (int)type)
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

                var obj = Instantiate(transitLineEntryPrefab, this.lineOverviewList.transform, false);
                
                entry = obj.GetComponent<UILineListEntry>();
                transitSystemEntries.Add(line, entry);

                entry.Initialize();
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
                Activate();
                this.selectedTransitSystem = null;
            }

            game.mainUI.HideConstructionCost();
            this.gameObject.SetActive(false);
            game.input.EnableControls();

            UIInstruction.Hide();
        }
    }
}
