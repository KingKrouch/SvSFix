// BepInEx and Harmony Stuff
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
// Unity and System Stuff
using UnityEngine;
// Mod Stuff
using SvSFix.Tools;

namespace SvSFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("neptunia-sisters-vs-sisters.exe")]
    public partial class SvSFix : BaseUnityPlugin
    {
        private static ManualLogSource _log;
        
        private void Awake()
        {
            SvSFix._log = Logger;
            // Plugin startup logic
            SvSFix._log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Reads or creates our config file.
            InitConfig();
            LoadGraphicsSettings(); //TODO: Figure out how to modify the shadow resolution.
            // Creates our custom components, and prints a log statement if it failed or not.
            var createdFramelimiter = InitializeFramelimiter();
            if (createdFramelimiter) {
                _log.LogInfo("Created Framelimiter.");
            }
            else { _log.LogError("Couldn't create Framelimiter Actor."); }
            // Finally, runs our UI and Framerate Patches.
            Harmony.CreateAndPatchAll(typeof(UIPatches));
            Harmony.CreateAndPatchAll(typeof(FrameratePatches));
            Harmony.CreateAndPatchAll(typeof(ResolutionPatches));
            Harmony.CreateAndPatchAll(typeof(FOVPatches));
            Harmony.CreateAndPatchAll(typeof(InputPatches));
            Harmony.CreateAndPatchAll(typeof(PhotoModePatches));
        }

        private static bool InitializeFramelimiter()
        {
            var frObject = new GameObject {
                name = "FramerateLimiter",
                transform = {
                    position = new Vector3(0, 0, 0),
                    rotation = Quaternion.identity
                }
            };
            DontDestroyOnLoad(frObject);
            var frLimiterComponent = frObject.AddComponent<FramerateLimitManager>();
            frLimiterComponent.fpsLimit = (double)Screen.currentResolution.refreshRate / _iFrameInterval.Value;
            return true;
        }
    }
}