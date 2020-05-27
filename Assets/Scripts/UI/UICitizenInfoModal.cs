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

        /// The car sprite.
        [SerializeField] Image carSprite;

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
            panel.AddItem("Occupation", "ui:citizen:occupation", "", "Sprites/ui_hardhat");
            
            // Current destination
            var dest = panel.AddItem("Destination", "ui:citizen:destination", 
                                     "", "Sprites/ui_destination");
            carSprite = dest.Icon;
            
            // Money
            panel.AddItem("Money", "ui:citizen:money", "", "Sprites/ui_money");

            // Happiness
            var hp = panel.AddProgressItem("Happiness", "ui:citizen:happiness", 
                                           "Sprites/ui_happy");
            happinessSprite = hp.Icon;

            // Energy & Work
            panel.AddProgressItem("Energy", "ui:citizen:energy", "Sprites/WIP");
            var workItem = panel.AddProgressItem("RemainingWork", "ui:citizen:remaining_work", "Sprites/WIP");
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
            
            _debugPanel.AddClickableItem("Next Car", "Next Car", Color.white, () =>
            {
                if (!(citizen.activePath?.IsDriving) ?? false)
                    return;

                // var next = GameController.instance.sim.trafficSim.GetNextCar(
                //     citizen.activePath._drivingState.drivingCar);
                var next = citizen.activePath._drivingCar.next;
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
                if (!(citizen.activePath?.IsDriving) ?? false)
                    return;

                var prev = citizen.activePath._drivingCar.prev;
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
                Utility.Dump(citizen.transitPreferences);
            });

            _debugPanel.AddClickableItem("HappinessInfluences", "Happiness Influences", Color.gray, () =>
            {
                foreach (var item in citizen.happinessInfluences)
                {
                    Utility.Dump(item);
                }
            });
            
            _debugPanel.AddClickableItem("Start Animation", "Start Animation", Color.blue, () =>
            {
                var sim = GameController.instance.sim;
                var c = citizen;
                sim.ScheduleEvent(sim.GameTime.AddMinutes(5), () => { c.SetHappiness(c.happiness + 2f); });
            });
            
            _debugPanel.AddClickableItem("Current Path", "Current Path", Color.blue, () =>
            {
                Debug.Log(citizen.activePath?.path?.ToString() ?? "no active path");
            });

            _debugPanel.AddClickableItem("Generate Schedule", "Generate Schedule", Color.blue, () =>
            {
                citizen.activePath = null;
                
                var prevDate = GameController.instance.sim.GameTime.Date;
                for (var i = 0; i < 10; ++i)
                {
                    Debug.Log(citizen.currentEvent.DebugDescription);

                    var nextDate = citizen.currentEvent.endTime.Date;
                    var newDay = prevDate != nextDate;

                    citizen.Update(citizen.currentEvent.endTime, newDay, nextDate - prevDate);
                }
            });
#endif
        }

        public void UpdateAll()
        {
            UpdateFrequentChanges();

            this.panel.SetValue("Age", citizen.age.ToString());
            this.panel.SetValue("Occupation", Translator.Get($"ui:citizen:occupation:{citizen.occupation.ToString()}"));

            var dst = citizen.CurrentDestination;
            if (citizen.activePath != null && dst != null)
            {
                if (citizen.car != null)
                {
                    carSprite.color = citizen.car.color;
                    carSprite.gameObject.SetActive(true);
                }

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
                carSprite.gameObject.SetActive(false);
            }

            // scheduleText.text = citizen.currentEvent.DebugDescription;
        }

        public void UpdateFrequentChanges()
        {
            this.panel.SetProgress("Happiness", citizen.happiness / 100f);
            this.panel.SetProgress("Energy", citizen.energy / 100f);
            this.panel.SetProgress("RemainingWork", citizen.remainingWork / 100f);
            this.panel.SetValue("Money", Translator.GetCurrency(citizen.money, true));

            if (citizen.happiness < 50)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[0];
            }
            else if (citizen.happiness < 75)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[1];
            }
            else
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[2];
            }

            if (citizen.activePath?.IsDriving ?? false)
            {
                this._debugPanel.SetValue("Distance From Start",
                    $"{citizen.activePath._drivingCar.distanceFromStart:n2} m");

                this._debugPanel.SetValue("Distance To Intersection",
                    $"{citizen.activePath._drivingCar.DistanceToIntersection:n2} m");
                
                this._debugPanel.SetValue("Velocity",
                    $"{citizen.activePath._drivingCar.CurrentVelocity.RealTimeMPS:n2} m/s");

                if (citizen.activePath._drivingCar.next != null)
                {
                    this._debugPanel.SetValue("Distance To Next",
                        $"{citizen.activePath._drivingCar.next.distanceFromStart - citizen.activePath._drivingCar.distanceFromStart:n2} m");
                }
                else
                {
                    this._debugPanel.SetValue("Distance To Next", "-");
                }

                this._debugPanel.SetValue("Step",
                    citizen.activePath.currentStep?.GetType().Name ?? "None");
            }
        }

        public void SetCitizen(Citizen citizen)
        {
            this.citizen = citizen;
            this.modal.SetTitle(citizen.Name);

            UpdateAll();

            if (citizen.activePath != null)
            {
                GameController.instance.input.FollowObject(
                    citizen.activePath.gameObject, InputController.FollowingMode.Center);
            }
        }
    }
}