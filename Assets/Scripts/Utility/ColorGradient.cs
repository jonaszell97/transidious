using UnityEngine;
using System;

namespace Transidious
{
    public class ColorGradient : MonoBehaviour
    {
        GameController game;
        public Color low;
        public Color high;
        public float currentValue;
        float period;
        bool loopDown;
        new Renderer renderer;

        public void Initialize(GameController game)
        {
            this.game = game;
            this.renderer = this.gameObject.GetComponent<Renderer>();
            this.currentValue = -1;
        }

        public void Activate(Color low, Color high,
                             float period = 1f, bool loopDown = true)
        {
            this.low = low;
            this.high = high;
            this.period = period;
            this.loopDown = loopDown;
            this.currentValue = 0;
        }

        public void Stop()
        {
            this.currentValue = -1f;
            this.renderer.material = GameController.GetUnlitMaterial(low);
        }

        void Update()
        {
            if (this.currentValue.Equals(-1f))
            {
                return;
            }

            this.currentValue += Time.deltaTime;

            float intervalValue = this.currentValue - ((int)(this.currentValue / period) * period);
            float percentageValue = intervalValue / period;

            Color currentColor;
            bool down = intervalValue > (period * .5f);

            if (down)
            {
                currentColor = Color.Lerp(high, low, percentageValue);
            }
            else
            {
                currentColor = Color.Lerp(low, high, percentageValue);
            }

            this.renderer.material = GameController.GetUnlitMaterial(currentColor);
        }
    }
}