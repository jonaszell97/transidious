using UnityEngine;
using System.Collections;

namespace Transidious
{
    public class Text : MonoBehaviour
    {
        /// <summary>
        /// Reference to the text mesh.
        /// </summary>
        TextMesh textMesh;

        public void SetText(string txt)
        {
            textMesh.text = txt;
        }

        public void SetColor(Color c)
        {
            textMesh.color = c;
        }

        public void SetFontSize(int size)
        {
            textMesh.fontSize = size;
        }

        void Awake()
        {
            textMesh = GetComponent<TextMesh>();
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}