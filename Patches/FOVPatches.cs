// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
// Game and Plugin Stuff
using Game.Cinema;
// Mod Stuff
namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class FOVPatches
    {
        // TODO:
        // 1. Expose DungeonCamera's kFieldOfView parameter to a custom FOV option.
        // 2. Look into figuring out where the Battle Camera's FOV parameter is being stored, and then expose that too.
        // 3. Fix the "Object reference not set to an instance of an object" error with Cutscene FOV patching.
        // 4. Check BattleCameraBase or some other class for battle FOV adjustment.

        //[HarmonyPatch(typeof(DungeonCamera), nameof(DungeonCamera.CameraParameter.Copy))]
        //[HarmonyPostfix]
        //private static bool CustomDungeonFOV(ref DungeonCamera.CameraParameter out_parameter)
        //{
        //out_parameter.field_of_view_ = 90.0f; // The default is 45f.
        //return false;
        //}
            
        [HarmonyPatch(typeof(GameCinemaAccessor), nameof(GameCinemaAccessor.Set), new Type[]{ typeof(LibCinemaBaseStart) })]
        [HarmonyPostfix]
        private static void PatchCutsceneCameraFOV(ref LibCinemaBaseStart cinema)
        {
            if (!_bMajorAxisFOVScaling.Value && !_bPresentCutscenesWithOriginalAspectRatio.Value) return;
            var cameras = cinema.GetComponentsInChildren<Camera>(true);
            foreach (var c in cameras) {
                if (c != null) {
                    c.gateFit = Camera.GateFitMode.Overscan; // By default, cutscenes use the "Fill" GateFitMode, which is a terrible idea outside of 16:9. While setting it to "Overscan" does mildly affect composition, it's not a major concern.
                }
            }
        }

        [HarmonyPatch(typeof(SystemCamera3D), "Start")]
        [HarmonyPostfix]
        private static void CameraAspectRatioFixes()
        {
            if (!_bMajorAxisFOVScaling.Value) return;
            SystemCamera3D.GetCamera().usePhysicalProperties = true;
            SystemCamera3D.GetCamera().sensorSize = new Vector2(16f, 9f);
            SystemCamera3D.GetCamera().gateFit = Camera.GateFitMode.Overscan;
            _log.LogInfo("Modified SystemCamera3D Properties.");
        }
    }
}