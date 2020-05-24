using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Transidious
{
    [RequireComponent(typeof(Button)), RequireComponent(typeof(TMP_Text))]
    public class UILocationLink : MonoBehaviour
    {
        /// The linked location.
        [SerializeField] Vector2 linkedLocation;

        /// The linked citizen.
        private Citizen linkedCitizen;

        /// The button component.
        public Button buttonComponent;

        /// The text mesh component.
        public TMP_Text textComponent;

        /// The movement speed.
        [SerializeField] float movementSpeed = 5f;
        [SerializeField] bool moving = false;

        /// Event listener before the movement starts.
        public Action preMoveListener;

        /// Event listener after the movement ends.
        public Action postMoveListener;

        public Color activeColor = new Color(193f / 255f, 214f / 255f, 1f, 1f);
        public Color inactiveColor = new Color(.3f, .3f, .3f);

        void Awake()
        {
            this.buttonComponent = GetComponent<Button>();
            this.buttonComponent.transition = Button.Transition.None;

            this.textComponent = GetComponent<TMP_Text>();

            Enable();

            this.buttonComponent.onClick.AddListener(() =>
            {
                preMoveListener?.Invoke();

                GameController.instance.input.StopFollowing();
                moving = true;

                var loc = linkedCitizen?.currentPosition ?? linkedLocation;
                var distance = (loc - (Vector2)Camera.main.transform.position).magnitude;
                UpdateSpeed(distance);
            });
        }

        public void SetLocation(Vector3 linkedLocation)
        {
            this.linkedLocation = linkedLocation;
        }
        
        public void SetLocation(Citizen c)
        {
            this.linkedCitizen = c;
        }

        public void Disable()
        {
            textComponent.color = inactiveColor;
            buttonComponent.enabled = false;
        }

        public void Enable()
        {
            textComponent.color = activeColor;
            buttonComponent.enabled = true;
        }

        void UpdateSpeed(float distance)
        {
            // Moving should always take about .5s.
            movementSpeed = distance * 2f;
        }

        void Update()
        {
            if (!moving)
            {
                return;
            }

            var loc = linkedCitizen?.currentPosition ?? linkedLocation;
            var newPos = Vector2.MoveTowards(Camera.main.transform.position, 
                                                     loc, 
                                                     movementSpeed * Time.deltaTime);

            if (newPos.Equals(linkedLocation))
            {
                moving = false;

                if (postMoveListener != null)
                {
                    postMoveListener();
                }
            }

            GameController.instance.input.SetCameraPosition(newPos);
        }
    }
}