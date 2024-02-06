using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using Game.UI.Title;

namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class GraphicsPatches
    {
        // An SSAO toggle for when the player is outside of gameplay or cutscenes, since it causes unnecessary GPU load.
        private static void ToggleSSAO(bool toggle)
        {
            var renderPipeline = QualitySettings.renderPipeline;
            Debug.Log("Render pipeline type: " + renderPipeline.GetType().ToString());
            if (renderPipeline is not UniversalRenderPipelineAsset) {
                Debug.LogError("Render pipeline is not of type UniversalRenderPipelineAsset.");
                return;
            }
            var asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            // Use reflection to access the 'rendererFeatures' property
            if (asset == null) return;
            var rendererDataList = asset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rendererDataList == null) {
                _log.LogError("RendererDataList returned Null.");
                return;
            }
            var scriptableRendererData = ((ScriptableRendererData[])rendererDataList?.GetValue(asset));
            if (scriptableRendererData is not { Length: > 0 }) return;
            foreach (var rendererData in scriptableRendererData) {
                foreach (var rendererFeature in rendererData.rendererFeatures.Where(rendererFeature => rendererFeature.name == "SSAO")) {
                    _log.LogInfo("SSAO Found! " + (toggle ? "Toggling On." : "Toggling Off."));
                    rendererFeature.SetActive(toggle);
                    return;
                }
            }
        }

        // Enable (or disable SSAO) based on if a map or a cutscene is being loaded.
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
        
        // Disable SSAO based on if on the title screen, or when a map or cutscene is unloaded.
        [HarmonyPatch(typeof(MapPlay), nameof(MapPlay.MapFree))]
        [HarmonyPatch(typeof(GameUiTitle), nameof(GameUiTitle.Ready))]
        [HarmonyPrefix]
        public static void MapReleaseHook()
        {
            ToggleSSAO(false);
            _log.LogInfo("Releasing Map");
        }

        // TODO: Figure out why this Grass Density modification is not working.
        [HarmonyPatch(typeof(MapEditTreeComponent), MethodType.Constructor)]
        [HarmonyPostfix]
        public static void GrassDensityPatch(MapEditTreeComponent __instance)
        {
            var parentTransform = __instance.gameObject.transform;
            var keepPercentage  = Mathf.Clamp(_grassDensity.Value / 100f, 0f, 1f);
            
            // Checks if the GameObject is named as "mobj" before making these changes.
            if (__instance.gameObject.name != "mobj") return;
            _log.LogInfo("Found MapEditTreeComponent with 'mobj' GameObject Name.");
            var grassObjects = (from Transform child in parentTransform let childGameObject = child.gameObject where childGameObject.name.Contains("grass") select child).ToList();
            // Randomly disable a percentage of grass objects.
            DisableRandomGrassObjects(grassObjects, keepPercentage);
        }

        private static void DisableRandomGrassObjects(List<Transform> grassObjects, float keepPercentage)
        {
            if (keepPercentage >= 1.0f) return;
            // Calculate the number of grass objects to keep.
            var keepCount = Mathf.RoundToInt(grassObjects.Count * keepPercentage);

            // Shuffle the grass objects list.
            grassObjects.Shuffle();

            // Enable the first 'keepCount' grass objects.
            for (var i = 0; i < keepCount; i++) {
                grassObjects[i].gameObject.SetActive(true);
            }
        }
    }
}

// A generic shuffle function for lists.
public static class ListExtensions
{
    private static readonly System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1) {
            n--;
            var k     = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}