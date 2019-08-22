using UnityEngine;
using System;

namespace Transidious
{
    public class ScreenShotMaker : MonoBehaviour
    {
        private static ScreenShotMaker _instance;

        public static ScreenShotMaker Instance
        {
            get
            {
                if (_instance == null)
                {
                    var prefab = Resources.Load("Prefabs/ScreenShotMaker") as GameObject;
                    var obj = Instantiate(prefab);

                    _instance = obj.GetComponent<ScreenShotMaker>();
                }

                return _instance;
            }
        }

        [System.Serializable]
        public struct ScreenShotInfo
        {
            public int xTiles;
            public int yTiles;
            public float tileSizeUnits;
            public float tileSizePixels;
        }

        public ScreenShotInfo MakeScreenshot(Map map, int resolution = 2048)
        {
            float cameraDistance = Camera.main.transform.position.z;

            Camera renderCamera = GetComponent<Camera>();
            if (renderCamera == null)
            {
                renderCamera = gameObject.AddComponent<Camera>();
            }

            renderCamera.enabled = true;
            renderCamera.cameraType = CameraType.Game;
            renderCamera.forceIntoRenderTexture = true;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = 500f;
            renderCamera.aspect = 1.0f;
            renderCamera.targetDisplay = 2;
            renderCamera.backgroundColor = new Color(249f / 255f, 245f / 255f, 237f / 255f, 1);

            // Account for border thickness.
            var minX = map.minX - 100f;
            var maxX = map.maxX + 100f;
            var minY = map.minY - 100f;
            var maxY = map.maxY + 100f;

            int xRes = Mathf.RoundToInt(resolution * ((maxX - minX) / (renderCamera.aspect * renderCamera.orthographicSize * 2 * renderCamera.aspect)));
            int yRes = Mathf.RoundToInt(resolution * ((maxY - minY) / (renderCamera.aspect * renderCamera.orthographicSize * 2 / renderCamera.aspect)));

            float maxRes = 4096f;
            int neededXTiles = (int)Mathf.Ceil((float)xRes / maxRes);
            int neededYTiles = (int)Mathf.Ceil((float)yRes / maxRes);

            float xstep = (float)(maxX - minX) / neededXTiles;
            float ystep = (float)(maxY - minY) / neededYTiles;
            float step = Mathf.Max(xstep, ystep);

            int textureSize = System.Math.Max(xRes / neededXTiles, yRes / neededYTiles);

            var renderTexture = new RenderTexture(resolution, resolution, 24);
            renderCamera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;

            var directory = "Assets/Resources/Maps/" + map.name;
            System.IO.Directory.CreateDirectory(directory);

            var di = new System.IO.DirectoryInfo(directory);
            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }

            for (int x = 0; x < neededXTiles; ++x)
            {
                for (int y = 0; y < neededYTiles; ++y)
                {
                    Texture2D virtualPhoto = new Texture2D(
                        textureSize, textureSize, TextureFormat.RGB24, false);

                    float currentX = minX + x * step;
                    float currentY = minY + y * step;
                    float currentMaxX = currentX + step;
                    float currentMaxY = currentY + step;

                    for (float i = currentX, xPos = 0; i < currentMaxX; i += renderCamera.aspect * renderCamera.orthographicSize * 2, xPos++)
                    {
                        for (float j = currentY, yPos = 0; j < currentMaxY; j += renderCamera.aspect * renderCamera.orthographicSize * 2, yPos++)
                        {
                            gameObject.transform.position = new Vector3(i + renderCamera.aspect * renderCamera.orthographicSize, j + renderCamera.aspect * renderCamera.orthographicSize, cameraDistance);

                            renderCamera.Render();
                            virtualPhoto.ReadPixels(new Rect(0, 0, resolution, resolution), (int)xPos * resolution, (int)yPos * resolution);
                        }
                    }

                    byte[] bytes = virtualPhoto.EncodeToPNG();
                    System.IO.File.WriteAllBytes(directory + "/" + x + "_" + y + ".png", bytes);

                    // Debug.Log(neededXTiles);
                    // Debug.Log(neededYTiles);

                    // throw new System.Exception();
                }
            }

            RenderTexture.active = null;
            renderCamera.targetTexture = null;

            return new ScreenShotInfo
            {
                xTiles = neededXTiles,
                yTiles = neededYTiles,
                tileSizePixels = textureSize,
                tileSizeUnits = Mathf.Max((maxX - minX) / neededXTiles, (maxY - minY) / neededYTiles),
            };
        }

        public byte[] MakeScreenshotSingle(Map map, int resolution = 1024)
        {
            map.boundaryOutlineObj.SetActive(false);

            var prevMaterial = map.boundaryBackgroundObj.GetComponent<MeshRenderer>().sharedMaterial;
            map.boundaryBackgroundObj.GetComponent<MeshRenderer>().sharedMaterial =
                GameController.GetUnlitMaterial(Color.white);

            float cameraDistance = map.input.camera.transform.position.z;

            Camera renderCamera = GetComponent<Camera>();
            if (renderCamera == null)
            {
                renderCamera = gameObject.AddComponent<Camera>();
            }

            renderCamera.enabled = true;
            renderCamera.cameraType = CameraType.Game;
            renderCamera.forceIntoRenderTexture = true;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = 650f;
            renderCamera.aspect = 1.0f;
            renderCamera.targetDisplay = 2;

            var renderTexture = new RenderTexture(resolution, resolution, 24);
            renderCamera.targetTexture = renderTexture;

            var minX = map.minX;
            var maxX = map.maxX;
            var minY = map.minY;
            var maxY = map.maxY;

            // var directory = "Assets/Resources/Maps/" + map.name;
            // System.IO.Directory.CreateDirectory(directory);

            // var di = new System.IO.DirectoryInfo(directory);
            // foreach (var file in di.GetFiles())
            // {
            //     file.Delete();
            // }

            int xRes = Mathf.RoundToInt(resolution * ((maxX - minX) / (renderCamera.aspect * renderCamera.orthographicSize * 2 * renderCamera.aspect)));
            int yRes = Mathf.RoundToInt(resolution * ((maxY - minY) / (renderCamera.aspect * renderCamera.orthographicSize * 2 / renderCamera.aspect)));

            Texture2D virtualPhoto = new Texture2D(xRes, yRes, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;

            for (float i = minX, xPos = 0; i < maxX;
                 i += renderCamera.aspect * renderCamera.orthographicSize * 2, xPos++)
            {
                for (float j = minY, yPos = 0; j < maxY;
                     j += renderCamera.aspect * renderCamera.orthographicSize * 2, yPos++)
                {
                    gameObject.transform.position = new Vector3(i + renderCamera.aspect *
                        renderCamera.orthographicSize, j + renderCamera.aspect *
                        renderCamera.orthographicSize, cameraDistance);

                    renderCamera.Render();
                    virtualPhoto.ReadPixels(new Rect(0, 0, resolution, resolution),
                        (int)xPos * resolution, (int)yPos * resolution);
                }
            }

            RenderTexture.active = null;
            renderCamera.targetTexture = null;

            var bytes = virtualPhoto.EncodeToPNG();

#if DEBUG
            System.IO.File.WriteAllBytes("/Users/Jonas/Downloads/" + map.name + ".png", bytes);
            System.IO.File.WriteAllBytes("/Users/Jonas/Downloads/" + map.name + ".jpg",
                virtualPhoto.EncodeToJPG());
#endif

            map.boundaryBackgroundObj.SetActive(true);
            map.boundaryBackgroundObj.GetComponent<MeshRenderer>().sharedMaterial = prevMaterial;

            return bytes;
        }
    }
}