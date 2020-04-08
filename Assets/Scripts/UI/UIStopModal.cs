using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        public List<Tuple<Transform, UILineLogo, TMP_Text>> nextDepartures;

        /// The next departures card.
        [SerializeField] private Transform nextDeparturesCard;

        /// The departure item prefab.
        [SerializeField] private GameObject departureItemPrefab;

        public void Initialize()
        {
            nextDepartures = new List<Tuple<Transform, UILineLogo, TMP_Text>>();
            
            modal.Initialize();
            panel.Initialize();

            panel.AddItem("Waiting", "ui:transit:waiting_citizens", "", "Sprites/ui_citizen_head");

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
                
                if (nextDepartures.Count < lines.Count)
                {
                    var item = Instantiate(departureItemPrefab, nextDeparturesCard.transform).transform;
                    nextDepartures.Add(Tuple.Create(item, item.GetChild(0).GetComponent<UILineLogo>(),
                                                          item.GetChild(1).GetComponent<TMP_Text>()));
                }
            }
            
            lines.Sort((v1, v2) => v1.Item2.CompareTo(v2.Item2));

            if (lines.Count == 0)
            {
                nextDeparturesCard.gameObject.SetActive(false);
            }
            else
            {
                nextDeparturesCard.gameObject.SetActive(true);

                for (var i = 0; i < nextDepartures.Count; ++i)
                {
                    var dep = nextDepartures[i];
                    if (i >= lines.Count)
                    {
                        dep.Item1.gameObject.SetActive(false);
                        continue;
                    }

                    var line = lines[i];
                    var logo = dep.Item2;
                    logo.SetLine(line.Item1, true);

                    var diff = (line.Item2 - time).TotalMinutes;

                    string text;
                    if (diff < 1)
                    {
                        text = Translator.Get("ui:transit:date_now");
                    }
                    else if (diff < 60)
                    {
                        text = Translator.Get("ui:transit:in_x_mins", ((int)System.Math.Ceiling(diff)).ToString());

#if DEBUG
                        text += $"({Translator.GetDate(line.Item2, Translator.DateFormat.TimeShort)})";
#endif
                    }
                    else if (time.Date == line.Item2.Date)
                    {
                        text = Translator.GetDate(line.Item2, Translator.DateFormat.TimeShort);
                    }
                    else
                    {
                        text = Translator.GetDate(line.Item2, Translator.DateFormat.DateTimeShort);
                    }

                    dep.Item1.gameObject.SetActive(true);
                    dep.Item3.text = text;
                }
            }

            panel.SetValue("Waiting", stop.TotalWaitingCitizens.ToString());

#if DEBUG
            panel.SetValue("schedule", stop.GetSchedule(stop.lineData.First().Key).ToString());
#endif
        }
    }
}