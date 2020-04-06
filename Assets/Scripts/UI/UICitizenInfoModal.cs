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

        bool isFollowing;

        void Start()
        {
            modal.titleInput.interactable = false;
            modal.onClose.AddListener(() =>
            {
                this.citizen = null;
                
                if (isFollowing)
                {
                    GameController.instance.input.StopFollowing();
                }
            });

#if DEBUG
            panel.AddClickableItem("Preferences", "Preferences", Color.gray, () =>
            {
                Utility.Dump(citizen.transitPreferences);
            });

            panel.AddClickableItem("HappinessInfluences", "Happiness Influences", Color.gray, () =>
            {
                foreach (var item in citizen.happinessInfluences)
                {
                    Utility.Dump(item);
                }
            });
            
            panel.AddClickableItem("Start Animation", "Start Animation", Color.blue, () =>
            {
                var sim = GameController.instance.sim;
                var c = citizen;
                sim.ScheduleEvent(sim.GameTime.AddMinutes(5), () => { c.SetHappiness(c.happiness + 2f); });
            });
            
            panel.AddClickableItem("Current Path", "Current Path", Color.blue, () =>
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
                // var building = citizen.pointsOfInterest[citizen.CurrentDestination.Value];

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

            if (citizen.activePath?.IsDriving ?? false)
            {
                isFollowing = true;
                GameController.instance.input.FollowObject(
                    citizen.activePath.gameObject, InputController.FollowingMode.Center);
            }
        }
    }
}