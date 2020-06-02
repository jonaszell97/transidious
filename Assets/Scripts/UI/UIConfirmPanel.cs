using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIConfirmPanel : MonoBehaviour
    {
        /// The confirmation dialog.
        public UIText confirmDialog;

        /// The cancel button.
        public Button cancelButton;

        /// The confirm button.
        public Button confirmButton;

        /// Show the dialog.
        public void Show(string key, UnityAction onConfirm, UnityAction onCancel = null)
        {
            confirmDialog.SetKey(key);

            cancelButton.onClick.RemoveAllListeners();
            if (onCancel != null)
            {
                cancelButton.onClick.AddListener(onCancel);
            }
            else
            {
                cancelButton.onClick.AddListener(this.Hide);
            }

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(onConfirm);
            confirmButton.onClick.AddListener(this.Hide);

            gameObject.SetActive(true);
            GameController.instance.mainUI.ShowOverlay();
        }

        /// Hide the dialog.
        public void Hide()
        {
            gameObject.SetActive(false);
            GameController.instance.mainUI.HideOverlay();
        }
    }
}