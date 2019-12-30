using UnityEngine;

namespace Transidious
{
    public class Heatmap : MonoBehaviour
    {
        public static readonly int MaxSize = 100;
        public Vector4[] properties;

        [SerializeField] Material material;

        public void SetPositions(Vector4[] properties)
        {
            this.properties = properties;
        }

        public void Show()
        {
            UpdateHeatmap();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void UpdateHeatmap()
        {
            Debug.Assert(properties.Length <= MaxSize);

            material.SetInt("_Points_Length", properties.Length);
            material.SetVectorArray("_Properties", properties);
        }
    }
}