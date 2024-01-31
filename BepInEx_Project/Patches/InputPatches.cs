// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
// Game and Plugin Stuff
using Game.Input.Local;
using Game.UI;
using Game.UI.Global.Resource;
using Game.UI.Local;
using IF.Steam;
using Steamworks;
// Mod Stuff
using SvSFix.Tools;

namespace SvSFix
{
    public struct Glyphs
    {
        public static Sprite GlyphA;
        public static Sprite GlyphB;
        public static Sprite GlyphX;
        public static Sprite GlyphY;
        public static Sprite GlyphDPadUp;
        public static Sprite GlyphDPadDown;
        public static Sprite GlyphDPadLeft;
        public static Sprite GlyphDPadRight;
        public static Sprite GlyphLsClick;
        public static Sprite GlyphLs;
        public static Sprite GlyphRsClick;
        public static Sprite GlyphRs;
        public static Sprite GlyphLb;
        public static Sprite GlyphLt;
        public static Sprite GlyphRb;
        public static Sprite GlyphRt;
        public static Sprite GlyphStart;
        public static Sprite GlyphBack;
        public static Sprite GlyphLsUp;
        public static Sprite GlyphLsDown;
        public static Sprite GlyphLsLeft;
        public static Sprite GlyphLsRight;
        public static Sprite[] GlyphLsUpDown = new Sprite[2];
        public static Sprite[] GlyphLsLeftRight = new Sprite[2];
        public static Sprite GlyphRsUp;
        public static Sprite GlyphRsDown;
        public static Sprite GlyphRsLeft;
        public static Sprite GlyphRsRight;
        public static Sprite[] GlyphRsUpDown = new Sprite[2];
        public static Sprite[] GlyphRsLeftRight = new Sprite[2];
        public static Sprite[] GlyphDPadUpDown = new Sprite[2];
        public static Sprite[] GlyphDPadLeftRight = new Sprite[2];
        public static Sprite[] GlyphDPadFull = new Sprite[4];
        // These are the glyphs that are going to be updated to cycle.
        public static Sprite GlyphLsUpDownPresent;
        public static Sprite GlyphLsLeftRightPresent;
        public static Sprite GlyphRsUpDownPresent;
        public static Sprite GlyphRsLeftRightPresent;
        public static Sprite GlyphDPadUpDownPresent;
        public static Sprite GlyphDPadLeftRightPresent;
        public static Sprite GlyphDPadFullPresent;
    }

    public partial class SvSFix
    {
        [HarmonyPatch]
        public class InputPatches
        {
            // TODO:
            // 1. Change Dungeon and Battle Cameras to always engage camera movement unless a menu is open.
            // 2. Rebind the mouse buttons (unless a menu is open) to Attack and Interact respectively in dungeons.
            // 3. Rebind the mouse buttons (unless a menu is open) to Primary Attack and Secondary Attack respectively in battle.
            // 4. Hide the mouse cursor in battles and dungeons unless a menu is open.
            // 5. Set up better rebinding defaults and investigate mouse (including mouse wheel) rebinding.
            // 6. Investigate adding hooks for individual sprites to load PlayStation or Switch equivalents if SteamInput is disabled (or returns a unknown controller type).
            // 7. Implement an option that allows reversing Cross/Circle in menus, enabled by default on Nintendo Switch controllers.
            // 8. Get Simultaneous KB/M + Controller input working, so the Steam Deck and Steam Controller trackpads are accounted for.

            public static GameObject advInputMgrObject;
            public static InputManager advInputMgrComponent;
            
            public static SpriteAtlas iconInputPS4;
            public static SpriteAtlas iconInputPS5;
            
            private static void SearchForAB()
            {
                string spriteAtlasNameToFind = "icon_input_PC";
                // Get all loaded AssetBundles
                var loadedAssetBundles = AssetBundle.GetAllLoadedAssetBundles();

                foreach (var loadedBundle in loadedAssetBundles)
                {
                    // Load all asset names in the current AssetBundle
                    var assetNames = loadedBundle.GetAllAssetNames();

                    // Check if the SpriteAtlas with the desired name is present in this AssetBundle
                    foreach (var assetName in assetNames)
                    {
                        if (assetName.EndsWith(spriteAtlasNameToFind, System.StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log("Found SpriteAtlas " + spriteAtlasNameToFind + " in AssetBundle " + loadedBundle.name);

                            // You can perform additional actions here, such as storing the reference to the AssetBundle
                            // or loading the SpriteAtlas itself if needed.
                        }
                    }
                }
            }
            
            private static void LoadSpriteAtlas()
            {
                // Get all loaded AssetBundles
                var loadedAssetBundles = AssetBundle.GetAllLoadedAssetBundles();
                // Define the name of the AssetBundle you want to access
                const string targetAssetBundleName = "interfaceglobal_assets_all.bundle";
                // Find the target AssetBundle by its name
                var targetAssetBundle = loadedAssetBundles.FirstOrDefault(assetBundle => assetBundle.name == targetAssetBundleName);
                // Check if the target AssetBundle was found
                if (targetAssetBundle != null) {
                    // Load assets from the target AssetBundle
                    iconInputPS4 = targetAssetBundle.LoadAsset<SpriteAtlas>("icon_input_PS4");
                    iconInputPS5 = targetAssetBundle.LoadAsset<SpriteAtlas>("icon_input_PS5");
                    // Now you can use iconInputPS4 and iconInputPS5 as needed
                }
                else {
                    // Try loading it into memory manually.
                    var assetBundlePath = Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", targetAssetBundleName);
                    var targetAssetBundleNew = AssetBundle.LoadFromFile(assetBundlePath);
                    if (targetAssetBundleNew != null) {
                        iconInputPS4 = targetAssetBundleNew.LoadAsset<SpriteAtlas>("icon_input_PS4");
                        iconInputPS5 = targetAssetBundleNew.LoadAsset<SpriteAtlas>("icon_input_PS5");
                    }
                    else
                    {
                        // Try to manually load them.
                        iconInputPS4 = Resources.Load<SpriteAtlas>($"Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4");
                        iconInputPS5 = Resources.Load<SpriteAtlas>($"Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5");
                        if (iconInputPS4 == null || iconInputPS5 == null) {
                            Debug.LogError("Failed to load AssetBundle: " + assetBundlePath);
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(Game.UI.Global.Resource.GameUiGlobalResource), nameof(Game.UI.Global.Resource.GameUiGlobalResource))]
            public static void Load(Game.UI.Global.Resource.GameUiGlobalResource __instance)
            {
                //if (__instance.IconInput == null)
                //{
                    //__instance.IconInput = new GameUiIcon.Input();
                    //__instance.IconInput?.Load("Assets/Project/AppData/Game/Interface/Icon/icon_input_PC.spriteatlas");
                //}
            }

            //private static AssetBundle spriteAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "interfaceglobal_assets_all.bundle"));
            //public static SpriteAtlas iconInputPS4 = spriteAssetBundle.LoadAsset<SpriteAtlas>("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4");
            //public static SpriteAtlas iconInputPS5 = spriteAssetBundle.LoadAsset<SpriteAtlas>("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5");

            //public static SpriteAtlas iconInputPS4 = Resources.Load($"Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4") as SpriteAtlas;
            //public static SpriteAtlas iconInputPS5 = Resources.Load($"Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5") as SpriteAtlas;

            // So both GameInput and World Manager seemingly have stuff that toggles the mouse cursor.
            [HarmonyPatch(typeof(GameInput), nameof(GameInput.RenewMouseCursorVisible))]
            [HarmonyPrefix]
            public static bool MouseCursorPatch(GameInput __instance, GameInput.EnumDevice device) // My hunch is that this may let me turn on/off the mouse cursor.
            {
                return true;
            }

            [HarmonyPatch(typeof(GameInputAccessor), "CurrentDevice", MethodType.Getter)]
            [HarmonyPostfix]
            public static void CustomCurrentDevice(ref GameInput.EnumDevice __result)
            {
                __result = _confInputType switch
                {
                    EInputType.Automatic => SingletonMonoBehaviour<GameInput>.Instance.Device,
                    EInputType.KBM => GameInput.EnumDevice.kKeyboard,
                    EInputType.Controller => GameInput.EnumDevice.kGamepad,
                    _ => SingletonMonoBehaviour<GameInput>.Instance.Device
                };
            }

            // GameUiBattleCommandMenu.InputKey probably has what we are looking for regarding custom controller prompt injection.

            [HarmonyPatch(typeof(SteamworksAccessor), nameof(SteamworksAccessor.Initialize))]
            [HarmonyPostfix]
            public static void SteamworksInitExtra()
            {
                advInputMgrObject = new GameObject
                {
                    name = "AdvancedInputManager",
                    transform =
                    {
                        position = new Vector3(0, 0, 0),
                        rotation = Quaternion.identity
                    }
                };
                DontDestroyOnLoad(advInputMgrObject);
                advInputMgrComponent = advInputMgrObject.AddComponent<InputManager>();
            }

            //[HarmonyPatch(typeof(GameUiIcon), "Setup")]
            //[HarmonyPrefix]
            //public static void ChangeConfirmButton()
            //{
            //SingletonMonoBehaviour<LibInput>.Instance.IsConfirmButtonX() = true;
            //}

            [HarmonyPatch(typeof(GameUiIcon.Input), "GetSprite", new Type[] { typeof(EnumIcon) })]
            [HarmonyPostfix]
            public static void GetSprite(EnumIcon icon, ref Sprite __result)
            {
                __result = GetGlyph(icon, __result);
            }

            static Sprite GetGlyph(EnumIcon icon, Sprite original) // TODO: Figure out why the keyboard prompt square after switching from controller input disappears.
            {
                Sprite result = new Sprite();
                if (advInputMgrComponent != null) // Checks if our input manager component is null before checking.
                {
                    if (advInputMgrComponent.steamInputInitialized && !_bDisableSteamInput.Value)
                    {
                        if (SteamInput.GetConnectedControllers(advInputMgrComponent.inputHandles) <= 0) return original;
                        result = icon switch
                        {
                            EnumIcon.PAD_ENTER      => Glyphs.GlyphA,
                            EnumIcon.PAD_BACK       => Glyphs.GlyphB,
                            EnumIcon.PAD_BUTTON_L   => Glyphs.GlyphX,
                            EnumIcon.PAD_BUTTON_U   => Glyphs.GlyphY,
                            EnumIcon.PAD_BUTTON_R   => Glyphs.GlyphB,
                            EnumIcon.PAD_BUTTON_D   => Glyphs.GlyphA,
                            EnumIcon.PAD_MOVE       => Glyphs.GlyphLs,
                            EnumIcon.PAD_MOVE_ALL   => Glyphs.GlyphLs,
                            EnumIcon.PAD_MOVE_L     => Glyphs.GlyphDPadRight,
                            EnumIcon.PAD_MOVE_U     => Glyphs.GlyphDPadUp,
                            EnumIcon.PAD_MOVE_R     => Glyphs.GlyphDPadDown,
                            EnumIcon.PAD_MOVE_D     => Glyphs.GlyphDPadLeft,
                            EnumIcon.PAD_MOVE_LR    => Glyphs.GlyphDPadLeftRightPresent,
                            EnumIcon.PAD_MOVE_UD    => Glyphs.GlyphDPadUpDownPresent,
                            EnumIcon.PAD_L1         => Glyphs.GlyphLb,
                            EnumIcon.PAD_R1         => Glyphs.GlyphRb,
                            EnumIcon.PAD_L2         => Glyphs.GlyphLt,
                            EnumIcon.PAD_R2         => Glyphs.GlyphRt,
                            EnumIcon.PAD_L3         => Glyphs.GlyphLsClick,
                            EnumIcon.PAD_R3         => Glyphs.GlyphRsClick,
                            EnumIcon.PAD_L_STICK    => Glyphs.GlyphLs,
                            EnumIcon.PAD_L_STICK_L  => Glyphs.GlyphLsLeft,
                            EnumIcon.PAD_L_STICK_U  => Glyphs.GlyphLsUp,
                            EnumIcon.PAD_L_STICK_R  => Glyphs.GlyphLsRight,
                            EnumIcon.PAD_L_STICK_D  => Glyphs.GlyphLsDown,
                            EnumIcon.PAD_L_STICK_LR => Glyphs.GlyphLsLeftRightPresent,
                            EnumIcon.PAD_L_STICK_UD => Glyphs.GlyphLsUpDownPresent,
                            EnumIcon.PAD_R_STICK    => Glyphs.GlyphRs,
                            EnumIcon.PAD_R_STICK_L  => Glyphs.GlyphRsLeft,
                            EnumIcon.PAD_R_STICK_U  => Glyphs.GlyphRsUp,
                            EnumIcon.PAD_R_STICK_R  => Glyphs.GlyphRsRight,
                            EnumIcon.PAD_R_STICK_D  => Glyphs.GlyphRsDown,
                            EnumIcon.PAD_R_STICK_LR => Glyphs.GlyphRsLeftRightPresent,
                            EnumIcon.PAD_R_STICK_UD => Glyphs.GlyphLsUpDownPresent,
                            EnumIcon.PAD_CREATE     => original,
                            EnumIcon.PAD_OPTIONS    => Glyphs.GlyphStart,
                            EnumIcon.PAD_TOUCH      => Glyphs.GlyphBack,
                            EnumIcon.PAD_SELECT     => Glyphs.GlyphBack,
                            EnumIcon.PAD_START      => Glyphs.GlyphStart,
                            _ => original
                        };
                    }
                    else {
                        if (UnityEngine.InputSystem.Gamepad.all[0].device == null) return result;
                        if (iconInputPS5 == null || iconInputPS4 == null) {
                            LoadSpriteAtlas();
                        }
                        switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                        {
                            case DualSenseGamepadHID: // TODO: Fix broken null references, so there's no errors with loading sprites.
                                if (iconInputPS5 != null)
                                {
                                    result = icon switch
                                    {
                                        EnumIcon.PAD_ENTER      => iconInputPS5.GetSprite("button_batu"),
                                        EnumIcon.PAD_BACK       => iconInputPS5.GetSprite("button_maru"),
                                        EnumIcon.PAD_BUTTON_L   => iconInputPS5.GetSprite("button_sikaku"),
                                        EnumIcon.PAD_BUTTON_U   => iconInputPS5.GetSprite("button_sankaku"),
                                        EnumIcon.PAD_BUTTON_R   => iconInputPS5.GetSprite("button_maru"),
                                        EnumIcon.PAD_BUTTON_D   => iconInputPS5.GetSprite("button_batu"),
                                        EnumIcon.PAD_MOVE       => original,
                                        EnumIcon.PAD_MOVE_ALL   => original,
                                        EnumIcon.PAD_MOVE_L     => original,
                                        EnumIcon.PAD_MOVE_U     => original,
                                        EnumIcon.PAD_MOVE_R     => original,
                                        EnumIcon.PAD_MOVE_D     => original,
                                        EnumIcon.PAD_MOVE_LR    => original,
                                        EnumIcon.PAD_MOVE_UD    => original,
                                        EnumIcon.PAD_L1         => iconInputPS5.GetSprite("L1"),
                                        EnumIcon.PAD_R1         => iconInputPS5.GetSprite("R1"),
                                        EnumIcon.PAD_L2         => iconInputPS5.GetSprite("L2"),
                                        EnumIcon.PAD_R2         => iconInputPS5.GetSprite("R2"),
                                        EnumIcon.PAD_L3         => iconInputPS5.GetSprite("L3"),
                                        EnumIcon.PAD_R3         => iconInputPS5.GetSprite("R3"),
                                        EnumIcon.PAD_L_STICK    => original,
                                        EnumIcon.PAD_L_STICK_L  => original,
                                        EnumIcon.PAD_L_STICK_U  => original,
                                        EnumIcon.PAD_L_STICK_R  => original,
                                        EnumIcon.PAD_L_STICK_D  => original,
                                        EnumIcon.PAD_L_STICK_LR => original,
                                        EnumIcon.PAD_L_STICK_UD => original,
                                        EnumIcon.PAD_R_STICK    => original,
                                        EnumIcon.PAD_R_STICK_L  => original,
                                        EnumIcon.PAD_R_STICK_U  => original,
                                        EnumIcon.PAD_R_STICK_R  => original,
                                        EnumIcon.PAD_R_STICK_D  => original,
                                        EnumIcon.PAD_R_STICK_LR => original,
                                        EnumIcon.PAD_R_STICK_UD => original,
                                        EnumIcon.PAD_CREATE     => iconInputPS5.GetSprite("create"),
                                        EnumIcon.PAD_OPTIONS    => iconInputPS5.GetSprite("options"),
                                        EnumIcon.PAD_TOUCH      => iconInputPS5.GetSprite("touch"),
                                        EnumIcon.PAD_SELECT     => iconInputPS5.GetSprite("touch"),
                                        EnumIcon.PAD_START      => iconInputPS5.GetSprite("start"),
                                        _ => original
                                    };
                                }
                                else { result = original; }
                                break;
                            case DualShock3GamepadHID:
                                result = original;
                                break;
                            case DualShock4GamepadHID:
                                if (iconInputPS4 != null) {
                                    result = icon switch {
                                        EnumIcon.PAD_ENTER      => iconInputPS4.GetSprite("button_batu"),
                                        EnumIcon.PAD_BACK       => iconInputPS4.GetSprite("button_maru"),
                                        EnumIcon.PAD_BUTTON_L   => iconInputPS4.GetSprite("button_sikaku"),
                                        EnumIcon.PAD_BUTTON_U   => iconInputPS4.GetSprite("button_sankaku"),
                                        EnumIcon.PAD_BUTTON_R   => iconInputPS4.GetSprite("button_maru"),
                                        EnumIcon.PAD_BUTTON_D   => iconInputPS4.GetSprite("button_batu"),
                                        EnumIcon.PAD_MOVE       => original,
                                        EnumIcon.PAD_MOVE_ALL   => original,
                                        EnumIcon.PAD_MOVE_L     => original,
                                        EnumIcon.PAD_MOVE_U     => original,
                                        EnumIcon.PAD_MOVE_R     => original,
                                        EnumIcon.PAD_MOVE_D     => original,
                                        EnumIcon.PAD_MOVE_LR    => original,
                                        EnumIcon.PAD_MOVE_UD    => original,
                                        EnumIcon.PAD_L1         => iconInputPS4.GetSprite("L1"),
                                        EnumIcon.PAD_R1         => iconInputPS4.GetSprite("R1"),
                                        EnumIcon.PAD_L2         => iconInputPS4.GetSprite("L2"),
                                        EnumIcon.PAD_R2         => iconInputPS4.GetSprite("R2"),
                                        EnumIcon.PAD_L3         => iconInputPS4.GetSprite("L3"),
                                        EnumIcon.PAD_R3         => iconInputPS4.GetSprite("R3"),
                                        EnumIcon.PAD_L_STICK    => original,
                                        EnumIcon.PAD_L_STICK_L  => original,
                                        EnumIcon.PAD_L_STICK_U  => original,
                                        EnumIcon.PAD_L_STICK_R  => original,
                                        EnumIcon.PAD_L_STICK_D  => original,
                                        EnumIcon.PAD_L_STICK_LR => original,
                                        EnumIcon.PAD_L_STICK_UD => original,
                                        EnumIcon.PAD_R_STICK    => original,
                                        EnumIcon.PAD_R_STICK_L  => original,
                                        EnumIcon.PAD_R_STICK_U  => original,
                                        EnumIcon.PAD_R_STICK_R  => original,
                                        EnumIcon.PAD_R_STICK_D  => original,
                                        EnumIcon.PAD_R_STICK_LR => original,
                                        EnumIcon.PAD_R_STICK_UD => original,
                                        EnumIcon.PAD_CREATE     => iconInputPS4.GetSprite("share"), // The big difference is the use of "share" instead of "create"
                                        EnumIcon.PAD_OPTIONS    => iconInputPS4.GetSprite("options"),
                                        EnumIcon.PAD_TOUCH      => iconInputPS4.GetSprite("touch"),
                                        EnumIcon.PAD_SELECT     => iconInputPS4.GetSprite("touch"),
                                        EnumIcon.PAD_START      => iconInputPS4.GetSprite("start"),
                                        _ => original
                                    };
                                }
                                else { result = original; }
                                break;
                            case SwitchProControllerHID:
                                result = original;
                                break;
                            case XInputControllerWindows:
                                // Change Nothing.
                                result = original;
                                break;
                            default:
                                result = original;
                                break;
                        }
                    }
                }
                return result;
            }
        }
    }
}