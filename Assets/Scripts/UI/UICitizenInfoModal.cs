using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UICitizenInfoModal : MonoBehaviour
    {
        /// The citizen currently displayed by the modal.
        public Citizen citizen;

        /// Reference to the modal component.
        public UIModal modal;

        /// The info panel.
        public UIInfoPanel panel;

        /// The happiness sprite.
        public Image happinessSprite;

        /// The schedule text.
        [SerializeField] TMP_Text scheduleText;

#if DEBUG
        private UIInfoPanel _debugPanel;
#endif

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            
            modal.titleInput.interactable = false;
            modal.onClose.AddListener(() =>
            {
                this.citizen = null;
                GameController.instance.input.StopFollowing();
            });
            
            // Age & Occupation
            panel.AddItem("Age", "ui:citizen:age", "", "Sprites/ui_calendar");
            panel.AddItem("Occupation", "ui:citizen:occupation", "", "Sprites/ui_hammer");

            // Current destination
            panel.AddItem("Destination", "ui:citizen:destination", "", "Sprites/ui_destination");
            
            // Money
            panel.AddItem("Money", "ui:citizen:money", "", "Sprites/ui_money");

            // Happiness
            var hp = panel.AddProgressItem("Happiness", "ui:citizen:happiness", "Sprites/ui_happy");
            happinessSprite = hp.Icon;

            // Energy & Work
            panel.AddProgressItem("Energy", "ui:citizen:energy", "Sprites/ui_energy");
            var workItem = panel.AddProgressItem("RemainingWork", "ui:citizen:remaining_work", "Sprites/ui_work");
            workItem.ProgressBar.ReverseGradient = true;

#if DEBUG
            _debugPanel = Instantiate(ResourceManager.instance.infoPanelCardPrefab, panel.transform.parent)
                .GetComponent<UIInfoPanel>();
            
            _debugPanel.Initialize();

            _debugPanel.AddItem("Distance From Start", "Distance From Start", "");
            _debugPanel.AddItem("Step", "Step", "");

            _debugPanel.AddItem("Distance To Next", "Distance To Next", "");
            _debugPanel.AddItem("Distance To Intersection", "Distance To Intersection", "");
            _debugPanel.AddItem("Velocity", "Velocity", "");
            _debugPanel.AddItem("NextTurn", "Next Turn", "");
            _debugPanel.AddItem("Blocking", "Blocking Intersection", "");

            _debugPanel.AddClickableItem("Next Car", "Next Car", Color.white, () =>
            {
                if (!(citizen.ActivePath?.IsDriving) ?? false)
                    return;

                // var next = GameController.instance.sim.trafficSim.GetNextCar(
                //     citizen.activePath._drivingState.drivingCar);
                var next = citizen.ActivePath._drivingCar.Next;
                if (next != null)
                {
                    GameController.instance.input.MoveTowards(next.CurrentPosition);
                }
                else
                {
                    Debug.Log("No next car");
                }
            });

            _debugPanel.AddClickableItem("Prev Car", "Prev Car", Color.white, () =>
            {
                if (!(citizen.ActivePath?.IsDriving) ?? false)
                    return;

                var prev = citizen.ActivePath._drivingCar.Prev;
                if (prev != null)
                {
                    GameController.instance.input.MoveTowards(prev.CurrentPosition);
                }
                else
                {
                    Debug.Log("No prev car");
                }
            });
            
            _debugPanel.AddClickableItem("Preferences", "Preferences", Color.gray, () =>
            {
                Utility.Dump(citizen.TransitPreferences);
            });

            _debugPanel.AddClickableItem("HappinessInfluences", "Happiness Influences", Color.gray, () =>
            {
                foreach (var item in citizen.HappinessInfluences)
                {
                    Utility.Dump(item);
                }
            });
            
            _debugPanel.AddClickableItem("Start Animation", "Start Animation", Color.blue, () =>
            {
                var sim = GameController.instance.sim;
                var c = citizen;
                sim.ScheduleEvent(sim.GameTime.AddMinutes(5), () => { c.SetHappiness(c.Happiness + 2f); });
            });
            
            _debugPanel.AddClickableItem("Current Path", "Current Path", Color.blue, () =>
            {
                Debug.Log(citizen.ActivePath?.path?.ToString() ?? "no active path");
            });

            _debugPanel.AddClickableItem("Generate Schedule", "Generate Schedule", Color.blue, () =>
            {
                citizen.ActivePath = null;
                
                var prevDate = GameController.instance.sim.GameTime.Date;
                for (var i = 0; i < 10; ++i)
                {
                    Debug.Log(citizen.CurrentEvent.DebugDescription);

                    var nextDate = citizen.CurrentEvent.endTime.Date;
                    var newDay = prevDate != nextDate;

                    citizen.Update(citizen.CurrentEvent.endTime, newDay, nextDate - prevDate);
                }
            });
#endif
        }

        public void UpdateAll()
        {
            UpdateFrequentChanges();

            this.panel.SetValue("Age", citizen.Age.ToString());
            this.panel.SetValue("Occupation", Translator.Get($"ui:citizen:occupation:{citizen.occupation.ToString()}"));

            var dst = citizen.CurrentDestination;
            if (citizen.ActivePath != null && dst != null)
            {
                this.panel.ShowItem("Destination");

                var value = this.panel.GetValue("Destination");
                value.text = dst.Name;
                // value.text = Translator.Get("ui:citizen:destination:" + citizen.CurrentDestination.Value.ToString());

                var link = value.GetComponent<UILocationLink>();
                if (link == null)
                {
                    link = value.gameObject.AddComponent<UILocationLink>();
                }

                link.SetLocation(dst.Centroid);

                if (dst is Building b)
                {
                    link.postMoveListener = () =>
                    {
                        b.ActivateModal();
                    };
                }
                else if (dst is NaturalFeature nf)
                {
                    link.postMoveListener = () =>
                    {
                        nf.ActivateModal();
                    };
                }
            }
            else
            {
                this.panel.HideItem("Destination");
            }

            // scheduleText.text = citizen.currentEvent.DebugDescription;
        }

        public void UpdateFrequentChanges()
        {
            this.panel.SetProgress("Happiness", citizen.Happiness / 100f);
            this.panel.SetProgress("Energy", citizen.Energy / 100f);
            this.panel.SetProgress("RemainingWork", citizen.RemainingWork / 100f);
            this.panel.SetValue("Money", Translator.GetCurrency(citizen.Money, true));

            if (citizen.Happiness < 50)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[0];
            }
            else if (citizen.Happiness < 75)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[1];
            }
            else
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[2];
            }

            if (citizen.ActivePath?.IsDriving ?? false)
            {
                this._debugPanel.SetValue("Distance From Start",
                    $"{citizen.ActivePath._drivingCar.DistanceFromStart:n2} m");

                this._debugPanel.SetValue("Distance To Intersection",
                    $"{citizen.ActivePath._drivingCar.DistanceToIntersection:n2} m");
                
                this._debugPanel.SetValue("Velocity",
                    $"{citizen.ActivePath._drivingCar.CurrentVelocity.RealTimeMPS:n2} m/s");
                
                _debugPanel.SetValue("Blocking", citizen.ActivePath.idm.BlockingIntersection.ToString());
                
                if (citizen.ActivePath._drivingCar.Next != null)
                {
                    this._debugPanel.SetValue("Distance To Next",
                        $"{citizen.ActivePath._drivingCar.Next.DistanceFromStart - citizen.ActivePath._drivingCar.DistanceFromStart:n2} m");
                }
                else
                {
                    this._debugPanel.SetValue("Distance To Next", "-");
                }
                
                if (citizen.ActivePath._drivingCar.NextTurn != null)
                {
                    this._debugPanel.SetValue("NextTurn", citizen.ActivePath._drivingCar.NextTurn.Value.ToString());
                }
                else
                {
                    this._debugPanel.SetValue("NextTurn", "-");
                }

                this._debugPanel.SetValue("Step",
                    citizen.ActivePath.currentStep?.GetType().Name ?? "None");
            }
        }

        public void SetCitizen(Citizen citizen)
        {
            this.citizen = citizen;
            this.modal.SetTitle(citizen.Name);

            UpdateAll();

            if (citizen.ActivePath != null)
            {
                GameController.instance.input.FollowObject(
                    citizen.ActivePath.gameObject, InputController.FollowingMode.Center);
            }
        }
    }
}