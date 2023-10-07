// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
// Game and Plugin Stuff
using Game.UI.MainMenu;
using Game.UI.MainMenu.Local;
// Mod Stuff
namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class BlackBarPatches
    {
        // TODO:
        // 1. Add hooks for fullscreen UI elements, ADV/VN segments (start and finish), and cutscenes (start and finish) to fade-in/fade-out our BlackBarController.
        // Seems like GameCinemaEventStart is responsible for cutscenes, try and investigate some patches that fade-in/fade-out our BlackBarController based on if a cutscene has started or finished.
        // Black Bar actors should be put on "GameUiDungeonFullMap/Canvas", "GameUiMainMenuBack/Cover/", "GameUiWorldMap"

        [HarmonyPatch(typeof(GameUiMainMenuController), nameof(GameUiMainMenuController.Close), new Type[]{typeof(bool)})]
        [HarmonyPostfix]
        public static void FadeOutBlackBars(GameUiMainMenuController __instance)
        {
            _log.LogInfo("Closed Pause Menu.");
            //_instance.StartCoroutine(controllerComponent.FadeOutBlackBars());
        }
        [HarmonyPatch(typeof(GameUiMainMenuController), nameof(GameUiMainMenuController.Open), new Type[]{typeof(bool), typeof(MenuContentsExceptionFlag)})]
        [HarmonyPostfix]
        public static void FadeInBlackBars(GameUiMainMenuController __instance)
        {
            _log.LogInfo("Opened Pause Menu.");
            //StartCoroutine(controllerComponent.FadeInBlackBars());
        }
    }
}