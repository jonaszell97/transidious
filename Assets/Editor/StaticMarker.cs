using UnityEditor;
using UnityEngine;

public class StaticMarker : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets,
                                       string[] deletedAssets,
                                       string[] movedAssets,
                                       string[] movedFromAssetPaths)
    {
        foreach (var assetName in importedAssets)
        {
            if (!assetName.StartsWith("Assets/Resources/Prefabs/Maps", System.StringComparison.CurrentCulture))
            {
                return;
            }

            var prefab = PrefabUtility.LoadPrefabContents(assetName);            
            var staticFlags = StaticEditorFlags.ContributeGI
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.OccludeeStatic
                | StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ReflectionProbeStatic;

            var done = false;
            foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (GameObjectUtility.GetStaticEditorFlags(mesh.gameObject).HasFlag(StaticEditorFlags.BatchingStatic))
                {
                    done = true;
                    break;
                }

                GameObjectUtility.SetStaticEditorFlags(mesh.gameObject, staticFlags);
            }

            if (done)
            {
                continue;
            }

            Debug.Log($"marking {assetName} as static...");

            foreach (var sprite in prefab.GetComponentsInChildren<SpriteRenderer>())
            {
                if (GameObjectUtility.GetStaticEditorFlags(sprite.gameObject).HasFlag(StaticEditorFlags.BatchingStatic))
                {
                    done = true;
                    break;
                }

                GameObjectUtility.SetStaticEditorFlags(sprite.gameObject, staticFlags);
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, assetName);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}