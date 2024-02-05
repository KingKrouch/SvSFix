using System.Collections.Generic;
using Game.ADV.Local;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class GraphicsPatches
    {
        // TODO: Figure out why this won't work.
        public static void ToggleSSAO(bool toggle)
        {
            // Let's adjust some of the Render Pipeline Settings during runtime.
            var asset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            if (asset != null)
            {
                // Use reflection to access internal field 'm_RendererDataList'
                FieldInfo rendererDataListField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);

                if (rendererDataListField != null)
                {
                    // Get the current UniversalRendererData
                    UniversalRendererData[] rendererDataList = (UniversalRendererData[])rendererDataListField.GetValue(asset);

                    if (rendererDataList != null && rendererDataList.Length > 0)
                    {
                        // Iterate through renderer data list
                        foreach (UniversalRendererData rendererData in rendererDataList)
                        {
                            // Get the current rendering features
                            List<ScriptableRendererFeature> renderingFeatures = rendererData.rendererFeatures;

                            // Find the SSAO feature using reflection and toggle it off
                            foreach (ScriptableRendererFeature feature in renderingFeatures)
                            {
                                if (feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
                                {
                                    FieldInfo enabledField = feature.GetType().GetField("m_Enabled", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (enabledField != null)
                                    {
                                        enabledField.SetValue(feature, toggle);
                                        switch (toggle)
                                        {
                                            case true:
                                                Debug.Log("SSAO has been toggled on!");
                                                break;
                                            case false:
                                                Debug.Log("SSAO has been toggled off!");
                                                break;
                                        }
                                        return; // Exit once we found and toggled it
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // This should be a lazy fix for removing SSAO from the world map and 2D stuff that doesn't pertain to a map.
        [HarmonyPatch(typeof(MapPlay), nameof(MapPlay.SetUp))]
        [HarmonyPrefix]
        public static void MapLoadHook()
        {
            ToggleSSAO(true);
            _log.LogInfo("Loading Map");
        }
        
        [HarmonyPatch(typeof(MapPlay), nameof(MapPlay.MapFree))]
        [HarmonyPrefix]
        public static void MapReleaseHook()
        {
            ToggleSSAO(false);
            _log.LogInfo("Releasing Map");
        }
    }
}