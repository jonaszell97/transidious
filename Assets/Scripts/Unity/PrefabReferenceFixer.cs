#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CreateAssetMenu()]
public class PrefabReferenceFixer : ScriptableObject
{
    [MenuItem("Assets/Force Reserialize")]
    private static void ForceReserialize()
    {
        GameObject[] selection = Selection.gameObjects;
        string[] objectPaths = new string[selection.Length];

        for (int i = 0; i < selection.Length; ++i)
        {
            objectPaths[i] = AssetDatabase.GetAssetPath(selection[i]);
        }

        AssetDatabase.ForceReserializeAssets(objectPaths);
    }
}

#endif