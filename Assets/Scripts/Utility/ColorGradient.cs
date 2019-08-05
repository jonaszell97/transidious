using UnityEngine;
using System;

namespace Transidious
{
    public class ColorGradient
    {
        public Color low;
        public Color high;
        public float currentValue = -1;

        float period;
        bool loopDown;

        public ColorGradient(Color low, Color high,
                             float period = 1f, bool loopDown = true)
        {
            this.low = low;
            this.high = high;
            this.period = period;
            this.loopDown = loopDown;
            this.currentValue = 0;
        }

        public Color CurrentColor
        {
            get
            {
                if (this.currentValue.Equals(-1f))
                {
                    return low;
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

                return currentColor;
            }
        }
    }
}