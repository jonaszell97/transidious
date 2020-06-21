using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UILineModal : MonoBehaviour
    {
        /// The modal component.
        public UIModal modal;

        /// The color picker component.
        public ColorPicker colorPicker;

        /// The currently selected line.
        public Line selectedLine;

        /// The info panel.
        public UIInfoPanel infoPanel;

        /// The line view card.
        public Transform lineViewCard;
        
        /// The line view stops that we have already created.
        private List<Tuple<GameObject, Image, Image, UILocationLink, Image, Transform>> lineViewStops;

        /// Whether or not the line color was changed.
        private bool _colorChanged;

        /// The system icons.
        [SerializeField] Sprite[] systemIcons;

        /// Prefab for line view stops.
        [SerializeField] private GameObject lineViewStopPrefab;
        
        /// Prefab for line view crossing lines.
        [SerializeField] private GameObject lineViewCrossingLinesPrefab;

        public void Initialize()
        {
            lineViewStops = new List<Tuple<GameObject, Image, Image, UILocationLink, Image, Transform>>();
            
            modal.Initialize();
            infoPanel.Initialize();

            infoPanel.AddItem("System", "ui:transit:system", "", "Sprites/bus_logo");
            infoPanel.AddItem("WeeklyPassengers", "ui:transit:weekly_passengers", 
                              "", "Sprites/ui_citizen_head");

            infoPanel.AddItem("TripsSaved", "ui:transit:trips_saved");
            infoPanel.AddItem("Fare", "ui:transit:fare");

            var maxCharacters = 32;
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
                    modal.titleInput.text = selectedLine.name;
                    return;
                }

                selectedLine.name = newName;
            });

            modal.onClose.AddListener(() =>
            {
                Route.DeactivateGradient();
                
                selectedLine.material.color = selectedLine.color;
                selectedLine = null;
            });

            modal.onTabChange = tab =>
            {
                if (tab == 1)
                {
                    colorPicker.SetColor(selectedLine.color);
                }
                else if (tab == 2 && _colorChanged)
                {
                    UpdateLineView();
                    _colorChanged = false;
                }
            };

            colorPicker.Initialize();
            colorPicker.onChange.AddListener(c =>
            {
                Route.DeactivateGradient();

                selectedLine.SetColor(c);
                UpdateColor();

                _colorChanged = true;
            });

#if DEBUG
            infoPanel.AddItem("NumVehicles", "Vehicles");
#endif
        }

        void UpdateColor()
        {
            modal.headerImage.color = selectedLine.color;
            modal.titleInput.textComponent.color = Math.ContrastColor(selectedLine.color);
        }

        void UpdateLineView()
        {
            var stops = selectedLine.stops;
            while (lineViewStops.Count < stops.Count)
            {
                var inst = Instantiate(lineViewStopPrefab, lineViewCard);
                var tf = inst.transform;

                var data = Tuple.Create(
                    inst,
                    tf.GetChild(0).GetComponent<Image>(),
                    tf.GetChild(1).GetComponent<Image>(),
                    tf.GetChild(2).GetComponent<UILocationLink>(),
                    tf.GetChild(3).GetComponent<Image>(),
                    tf.GetChild(4).transform);

                data.Item5.GetComponent<Button>().onClick.AddListener(() =>
                {
                    data.Item4.buttonComponent.onClick.Invoke();
                });
                
                lineViewStops.Add(data);
            }

            var crossingLines = new List<Line>();
            for (var i = 0; i < lineViewStops.Count; ++i)
            {
                var data = lineViewStops[i];
                if (i >= stops.Count)
                {
                    data.Item1.SetActive(false);
                    continue;
                }

                var stop = stops[i];
                data.Item2.color = selectedLine.color;
                data.Item3.color = selectedLine.color;
                
                if (i == 0)
                {
                    data.Item2.enabled = false;
                    data.Item3.enabled = true;
                }
                else if (i == stops.Count - 1)
                {
                    data.Item2.enabled = true;
                    data.Item3.enabled = false;
                }
                else
                {
                    data.Item2.enabled = true;
                    data.Item3.enabled = true;
                }

                data.Item4.textComponent.text = stop.name;
                data.Item4.SetLocation(stop.location);

                foreach (var (line, _) in stop.lineData)
                {
                    if (line == selectedLine)
                        continue;
                    
                    crossingLines.Add(line);
                }
    
                if (crossingLines.Count == 0 && i > 0 && i < stops.Count - 1)
                {
                    data.Item5.sprite = SpriteManager.GetSprite("Sprites/stop_small_rect");
                    data.Item5.color = selectedLine.color;
                    data.Item6.gameObject.SetActive(false);

                    var rt = data.Item5.rectTransform;
                    rt.localScale = new Vector3(1f, 1.5f, 1f);
                }
                else
                {
                    data.Item5.sprite = SpriteManager.GetSprite("Sprites/stop_ring");
                    data.Item5.color = Color.white;
                    
                    var rt = data.Item5.rectTransform;
                    rt.localScale = Vector3.one;

                    var crossingLineObjects = data.Item6.childCount;
                    for (var j = 0; j < crossingLines.Count - crossingLineObjects; ++j)
                    {
                        Instantiate(lineViewCrossingLinesPrefab, data.Item6);
                    }

                    for (var j = 0; j < data.Item6.childCount; ++j)
                    {
                        var obj = data.Item6.GetChild(j);
                        if (j >= crossingLines.Count)
                        {
                            obj.gameObject.SetActive(false);
                            continue;
                        }

                        obj.gameObject.SetActive(true);
                        obj.GetComponent<UILineLogo>().SetLine(crossingLines[j], true);
                    }
                }
                
                data.Item1.SetActive(true);
                crossingLines.Clear();
            }
        }

        public void SetLine(Line line, Route route = null)
        {
            this.selectedLine = line;
            modal.titleInput.text = line.name;
            
            UpdateColor();
            UpdateLineView();

            var systemName = line.type.ToString().ToLower();
            var system = infoPanel.GetItem("System");
            system.Icon.sprite = systemIcons[(int)line.type];
            system.Value.text = Translator.Get($"transit:{systemName}");

            infoPanel.SetValue("WeeklyPassengers", line.weeklyPassengers.ToString());
            infoPanel.SetValue("TripsSaved", "0%");
            infoPanel.SetValue("Fare", Translator.GetCurrency(line.TripFare, true));

#if DEBUG
            infoPanel.SetValue("NumVehicles", line.vehicles.Count.ToString());
#endif
        }
    }
}