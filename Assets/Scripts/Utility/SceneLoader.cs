using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Transidious
{
    public class SceneLoader : MonoBehaviour
    {
        /// The scene to load asynchronously.
        public string scene;

        public TMPro.TMP_Text loadingText;
        bool loading = false;

        void Update()
        {
            if (!loading)
            {
                loading = true;
                StartCoroutine(LoadSceneAsync());
            }
            else
            {
                var newColor = Mathf.PingPong(Time.time, 255f);
                loadingText.color = new Color(newColor, newColor, newColor);
            }
        }

        IEnumerator LoadSceneAsync()
        {
            yield return null;

            var loadingOp = SceneManager.LoadSceneAsync(scene);
            while (!loadingOp.isDone)
            {
                yield return null;
            }

            yield break;
        }
    }
}