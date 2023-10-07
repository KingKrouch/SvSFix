using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        try
        {
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);
            DirectoryInfo d = new DirectoryInfo(assetBundleDirectory);
            File.Delete("Assets/AssetBundles/AssetBundles");
            foreach (var file in d.GetFiles("*.manifest"))
            {
                file.Delete();
            }
            foreach (var file in d.GetFiles("*.manifest.meta"))
            {
                file.Delete();
            }
            AssetDatabase.Refresh();
            Debug.Log("<b>✔️ SUCCESSFULLY BUILDED ASSETBUNDLES ✔️</b>");
        }
        catch(Exception e)
        {
            Debug.LogError("AN ERROR OCCURED WHILE BUILDING THE ASSETBUNDLES!\n" + e.ToString());
        }
    }
}