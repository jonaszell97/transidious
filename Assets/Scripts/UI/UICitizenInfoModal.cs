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
#endif
        }

        public void SetCitizen(Citizen citizen)
        {
            this.citizen = citizen;
            this.modal.SetTitle(citizen.Name);

            this.panel.SetValue("Age", citizen.age.ToString());
            this.panel.SetValue("Occupation", Translator.Get($"ui:citizen:occupation:{citizen.occupation.ToString()}"));
            this.panel.SetValue("Happiness", this.citizen.happiness + " %");
            this.panel.SetValue("Money", Translator.GetCurrency(this.citizen.money));

            if ((citizen.activePath?.IsDriving ?? false) && citizen.CurrentDestination.HasValue)
            {
                carSprite.color = citizen.car.color;
                carSprite.gameObject.SetActive(true);

                this.panel.ShowItem("Destination");

                var value = this.panel.GetValue("Destination");
                value.text = Translator.Get("ui:citizen:destination:" + citizen.CurrentDestination.Value.ToString());

                var link = value.GetComponent<UILocationLink>();
                var building = citizen.pointsOfInterest[citizen.CurrentDestination.Value];

                link.SetLocation(building.centroid);
                link.postMoveListener = () =>
                {
                    building.ActivateModal();
                };
            }
            else
            {
                this.panel.HideItem("Destination");
                carSprite.gameObject.SetActive(false);
            }

            if (citizen.happiness < 50)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[0];
            }
            else if (citizen.happiness < 80)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[1];
            }
            else
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[2];
            }

            if (citizen.activePath?.IsDriving ?? false)
            {
                isFollowing = true;
                GameController.instance.input.FollowObject(
                    citizen.activePath.gameObject, InputController.FollowingMode.Center);
            }
        }
    }
}