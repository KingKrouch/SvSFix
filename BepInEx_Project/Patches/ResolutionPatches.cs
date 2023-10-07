// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
// Mod Stuff
namespace SvSFix;

public partial class SvSFix
{
    [HarmonyPatch]
    public class ResolutionPatches
    {
        [HarmonyPatch(typeof(DbPlayerCore), nameof(DbPlayerCore.ApplyConfigScreen), new Type[] { typeof(FullScreenMode), typeof(Vector2Int) })]
        [HarmonyPrefix]
        public static bool ForceCustomResolution(FullScreenMode mode, Vector2Int size) // I do plan on revising this once I figure out how to unhardcode the resolution options. Gonna redirect that to writing to our config file.
        {
            if (!_bForceCustomResolution.Value) {
                Screen.SetResolution(size.x, size.y, DbPlayerCore.ConvertConfigScreenMode());
            }
            else {
                Screen.SetResolution(_iHorizontalResolution.Value, _iVerticalResolution.Value, DbPlayerCore.ConvertConfigScreenMode());
            }
            return false;
        }

        // For now, let's use this

        //public static int resolutionIndex = 0;
        //public static List<ResolutionManager.Resolution> list = ResolutionManager.ScreenResolutions().ToList<ResolutionManager.Resolution>();
        //[HarmonyPatch(typeof(GameScreen), nameof(GameScreen.EnumPattern), MethodType.Enumerator)]
        //[HarmonyTranspiler]
        //static IEnumerator customEnumPattern()
        //{

        //}
        //[HarmonyPatch(typeof(DbPlayerCore), "ApplyConfigScreen", new Type[] { typeof(FullScreenMode), typeof(Vector2Int) })]
        //[HarmonyPrefix]
        //public static bool ApplyConfigResolution()
        //{
        //Screen.SetResolution(SvSFix._iHorizontalResolution.Value, SvSFix._iVerticalResolution.Value, DbPlayerCore.ConvertConfigScreenMode());
        //return false;
        //}

        //[HarmonyPatch(typeof(GameUiConfigScreenResolutionList), "Entry")] // Modify Screen Resolution Selection
        //[HarmonyPrefix]
        //public static bool CustomResolutions(GameUiConfigScreenResolutionList __instance)
        //{
        //GameUiConfigDropDownList.Local local_ = this.local_;
        //if (local_ != null) {
        //local_.Heading.Renew(StrInterface.UI_CONFIG_LIST_GENERAL_SCREEN_RESOLUTION);
        //}
        //GameUiConfigDropDownList.Local local_2 = this.local_;
        //if (((local_2 != null) ? local_2.Listing : null) == null) {
        //return;
        //}
        //OSB.GetShared();
        //string @string = GameUiAccessor.GetString(StrInterface.UI_CONFIG_PARAM_RESOLUTION);
        //List<ResolutionManager.resolution> list = ResolutionManager.ScreenResolutions().ToList<ResolutionManager.resolution>();
        //for (int i = 0; i < list.Count; i++) {
        //this.local_.Listing.Add(OSB.Start.AppendFormat(@string, list[i].width, list[i].height));
        //}
        //this.local_.Entry();
        //return false;
        //}
    }
}