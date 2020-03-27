using Transidious;
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
            if (assetName.StartsWith("Assets/Resources/Maps/", System.StringComparison.CurrentCulture)
                && assetName.Contains(".png")
                && !assetName.Contains("minimap"))
            {
                ImportMapTileSprite(assetName);
                continue;
            }
            if (!assetName.StartsWith("Assets/Resources/Maps", System.StringComparison.CurrentCulture)
            || !assetName.Contains(".prefab"))
            {
                continue;
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

            foreach (var route in prefab.GetComponentsInChildren<Route>())
            {
                route.gameObject.transform.SetParent(null);
            }
            foreach (var stop in prefab.GetComponentsInChildren<Stop>())
            {
                stop.gameObject.transform.SetParent(null);
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, assetName);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    static void ImportMapTileSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        if (importer.alphaSource == TextureImporterAlphaSource.None)
            return;

        Debug.Log($"changing {path} import settings");

        importer.mipmapEnabled = true;
        importer.maxTextureSize = 4096;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.filterMode = FilterMode.Trilinear;

        importer.textureCompression = TextureImporterCompression.Compressed;
        // importer.textureCompression = TextureImporterCompression.CompressedHQ;

        importer.SaveAndReimport();
    }
}