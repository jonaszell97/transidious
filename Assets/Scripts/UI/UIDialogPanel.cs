using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIDialogPanel : MonoBehaviour
    {
        /// A single item of dialog.
        public struct DialogItem
        {
            /// The localization key of the message.
            public string Message;

            /// The icon to display.
            public string Icon;

            /// The localization key of the confirm button ('Next' or 'Confirm' by default).
            public string ConfirmKey;

            /// The rect transform to highlight, if any.
            public Tuple<RectTransform, float, bool, bool> Highlight;
        }

        /// Reference to the main UI.
        public MainUI mainUI;

        /// The dialog text.
        public UIText text;

        /// The icon.
        public Image icon;

        /// The back button.
        public UIButton backButton;

        /// The next button.
        public UIButton nextButton;

        /// Setup and display the dialog panel.
        public void Show(DialogItem[] items, UnityAction onCompletion = null)
        {
            for (var i = 0; i < items.Length; ++i)
            {
                if (items[i].ConfirmKey == null)
                {
                    if (i == items.Length - 1)
                    {
                        items[i].ConfirmKey = "ui:confirm";
                    }
                    else
                    {
                        items[i].ConfirmKey = "ui:next";
                    }
                }
            }

            backButton.button.onClick.RemoveAllListeners();
            nextButton.button.onClick.RemoveAllListeners();
            
            backButton.gameObject.SetActive(false);

            var current = 0;
            backButton.button.onClick.AddListener(() =>
            {
                Debug.Assert(current > 0);

                if (--current == 0)
                {
                    backButton.gameObject.SetActive(false);
                }

                Display(items[current]);
            });

            bool wasPaused = GameController.instance.EnterPause(true);
            nextButton.button.onClick.AddListener(() =>
            {
                if (current == items.Length - 1)
                {
                    onCompletion?.Invoke();
                    Hide();
                    mainUI.highlightOverlay.Hide();
                    mainUI.HideOverlay();

                    if (!wasPaused)
                    {
                        GameController.instance.ExitPause(true);
                    }
                    else
                    {
                        GameController.instance.UnblockPause();
                    }

                    return;
                }

                backButton.gameObject.SetActive(true);

                ++current;
                Display(items[current]);
            });

            Display(items[0]);
            gameObject.SetActive(true);
        }

        /// Hide the dialog panel.
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        /// Display a dialog item.
        private void Display(DialogItem item)
        {
            text.SetKey(item.Message);
            icon.sprite = SpriteManager.GetSprite(item.Icon);
            nextButton.text.SetKey(item.ConfirmKey);

            if (item.Highlight != null)
            {
                mainUI.highlightOverlay.Highlight(item.Highlight.Item1, item.Highlight.Item2, item.Highlight.Item3, item.Highlight.Item4);
                mainUI.HideOverlay();
            }
            else
            {
                mainUI.highlightOverlay.Hide();
                mainUI.ShowOverlay();
            }
        }
    }
}