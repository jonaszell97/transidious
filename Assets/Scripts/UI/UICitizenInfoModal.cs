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

            panel.AddItem("Age", "ui:citizen:age", "", "Sprites/WIP");
            panel.AddItem("Occupation", "ui:citizen:occupation", "", "Sprites/WIP");
            
            var dest = panel.AddItem("Destination", "ui:citizen:destination", 
                                                             "", "Sprites/ui_car");
            carSprite = dest.Item2;
            
            panel.AddItem("Money", "ui:citizen:money", "", "Sprites/money");
            
            var hp = panel.AddItem("Happiness", "ui:citizen:happiness", 
                                                           "", "Sprites/ui_happy");
            happinessSprite = hp.Item2;
            
            panel.AddItem("Energy", "ui:citizen:energy", "", "Sprites/WIP");
            panel.AddItem("RemainingWork", "ui:citizen:remaining_work", "", "Sprites/WIP");

#if DEBUG
            var debugPanel = Instantiate(ResourceManager.instance.infoPanelCardPrefab, panel.transform.parent)
                .GetComponent<UIInfoPanel>();
            
            debugPanel.Initialize();

            debugPanel.AddClickableItem("Preferences", "Preferences", Color.gray, () =>
            {
                Utility.Dump(citizen.transitPreferences);
            });

            debugPanel.AddClickableItem("HappinessInfluences", "Happiness Influences", Color.gray, () =>
            {
                foreach (var item in citizen.happinessInfluences)
                {
                    Utility.Dump(item);
                }
            });
            
            debugPanel.AddClickableItem("Start Animation", "Start Animation", Color.blue, () =>
            {
                var sim = GameController.instance.sim;
                var c = citizen;
                sim.ScheduleEvent(sim.GameTime.AddMinutes(5), () => { c.SetHappiness(c.happiness + 2f); });
            });
            
            debugPanel.AddClickableItem("Current Path", "Current Path", Color.blue, () =>
            {
                Debug.Log(citizen.activePath?.path?.ToString() ?? "no active path");
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
                carSprite.color = citizen.car.color;
                carSprite.gameObject.SetActive(true);

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
        }

        public void UpdateFrequentChanges()
        {
            this.panel.SetValue("Happiness", $"{citizen.happiness:n2}%");
            this.panel.SetValue("Energy", $"{citizen.energy:n2}%");
            this.panel.SetValue("RemainingWork", $"{citizen.remainingWork:n2}%");
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