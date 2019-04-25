using UnityEngine;
using System.Collections.Generic;

public class ScreenShotMaker : MonoBehaviour
{
    public GameObject target;
 
    private RenderTexture renderTexture;
    private Camera renderCamera;
    private Vector4 bounds;

    public void MakeScreenshots(Map map)
    {
        gameObject.AddComponent(typeof(Camera));

        renderCamera = GetComponent<Camera>();
        renderCamera.enabled = true;
        renderCamera.cameraType = CameraType.Game;
        renderCamera.forceIntoRenderTexture = true;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = .5f;
        renderCamera.aspect = 1f;
        renderCamera.targetDisplay = 2;
        renderCamera.backgroundColor = Color.white;

        float cameraDistance = map.input.camera.transform.position.z;
        int resolution = 2048;

        bounds.w = map.input.minX + 4.5f;
        bounds.x = map.input.maxX - 4.5f;
        bounds.y = map.input.minY + 4.5f;
        bounds.z = map.input.maxY - 4.5f;

        float minX = map.input.minX + 4.5f;
        float maxX = map.input.maxX - 4.5f;
        float minY = map.input.minY + 4.5f;
        float maxY = map.input.maxY - 4.5f;

        float pixelWidth = renderCamera.WorldToScreenPoint(new Vector3(maxX, minY)).x
            - renderCamera.WorldToScreenPoint(new Vector3(minX, minY)).x;

        float pixelHeight = renderCamera.WorldToScreenPoint(new Vector3(maxX, maxY)).y
            - renderCamera.WorldToScreenPoint(new Vector3(minX, minY)).y;

        int neededXTiles = (int)Mathf.Ceil(pixelWidth / (float)resolution);
        int neededYTiles = (int)Mathf.Ceil(pixelHeight / (float)resolution);

        renderTexture = new RenderTexture(resolution, resolution, 24);
        renderCamera.targetTexture = renderTexture;

        float xdiff = bounds.x - bounds.w;
        float ydiff = bounds.z - bounds.y;
        float xstep = xdiff / neededXTiles;
        float ystep = ydiff / neededYTiles;

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
                Texture2D virtualPhoto = new Texture2D(resolution, resolution,
                                                       TextureFormat.RGB24, false);

                RenderTexture.active = renderTexture;

                float initialXPos = bounds.w + x * xstep;
                float initialYPos = bounds.y + y * ystep;

                float finalXPos = initialXPos + xstep;
                float finalYPos = initialYPos + ystep;

                for (float i = initialXPos, xPos = 0; i < finalXPos; i += xstep, xPos++)
                {
                    for (float j = initialYPos, yPos = 0; j < finalYPos; j += ystep, yPos++)
                    {
                        gameObject.transform.position = new Vector3(i + renderCamera.aspect * renderCamera.orthographicSize, j + renderCamera.aspect * renderCamera.orthographicSize, cameraDistance);

                        renderCamera.Render();

                        virtualPhoto.ReadPixels(new Rect(0, 0, resolution, resolution), (int)xPos * resolution, (int)yPos * resolution);
                    }
                }

                RenderTexture.active = null;

                byte[] bytes = virtualPhoto.EncodeToPNG();
                System.IO.File.WriteAllBytes("Assets/Resources/Maps/" + map.name + "/" + x.ToString() + "_" + y.ToString() + ".png", bytes);
            }
        }

        /*
        int xRes = Mathf.RoundToInt(((bounds.x - bounds.w) / (renderCamera.aspect * renderCamera.orthographicSize * 2 * renderCamera.aspect)));
        int yRes = Mathf.RoundToInt(((bounds.z - bounds.y) / (renderCamera.aspect * renderCamera.orthographicSize * 2 * renderCamera.aspect)));

        int neededXTiles = (int)Mathf.Ceil((float)xRes / (float)resolution);
        int neededYTiles = (int)Mathf.Ceil((float)yRes / (float)resolution);

        float xdiff = bounds.x - bounds.w;
        float ydiff = bounds.z - bounds.y;
        float xstep = xdiff / neededXTiles;
        float ystep = ydiff / neededYTiles;

        System.IO.Directory.CreateDirectory("Assets/Resources/Maps/" + map.name);

        for (int x = 0; x < neededXTiles; ++x)
        {
            for (int y = 0; y < neededYTiles; ++y)
            {
                Texture2D virtualPhoto = new Texture2D(resolution, resolution,
                                                       TextureFormat.RGB24, false);

                RenderTexture.active = renderTexture;

                float initialXPos = bounds.w + x * xstep;
                float initialYPos = bounds.y + y * ystep;

                float finalXPos = initialXPos + xstep;
                float finalYPos = initialYPos + ystep;

                for (float i = initialXPos, xPos = 0; i < finalXPos; i += renderCamera.aspect * renderCamera.orthographicSize * 2, xPos++)
                {
                    for (float j = initialYPos, yPos = 0; j < finalYPos; j += renderCamera.aspect * renderCamera.orthographicSize * 2, yPos++)
                    {
                        gameObject.transform.position = new Vector3(i + renderCamera.aspect * renderCamera.orthographicSize, j + renderCamera.aspect * renderCamera.orthographicSize, cameraDistance);

                        renderCamera.Render();

                        virtualPhoto.ReadPixels(new Rect(0, 0, resolution, resolution), (int)xPos * resolution, (int)yPos * resolution);
                    }
                }

                RenderTexture.active = null;

                byte[] bytes = virtualPhoto.EncodeToPNG();
                System.IO.File.WriteAllBytes("Assets/Resources/Maps/" + map.name + "/" +  x.ToString() + "_" + y.ToString() + ".png", bytes);
            }
        }*/
    }
}