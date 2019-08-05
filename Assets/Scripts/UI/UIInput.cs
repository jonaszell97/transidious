using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIInput : MonoBehaviour
    { 
        protected virtual void Awake()
        {
            var inputField = GetComponent<TMP_InputField>();
            Awake(inputField);
        }

        protected void Awake(TMP_InputField inputField)
        {
            inputField.onSelect.AddListener((string _) => {
                GameController.instance?.input?.DisableControls();
            });

            inputField.onDeselect.AddListener((string _) => {
                GameController.instance?.input?.EnableControls();
            });
        }
    }
}