using System.Linq;
using UnityEngine;

namespace Transidious
{
    public class UIStopModal : MonoBehaviour
    {
        /// The stop currently displayed by the modal.
        public Stop stop;

        /// Reference to the modal component.
        public UIModal modal;

        /// The info panel.
        public UIInfoPanel panel;

        void Start()
        {
#if DEBUG
            panel.AddItem("next_departure", "Next Departure", "");
#endif

            var maxCharacters = 100;
            modal.titleInput.interactable = true;

            modal.titleInput.onValidateInput = (string text, int charIndex, char addedChar) =>
            {
                if (text.Length + 1 >= maxCharacters)
                {
                    return '\0';
                }

                return addedChar;
            };

            modal.titleInput.onSubmit.AddListener((string newName) =>
            {
                if (newName.Length == 0 || newName.Length > maxCharacters)
                {
                    modal.titleInput.text = stop.name;
                    return;
                }

                stop.name = newName;
            });

            modal.onClose.AddListener(() =>
            {
                this.stop = null;
            });
        }

        public void SetStop(Stop stop)
        {
            this.stop = stop;
            this.modal.SetTitle(stop.name);

#if DEBUG
            panel.SetValue("next_departure", Translator.GetDate(
                stop.NextDeparture(stop.lineData.First().Key, GameController.instance.sim.GameTime),
                Translator.DateFormat.DateTimeShort));
#endif
        }
    }
}