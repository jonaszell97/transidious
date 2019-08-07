using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UILineModal : MonoBehaviour
    {
        /// The modal component.
        public UIModal modal;

        /// The line logo.
        public UILineLogo logo;

        /// The text components.
        public TMP_Text[] textComponents;

        /// The line color button.
        public Toggle lineColorToggle;

        /// The line color button image.
        public Image lineColorButtonImg;

        /// The line view component.
        public UILineView lineView;

        /// The color picker component.
        public ColorPicker colorPicker;

        /// The currently selected line.
        public Line selectedLine;

        void Start()
        {
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
                selectedLine.gradient = null;
                selectedLine.material.color = selectedLine.color;
                selectedLine = null;
            });

            colorPicker.onChange.AddListener((Color c) =>
            {
                selectedLine.gradient = null;
                selectedLine.SetColor(c);
                logo.SetColor(c);
                lineColorButtonImg.color = c;

                var textColor = Math.ContrastColor(c);
                var textComponent = logo.GetComponentInChildren<TMPro.TMP_Text>();
                textComponent.color = textColor;
                textComponent.outlineColor = textColor;
            });

            lineColorToggle.onValueChanged.AddListener((bool active) =>
            {
                if (active)
                {
                    lineView.gameObject.SetActive(false);
                    colorPicker.gameObject.SetActive(true);

                    // We need to this next frame, otherwise the position of the color picker won't be up-to-date. 
                    StartCoroutine(UpdateColorPickerNextFrame(colorPicker));
                }
                else
                {
                    lineView.gameObject.SetActive(true);
                    colorPicker.gameObject.SetActive(false);
                }
            });
        }

        public void SetLine(Line line, Route route = null)
        {
            this.selectedLine = line;

            Stop closestStop;
            if (route)
            {
                Vector2 mousePosWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var startDistance = (route.beginStop.location - mousePosWorld).magnitude;
                var endDistance = (route.endStop.location - mousePosWorld).magnitude;

                if (startDistance <= endDistance)
                {
                    closestStop = route.beginStop;
                }
                else
                {
                    closestStop = route.endStop;
                }
            }
            else
            {
                closestStop = line.stops[line.stops.Count / 2];
            }

            lineView.UpdateLayout(line, closestStop, 5);
            lineView.gameObject.SetActive(true);
            colorPicker.gameObject.SetActive(false);

            lineColorButtonImg.color = line.color;
            logo.SetLine(line);
        }

        IEnumerator UpdateColorPickerNextFrame(ColorPicker colorPicker)
        {
            yield return null;

            colorPicker.UpdateBoundingBoxes(true);
            colorPicker.SetColor(selectedLine.color);

            yield break;
        }

        IEnumerator UpdateModalPositionNextFrame(UIModal modal, Vector3 pos)
        {
            yield return null;

            modal.PositionAt(pos);

            yield break;
        }
    }
}