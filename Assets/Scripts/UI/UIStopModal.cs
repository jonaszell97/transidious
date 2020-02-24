using System;
using System.Collections.Generic;
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

        /// The next departures.
        public TMPro.TMP_Text[] nextDepartures;

        void Start()
        {
#if DEBUG
            panel.AddItem("schedule", "Schedule");
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

            var time = GameController.instance.sim.GameTime;
            var lines = new List<Tuple<Line, DateTime>>();

            foreach (var line in stop.lineData)
            {
                lines.Add(Tuple.Create(line.Key, line.Value.schedule.GetNextDeparture(time)));
            }

            lines.Sort((v1, v2) => v1.Item2.CompareTo(v2.Item2));

            if (lines.Count == 0)
            {
                for (var i = 0; i < nextDepartures.Length; ++i)
                {
                    panel.HideItem($"Departures{i}");
                }
            }
            else
            {
                for (var i = 0; i < nextDepartures.Length; ++i)
                {
                    if (i >= lines.Count)
                    {
                        panel.HideItem($"Departures{i}");
                        continue;
                    }

                    panel.ShowItem($"Departures{i}");

                    var line = lines[i];
                    var logo = nextDepartures[i].GetComponentInChildren<UILineLogo>();
                    logo.SetLine(line.Item1, true);

                    nextDepartures[i].text = Translator.GetDate(line.Item2,
                        Translator.DateFormat.DateTimeShort);
                }
            }

#if DEBUG
            panel.SetValue("schedule", stop.GetSchedule(stop.lineData.First().Key).ToString());
#endif
        }
    }
}