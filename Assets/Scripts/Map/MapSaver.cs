using UnityEngine;
using System.Collections;

namespace Transidious
{
    public class NewMonoBehaviour : MonoBehaviour
    {
        public Map map;
        public string saveName;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnDestroy()
        {
            map.SaveToFile(saveName);
        }
    }
}
