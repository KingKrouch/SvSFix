// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
// Game and Plugin Stuff
using IF.PhotoMode.Control;
using IF.PhotoMode.Imaging;
using IF.Steam;
using Steamworks;

// Mod Stuff
namespace SvSFix
{
    public partial class SvSFix
    {
        [HarmonyPatch]
        public class PhotoModePatches
        {
            // TODO:
            // 1. Remove Photo Mode height restrictions (or at least make them to the floor rather than an angle that can't look up skirts).
            // 2. Add camera tilting to Photo Mode
            // 3. Add a Steam Screenshot hook to the Photo Mode screenshot feature.
            // 4. Add character height and rotation control.
            // 5. Possibly investigate a free-cam that can be enabled at any point during dungeon exploration or combat.

            private static Vector3 positionBeforeClamping;

            [HarmonyPatch(typeof(Photo), nameof(Photo.Capture), new Type[] { typeof(Camera), typeof(Vector2Int) })]
            [HarmonyPrefix]
            public static bool SteamScreenshotHook(ref Camera target_camera, ref Vector2Int resolution)
            {
                bool initialized = SteamworksAccessor.IsSteamworksReady;
                if (initialized)
                {
                    // TODO: We need to find a way of getting the actual screenshot taken by the game. For now, we know how to write a Steam Screenshot at least.
                    RenderTexture snapshotRt = new RenderTexture(Screen.width, Screen.height, 24);
                    Texture2D snapshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    var oldTargetTexture = target_camera.targetTexture;
                    target_camera.targetTexture = snapshotRt;
                    target_camera.Render();
                    snapshot.ReadPixels(new Rect(0, 0, snapshotRt.width, snapshotRt.height), 0, 0); // Finally transfer the render target to our texture.
                    // Flip our texture (as it's upside down). TODO: Troubleshoot why this isn't working properly.
                    Color[] pixels = snapshot.GetPixels();
                    Array.Reverse(pixels);
                    snapshot.SetPixels(pixels);
                    // Now we finally let Steam's Screenshots API do its thing.
                    long snapshotSize = snapshot.GetRawTextureData().Length;
                    byte[] snapshotData = snapshot.GetRawTextureData();
                    SteamScreenshots.WriteScreenshot(snapshotData, (uint)snapshotSize, snapshot.width, snapshot.height);
                    target_camera.targetTexture = oldTargetTexture; // Sets our target texture back to its original after taking a screenshot.
                }
                return true;
            }

            //[HarmonyPatch(typeof(CameraControl), "SetPosition", new Type[] { typeof(Vector3) })]
            //[HarmonyPostfix] // This has to be a postfix apparently.
            public static void GrabPreclampedPosition(ref Vector3 position, CameraControl __instance)
            {
                //_log.LogInfo("Hooked Camera Clamping!"); // This hook seemingly works, I just need to figure out how to access private info.
                positionBeforeClamping = position;
            }

            //[HarmonyPatch(typeof(CameraControl), "ClampPosition", new Type[] { typeof(Vector3) })]
            //[HarmonyPostfix] // TODO: Figure out how to get the camera height clamping completely removed for photo mode.
            public static void RemoveCameraClamping(ref Vector3 position, CameraControl __instance)
            {
                position = positionBeforeClamping;
            }
        }
    }
}