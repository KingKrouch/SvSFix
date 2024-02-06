using System.Collections.Generic;
using Game.ADV.Local;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using Game.UI.Title;
using UnityEngine.Experimental.Rendering.Universal;

namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class GraphicsPatches
    {
        // TODO: Further test this to see if this fixes the framerate issues on the Steam Deck.
        public static void ToggleSSAO(bool toggle)
        {
            var renderPipeline = QualitySettings.renderPipeline;
            Debug.Log("Render pipeline type: " + renderPipeline.GetType().ToString());
            if (renderPipeline is not UniversalRenderPipelineAsset) {
                Debug.LogError("Render pipeline is not of type UniversalRenderPipelineAsset.");
                return;
            }
            var asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            // Use reflection to access the 'rendererFeatures' property
            FieldInfo rendererDataList = asset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rendererDataList == null) {
                _log.LogError("RendererDataList returned Null.");
                return;
            }
            var scriptableRendererData = ((ScriptableRendererData[])rendererDataList?.GetValue(asset));
            if (scriptableRendererData != null && scriptableRendererData.Length > 0) {
                foreach (var rendererData in scriptableRendererData) {
                    foreach (var rendererFeature in rendererData.rendererFeatures) {
                        if (rendererFeature.name == "SSAO") {
                            _log.LogInfo("SSAO Found! " + (toggle ? "Toggling On." : "Toggling Off."));
                            rendererFeature.SetActive(toggle);
                            return;
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
            switch (_screenSpaceAmbientOcclusion.Value) {
                case true:
                    ToggleSSAO(true);
                    break;
                case false:
                    ToggleSSAO(false);
                    break;
            }
            _log.LogInfo("Loading Map");
        }
        
        [HarmonyPatch(typeof(MapPlay), nameof(MapPlay.MapFree))]
        [HarmonyPatch(typeof(GameUiTitle), nameof(GameUiTitle.Ready))]
        [HarmonyPrefix]
        public static void MapReleaseHook()
        {
            ToggleSSAO(false);
            _log.LogInfo("Releasing Map");
        }
    }
}