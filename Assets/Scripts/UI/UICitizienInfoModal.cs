using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UICitizienInfoModal : MonoBehaviour
    {
        /// The citizien currently displayed by the modal.
        public Citizien citizien;

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
                this.citizien = null;
                
                if (isFollowing)
                {
                    GameController.instance.input.StopFollowing();
                }
            });

#if DEBUG
            panel.AddClickableItem("Preferences", "Preferences", Color.gray, () =>
            {
                Utility.Dump(citizien.transitPreferences);
            });

            panel.AddClickableItem("HappinessInfluences", "Happiness Influences", Color.gray, () =>
            {
                foreach (var item in citizien.happinessInfluences)
                {
                    Utility.Dump(item);
                }
            });
#endif
        }

        public void SetCitizien(Citizien citizien)
        {
            this.citizien = citizien;
            this.modal.SetTitle(citizien.Name);

            this.panel.SetValue("Age", citizien.age.ToString());
            this.panel.SetValue("Occupation", Translator.Get($"ui:citizien:occupation:{citizien.occupation.ToString()}"));
            this.panel.SetValue("Happiness", this.citizien.happiness + " %");
            this.panel.SetValue("Money", Translator.GetCurrency(this.citizien.money));

            if ((citizien.car?.IsDriving ?? false) && citizien.CurrentDestination.HasValue)
            {
                carSprite.color = citizien.car.color;
                carSprite.gameObject.SetActive(true);

                this.panel.ShowItem("Destination");

                var value = this.panel.GetValue("Destination");
                value.text = Translator.Get("ui:citizien:destination:" + citizien.CurrentDestination.Value.ToString());

                var link = value.GetComponent<UILocationLink>();
                var building = citizien.pointsOfInterest[citizien.CurrentDestination.Value];

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

            if (citizien.happiness < 50)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[0];
            }
            else if (citizien.happiness < 80)
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[1];
            }
            else
            {
                happinessSprite.sprite = SpriteManager.instance.happinessSprites[2];
            }

            if (citizien.car?.IsDriving ?? false)
            {
                isFollowing = true;
                GameController.instance.input.FollowObject(
                    citizien.car.gameObject, InputController.FollowingMode.Visible);
            }
        }
    }
}