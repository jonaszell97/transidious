using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public class WalkingCitizien : MonoBehaviour
    {
        SimulationController sim;
        public Citizien citizien;
        public float maxVelocity;
        public PathFollowingObject pathFollow;
        public PathFollowingObject.CompletionCallback callback;
        new SpriteRenderer renderer;

        public bool isFocused
        {
            get
            {
                return sim.citizienModal.citizien == citizien;
            }
        }

        public Color color
        {
            get
            {
                return renderer.color;
            }
            set
            {
                renderer.color = value;
            }
        }

        public Bounds Bounds
        {
            get
            {
                return renderer.bounds;
            }
        }

        public void Initialize(SimulationController sim, Citizien citizien, Color c)
        {
            this.renderer = GetComponent<SpriteRenderer>();
            renderer.color = c;

            if (citizien.age < 10)
            {
                this.maxVelocity = 5f;
            }
            else if (citizien.age < 30)
            {
                this.maxVelocity = 8f;
            }
            else if (citizien.age < 60f)
            {
                this.maxVelocity = 7f;
            }
            else
            {
                this.maxVelocity = 3f;
            }

            this.sim = sim;
            this.citizien = citizien;
            this.transform.SetLayer(MapLayer.Citiziens);
        }

        void PathDone(PathFollowingObject obj)
        {
            var oldPathFollow = pathFollow;
            this.gameObject.SetActive(false);

            if (callback != null)
            {
                callback(obj);
            }
            if (pathFollow == oldPathFollow)
            {
                pathFollow = null;
            }
        }

        public void FollowPath(List<Vector3> path, bool isFinalStep = false,
                               PathFollowingObject.CompletionCallback callback = null)
        {
            this.gameObject.SetActive(true);
            this.callback = callback;
            this.pathFollow = new PathFollowingObject(sim, this.gameObject, path,
                                                      maxVelocity, 0f, isFinalStep,
                                                      PathDone, citizien.activePath);
        }

        void FixedUpdate()
        {
            if (!sim.game.Paused && pathFollow != null)
            {
                pathFollow.FixedUpdate();
                citizien.currentPosition = transform.position;
            }

            if (isFocused)
            {
                UpdateUIPosition();
            }
        }

        public void Highlight()
        {
            //GetComponent<SpriteRenderer>().sprite = SpriteManager.instance.carSpritesOutlined[carModel];
        }

        public void Unhighlight()
        {
            //GetComponent<SpriteRenderer>().sprite = SpriteManager.instance.carSprites[carModel];
        }

        void UpdateUIPosition()
        {
            var modal = GameController.instance.sim.citizienModal;
            modal.modal.PositionAt(transform.position);
        }

        void OnMouseEnter()
        {
            this.Highlight();
        }

        void OnMouseExit()
        {
            this.Unhighlight();
        }

        public void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = GameController.instance.sim.citizienModal;
            modal.SetCitizien(this.citizien);

            modal.modal.PositionAt(transform.position);
            modal.modal.Enable();
        }
    }
}