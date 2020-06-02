using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Transidious.UI;
using UI;

namespace Transidious
{
    public class MainUI : MonoBehaviour
    {
        public enum State
        {
            Default,
            Transitioning,
            TransitEditor,
            Data,
            Settings,
            LineBuilding,
        }

        /// The main UI instance.
        public static MainUI instance;

        /// Referene to the main camera.
        public Camera mainCamera;

        /// Reference to the game controller.
        public GameController game;

        /// The state of the UI.
        public State state;

        /// The modal instance.
        public UIModal modal;

        /// Array containing all of the ui elements from left to right.
        public RectTransform[] panels;

        /// The instruction panel.
        public UIInstruction instructionPanel;

        /// <summary>
        /// The greyed out overlay.
        /// </summary>
        [SerializeField] GameObject overlay;

        /**
         * Date & Time Tab
         */

        /// The play/pause button.
        public Button playPauseButton;

        /// The game time text.
        public TMP_Text gameTimeText;

        /// The simulation speed button.
        public Button simSpeedButton;

        /// The expandable time panel.
        public UIExpandablePanel expandableTimePanel;

        /// The long format date text.
        public TMP_Text longFormatDate;

        /**
         * Population Tab
         */

        /// The citizen count ui text.
        public TMPro.TMP_Text citizenCountText;

        /// The citizen trend arrow.
        public Image citizenCountTrendImg;

        /**
         * Finances Tab
         */

        /// <summary>
        /// The money ui text object.
        /// </summary>
        public TMPro.TMP_Text moneyText;

        /// <summary>
        /// The income ui text object.
        /// </summary>
        public TMPro.TMP_Text incomeText;

        /// The background panel image.
        public Image financeBackgroundImage;

        /// The detailed financial panel.
        public UIInfoPanel financesPanel;

        /// The expandable finances panel.
        public UIExpandablePanel expandableFinancePanel;

        /// <summary>
        /// The income colors.
        /// </summary>
        public Color[] financeColors;

        /// <summary>
        /// The panel that shows construction and monthly costs.
        /// </summary>
        public GameObject constructionCostPanel;
        public TMP_Text constructionCostText;
        public TMP_Text monthlyCostText;

        /**
         * Right-hand side icons.
         */

        public Button[] settingsIcons;

        /// <summary>
        ///  The transit editor panel, hidden by default.
        /// </summary>
        public GameObject transitEditorPanel;

        /// <summary>
        /// Reference to the transit UI controller.
        /// </summary>
        public TransitUI transitUI;

        /// <summary>
        ///  The settings panel, hidden by default.
        /// </summary>
        public GameObject settingsPanel;

        /// <summary>
        ///  The data panel, hidden by default.
        /// </summary>
        public GameObject dataPanel;

        /// The line building panel.
        public GameObject lineBuildingPanel;

        /// The line building redo button.
        public UIIcon lineBuildingRedoButton;
        
        /// The line building undo button.
        public UIIcon lineBuildingUndoButton;
        
        /// The line building trash button.
        public UIIcon lineBuildingTrashButton;

        /**
         * Settings buttons
         */
        public Button consoleButton;
        public GameObject developerConsole;

        /**
         * Data buttons
         */
        public Button populationHeatmapButton;

        /**
         * Scale Bar
         */

        /// The scale bar.
        public GameObject scaleBar;

        /// The scale text.
        public TMP_Text scaleText;

        /// <summary>
        ///  Time after which the scale bar should fade.
        /// </summary>
        public float fadeScaleBarTime = 0f;
        
        /**
         * Tooltip
         */

        /// <summary>
        /// Reference to the tooltip instance.
        /// </summary>
        public UITooltip tooltipInstance;

        /// The generic confirmation dialog.
        public UIConfirmPanel confirmPanel;

        /**
         * Modals
         */

        /// The citizen info modal.
        public UICitizenInfoModal citizenModal;
        
        /// The building info modal.
        public UIBuildingInfoModal buildingModal;
        
        /// The feature info modal.
        public UIFeatureInfoModal featureModal;
        
        /// The stop info modal.
        public UIStopModal stopModal;
        
        /// The line info modal.
        public UILineModal lineModal;
        
        /// The transit vehicle info modal.
        public UITransitVehicleModal transitVehicleModal;

#if DEBUG
        /// The street modal.
        public UIStreetModal streetModal;

        /// The intersection modal.
        public UIIntersectionModal intersectionModal;
#endif
        
        void RegisterUICallbacks()
        {
            this.playPauseButton.onClick.AddListener(OnPlayPauseClick);
            this.simSpeedButton.onClick.AddListener(OnSimSpeedClick);

            // Finance panel
            financesPanel.AddItem("Earnings", "ui:finances:earnings", "0");
            financesPanel.AddItem("Expenses", "ui:finances:expenses", "0");
            financesPanel.AddItem("Income", "ui:finances:income", "0");

            // Transit panel
            this.settingsIcons[0].onClick.AddListener(() =>
            {
                switch (state)
                {
                    case State.Default:
                        ShowTransitPanel();
                        break;
                    case State.TransitEditor:
                        ShowPanels();
                        break;
                    default:
                        break;
                }
            });

            // Data panel
            this.settingsIcons[1].onClick.AddListener(() =>
            {
                switch (state)
                {
                    case State.Default:
                        ShowDataPanel();
                        break;
                    case State.Data:
                        ShowPanels();
                        break;
                    default:
                        break;
                }
            });

            // Settings panel
            this.settingsIcons[2].onClick.AddListener(() =>
            {
                switch (state)
                {
                    case State.Default:
                        ShowSettingsPanel();
                        break;
                    case State.Settings:
                        ShowPanels();
                        break;
                    default:
                        break;
                }
            });

            // Console
            this.consoleButton.onClick.AddListener(() =>
            {
                game.developerConsole.Toggle();
            });

            // Population heatmap
            this.populationHeatmapButton.onClick.AddListener(() =>
            {
                var map = game.loadedMap;
                if (map.heatmap.gameObject.activeSelf)
                {
                    map.heatmap.Hide();
                }
                else
                {
                    ShowPopulationHeatmap();
                }
            });

            // Modals
            citizenModal.Initialize();
            buildingModal.Initialize();
            featureModal.Initialize();
            stopModal.Initialize();
            lineModal.Initialize();
            transitVehicleModal.Initialize();
            
#if DEBUG
            streetModal.Initialize();
            intersectionModal.Initialize();
#endif
        }

        void Awake()
        {
            instance = this;
            state = State.Default;
            mainCamera = Camera.main;
            UITooltip.instance = tooltipInstance;
            UIInstruction.instance = instructionPanel;
        }

        void Start()
        {
            RegisterUICallbacks();
            UpdateDate(game.sim.GameTime);
            UpdateFinances();

            transitUI.Initialize();
        }

        public void ShowOverlay()
        {
            this.overlay.gameObject.SetActive(true);
        }

        public void HideOverlay()
        {
            this.overlay.gameObject.SetActive(false);
        }

        void OnPlayPauseClick()
        {
            game.TogglePause();
        }

        void OnSimSpeedClick()
        {
            if (game.Paused)
            {
                game.ExitPause();
            }

            var newSimSpeed = ((int)game.sim.simulationSpeed + 1) % 3;
            game.sim.SetSimulationSpeed((SimulationController.SimulationSpeed)newSimSpeed);

            this.simSpeedButton.GetComponent<Image>().sprite = SpriteManager.instance.simSpeedSprites[newSimSpeed];
        }

        public void UpdateDate(DateTime date)
        {
            this.longFormatDate.text = Translator.GetDate(game.sim.GameTime, Translator.DateFormat.DateLong);
        }

        public void UpdateFinances()
        {
            var finances = game.financeController;
            moneyText.text = Translator.GetCurrency(finances.Money, true);

            var income = finances.Income;
            var incomeCmp = income.CompareTo(0);
            var incomeStr = Translator.GetCurrency(income, false, true);

            incomeText.text = incomeStr;

            var c = financeColors[incomeCmp + 1];
            financeBackgroundImage.color = new Color(c.r, c.g, c.b, financeBackgroundImage.color.a);

            financesPanel.SetValue("Earnings", Translator.GetCurrency(finances.earnings));
            financesPanel.SetValue("Expenses", Translator.GetCurrency(finances.expenses));
            
            financesPanel.SetValue("Income", incomeStr);
            financesPanel.GetValue("Income").color = new Color(c.r, c.g, c.b, 1f);
        }

        void HidePanels(State finalState, int panelIndex, Action onDone = null)
        {
            if (state != State.Default)
            {
                return;
            }

            state = State.Transitioning;

            if (UIExpandablePanel.activePanel != null)
            {
                UIExpandablePanel.activePanel.HideNoAnim();
            }

            var duration = .3f;

            // Animate date & time panel
            var timePanelAnimator = panels[0].gameObject.GetComponent<TransformAnimator>();
            if (timePanelAnimator == null)
            {
                timePanelAnimator = panels[0].gameObject.AddComponent<TransformAnimator>();
                timePanelAnimator.Initialize();
                timePanelAnimator.SetTargetSizeDelta(
                    new Vector2(gameTimeText.GetComponent<RectTransform>().rect.width + 20, panels[0].sizeDelta.y));

                timePanelAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            var gameTimeTextAnimator = gameTimeText.GetComponent<TransformAnimator>();
            if (gameTimeTextAnimator == null)
            {
                gameTimeTextAnimator = gameTimeText.gameObject.AddComponent<TransformAnimator>();
                gameTimeTextAnimator.Initialize();
                gameTimeTextAnimator.SetTargetAnchoredPosition(new Vector2(10f, gameTimeText.GetComponent<RectTransform>().anchoredPosition.y));
                gameTimeTextAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            panels[0].GetComponent<Button>().enabled = false;
            playPauseButton.gameObject.SetActive(false);
            simSpeedButton.gameObject.SetActive(false);

            // Animate finances panel
            var financePanelAnimator = panels[1].gameObject.GetComponent<TransformAnimator>();
            if (financePanelAnimator == null)
            {
                financePanelAnimator = panels[1].gameObject.AddComponent<TransformAnimator>();
                financePanelAnimator.Initialize();
                financePanelAnimator.SetTargetSizeDelta(new Vector2(0f, panels[1].sizeDelta.y));

                var posDiff = timePanelAnimator.originalScale.x - timePanelAnimator.targetScale.x;
                financePanelAnimator.SetTargetAnchoredPosition(
                    new Vector2(panels[1].anchoredPosition.x - posDiff, panels[1].anchoredPosition.y));
                
                financePanelAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            financePanelAnimator.onFinish = null;
            panels[1].gameObject.DisableImmediateChildren();

            // Animate population panel
            var populationPanelAnimator = panels[2].gameObject.GetComponent<TransformAnimator>();
            if (populationPanelAnimator == null)
            {
                populationPanelAnimator = panels[2].gameObject.AddComponent<TransformAnimator>();
                populationPanelAnimator.Initialize();
                populationPanelAnimator.SetTargetSizeDelta(new Vector2(0f, panels[2].sizeDelta.y));

                var posDiff = timePanelAnimator.originalScale.x - timePanelAnimator.targetScale.x;
                populationPanelAnimator.SetTargetAnchoredPosition(
                    new Vector2(panels[2].anchoredPosition.x - panels[1].rect.width - posDiff, panels[2].anchoredPosition.y));
                
                populationPanelAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            populationPanelAnimator.onFinish = null;
            panels[2].gameObject.DisableImmediateChildren();

            // Animate details panel
            var detailsPanelAnimator = panels[3].gameObject.GetComponent<TransformAnimator>();
            if (detailsPanelAnimator == null)
            {
                detailsPanelAnimator = panels[3].gameObject.AddComponent<TransformAnimator>();
                detailsPanelAnimator.Initialize();
                detailsPanelAnimator.SetTargetSizeDelta(new Vector2(40f, panels[3].sizeDelta.y));

                var posDiff = timePanelAnimator.originalScale.x - timePanelAnimator.targetScale.x;
                detailsPanelAnimator.SetTargetAnchoredPosition(
                    new Vector2(panels[3].anchoredPosition.x - panels[2].rect.width - panels[1].rect.width - posDiff,
                    panels[3].anchoredPosition.y));

                detailsPanelAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            TransformAnimator iconAnimator = null;
            for (var i = 0; i < settingsIcons.Length; ++i)
            {
                var icon = settingsIcons[i];
                if (i == panelIndex)
                {
                    iconAnimator = icon.GetComponent<TransformAnimator>();
                    if (iconAnimator == null)
                    {
                        var rc = icon.GetComponent<RectTransform>();
                        iconAnimator = icon.gameObject.AddComponent<TransformAnimator>();
                        iconAnimator.Initialize();

                        iconAnimator.SetTargetAnchoredPosition(
                            new Vector2(-detailsPanelAnimator.targetScale.x + (rc.rect.width / 2f) + 7.5f, rc.anchoredPosition.y));

                        iconAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
                    }
                }
                else
                {
                    icon.gameObject.SetActive(false);
                }
            }

            // Animate multi-use panel.
            var multiPanelAnimator = panels[4].gameObject.GetComponent<TransformAnimator>();
            if (multiPanelAnimator == null)
            {
                multiPanelAnimator = panels[4].gameObject.AddComponent<TransformAnimator>();
                multiPanelAnimator.Initialize();

                multiPanelAnimator.SetTargetSizeDelta(
                    new Vector2(panels[4].transform.parent.GetComponent<RectTransform>().rect.width - timePanelAnimator.targetScale.x - detailsPanelAnimator.targetScale.x,
                    panels[4].sizeDelta.y));

                var posDiff = timePanelAnimator.originalScale.x - timePanelAnimator.targetScale.x;
                multiPanelAnimator.SetTargetAnchoredPosition(
                    new Vector2(timePanelAnimator.targetScale.x + detailsPanelAnimator.targetScale.x,
                    panels[3].anchoredPosition.y));

                multiPanelAnimator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);
            }

            timePanelAnimator.onFinish = () =>
            {
                state = finalState;
                onDone?.Invoke();
            };

            gameTimeTextAnimator.StartAnimation(duration);
            timePanelAnimator.StartAnimation(duration);
            financePanelAnimator.StartAnimation(duration);
            populationPanelAnimator.StartAnimation(duration);
            detailsPanelAnimator.StartAnimation(duration);
            iconAnimator?.StartAnimation(duration);
            multiPanelAnimator.StartAnimation(duration);
        }

        public void ShowPanels()
        {
            if (state == State.Default || state == State.Transitioning)
            {
                return;
            }

            if (transitUI.selectedTransitSystem.HasValue)
            {
                transitUI.HideTransitSystemOverviewPanel();
            }

            this.transitEditorPanel.gameObject.SetActive(false);
            this.dataPanel.gameObject.SetActive(false);
            this.settingsPanel.gameObject.SetActive(false);
            this.lineBuildingPanel.gameObject.SetActive(false);

            var duration = .3f;

            // Animate date & time panel
            var timePanelAnimator = panels[0].gameObject.GetComponent<TransformAnimator>();
            var gameTimeTextAnimator = gameTimeText.GetComponent<TransformAnimator>();
            
            timePanelAnimator.onFinish = () =>
            {
                panels[0].GetComponent<Button>().enabled = true;
                playPauseButton.gameObject.SetActive(true);
                simSpeedButton.gameObject.SetActive(true);

                foreach (var icon in settingsIcons)
                {
                    icon.gameObject.SetActive(true);
                }

                state = State.Default;
            };

            // Animate finances panel
            var financePanelAnimator = panels[1].gameObject.GetComponent<TransformAnimator>();
            financePanelAnimator.onFinish = () =>
            {
                panels[1].gameObject.EnableImmediateChildren();
            };

            // Animate population panel
            var populationPanelAnimator = panels[2].gameObject.GetComponent<TransformAnimator>();
            populationPanelAnimator.onFinish = () =>
            {
                panels[2].gameObject.EnableImmediateChildren();
            };

            // Animate details panel
            var detailsPanelAnimator = panels[3].gameObject.GetComponent<TransformAnimator>();

            TransformAnimator iconAnimator = null;
            for (var i = 0; i < settingsIcons.Length; ++i)
            {
                var icon = settingsIcons[i];
                if (icon.gameObject.activeSelf)
                {
                    iconAnimator = icon.GetComponent<TransformAnimator>();
                }
            }
            
            // Animate multi-use panel.
            var multiPanelAnimator = panels[4].gameObject.GetComponent<TransformAnimator>();

            gameTimeTextAnimator.StartAnimation(duration);
            timePanelAnimator.StartAnimation(duration);
            financePanelAnimator.StartAnimation(duration);
            populationPanelAnimator.StartAnimation(duration);
            detailsPanelAnimator.StartAnimation(duration);
            iconAnimator?.StartAnimation(duration);
            multiPanelAnimator.StartAnimation(duration);
        }

        public void ShowTransitPanel()
        {
            this.transitEditorPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            this.transitEditorPanel.gameObject.SetActive(true);

            HidePanels(State.TransitEditor, 0);
        }

        public void ShowDataPanel()
        {
            this.dataPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            this.dataPanel.gameObject.SetActive(true);

            HidePanels(State.Data, 1);
        }

        public void ShowSettingsPanel()
        {
            this.settingsPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            this.settingsPanel.gameObject.SetActive(true);

            HidePanels(State.Settings, 2);
        }

        public void ShowLineBuildingPanel()
        {
            this.lineBuildingPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            this.lineBuildingPanel.gameObject.SetActive(true);

            HidePanels(State.LineBuilding, 2);
        }

        public void ShowConstructionCost(decimal constructionCost = 0m, decimal monthlyCost = 0m)
        {
            constructionCostPanel.SetActive(true);
            constructionCostText.text = Translator.GetCurrency(constructionCost, true);
            monthlyCostText.text = Translator.GetCurrency(monthlyCost, true);
        }

        public void HideConstructionCost()
        {
            constructionCostPanel.SetActive(false);
        }

        public void ShowPopulationHeatmap()
        {
            var radiusMultiplier = 25;
            var populationMultiplier = 100;

            var maxOccupants = 0;
            var buildings = new List<Tuple<Vector2, int, float>>();

            foreach (var b in game.loadedMap.buildings)
            {
                var residentCount = b.GetOccupancyCount(OccupancyKind.Resident);
                if (b.type != Building.Type.Residential || residentCount == 0)
                {
                    continue;
                }

                maxOccupants = System.Math.Max(maxOccupants, residentCount * populationMultiplier);

                var radius = Mathf.Min((b.centroid - b.collisionRect.center).magnitude * radiusMultiplier, 250f);
                buildings.Add(Tuple.Create(b.centroid, residentCount * populationMultiplier, radius));
            }

            if (buildings.Count == 0)
            {
                return;
            }

            if (buildings.Count > Heatmap.MaxSize)
            {
                buildings.Sort((b1, b2) => b2.Item2.CompareTo(b1.Item2));
                buildings.RemoveRange(Heatmap.MaxSize, buildings.Count - Heatmap.MaxSize);
            }

            var properties = new Vector4[buildings.Count];

            var i = 0;
            foreach (var b in buildings)
            {
                properties[i++] = new Vector4(
                    b.Item1.x, b.Item1.y,
                    b.Item3, ((float)b.Item2 / maxOccupants));
            }

            game.loadedMap.heatmap.SetPositions(properties);
            game.loadedMap.heatmap.Show();
        }

        public void FadeScaleBar()
        {
            scaleBar.SetActive(false);
            scaleText.gameObject.SetActive(false);
        }

        public void UpdateScaleBar()
        {
            if (scaleBar == null)
            {
                return;
            }

            scaleBar.SetActive(true);
            scaleText.gameObject.SetActive(true);

            var maxX = mainCamera.ScreenToWorldPoint(new Vector3(100f, 0f, 0f)).x;
            var minX = mainCamera.ScreenToWorldPoint(new Vector3(0f, 0f, 0f)).x;

            var maxLength = maxX - minX;
            float scale;
            string scaleTxt;

            if (maxLength >= 10000f)
            {
                scaleTxt = "10km";
                scale = 10000f;
            }
            else if (maxLength >= 5000f)
            {
                scaleTxt = "5km";
                scale = 5000f;
            }
            else if (maxLength >= 2000f)
            {
                scaleTxt = "2km";
                scale = 2000f;
            }
            else if (maxLength >= 1000f)
            {
                scaleTxt = "1km";
                scale = 1000f;
            }
            else if (maxLength >= 500f)
            {
                scaleTxt = "500m";
                scale = 500f;
            }
            else if (maxLength >= 200f)
            {
                scaleTxt = "200m";
                scale = 200f;
            }
            else if (maxLength >= 100f)
            {
                scaleTxt = "100m";
                scale = 100f;
            }
            else if (maxLength >= 50f)
            {
                scaleTxt = "50m";
                scale = 50f;
            }
            else if (maxLength >= 20f)
            {
                scaleTxt = "20m";
                scale = 20f;
            }
            else if (maxLength >= 10f)
            {
                scaleTxt = "10m";
                scale = 10f;
            }
            else if (maxLength >= 5f)
            {
                scaleTxt = "5m";
                scale = 5f;
            }
            else
            {
                scaleTxt = "1m";
                scale = 1f;
            }

            scaleText.text = scaleTxt;

            var img = scaleBar.GetComponent<UnityEngine.UI.Image>();
            var rectTransform = img.rectTransform;

            rectTransform.sizeDelta = new Vector2(100 * (scale / maxLength), rectTransform.sizeDelta.y);

            fadeScaleBarTime = 3f;
        }
    }
}
