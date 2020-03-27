using System;
using UnityEngine;

namespace Transidious
{
    public class ColorGradient : MonoBehaviour
    {
        public Color low;
        public Color high;

        private Action<Color> callback;
        float currentValue = -1;

        float period;
        bool loopDown;

        public Color CurrentColor;

        public void Initialize(Color low, Color high, Action<Color> callback = null,
                               float period = 1f, bool loopDown = true)
        {
            this.low = low;
            this.high = high;
            this.period = period;
            this.loopDown = loopDown;
            this.callback = callback;
            this.CurrentColor = loopDown ? high : low;
            this.currentValue = 0;
        }

        private void Update()
        {
            if (this.currentValue.Equals(-1f))
            {
                CurrentColor = low;
                callback?.Invoke(CurrentColor);
                return;
            }

            this.currentValue += Time.deltaTime;

            float intervalValue = this.currentValue - ((int)(this.currentValue / period) * period);
            float percentageValue = intervalValue / period;

            bool down = intervalValue > (period * .5f);
            if (down)
            {
                CurrentColor = Color.Lerp(high, low, percentageValue);
            }
            else
            {
                CurrentColor = Color.Lerp(low, high, percentageValue);
            }
            
            callback?.Invoke(CurrentColor);
        }
    }
}