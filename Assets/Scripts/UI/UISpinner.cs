using UnityEngine;
using UnityEngine.UI;

namespace Transidious
{
    public class UISpinner : MonoBehaviour
    {
        public int speed = 20;

        void Awake()
        {
            GetComponent<Image>().color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
        }

        void Update()
        {
            transform.Rotate (new Vector3 (0, 0, 100) * Time.deltaTime * speed);
        }
    }
}