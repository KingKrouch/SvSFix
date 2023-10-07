// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
// Game and Plugin Stuff
using Game.Input.Local;
using Game.UI;
using Game.UI.Local;
using IF.Steam;
using Steamworks;
// Mod Stuff
using KingKrouch.Utility.Helpers;

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

            //private static AssetBundle spriteAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "interfaceglobal_assets_all.bundle"));
            //public static SpriteAtlas iconInputPS4 = spriteAssetBundle.LoadAsset<SpriteAtlas>("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4");
            //public static SpriteAtlas iconInputPS5 = spriteAssetBundle.LoadAsset<SpriteAtlas>("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5");

            //public static SpriteAtlas iconInputPS4 = Resources.Load("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4") as SpriteAtlas;
            //public static SpriteAtlas iconInputPS5 = Resources.Load("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5") as SpriteAtlas;

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
                switch (_confInputType)
                {
                    case EInputType.Automatic:
                        __result = SingletonMonoBehaviour<GameInput>.Instance.Device;
                        break;
                    case EInputType.KBM:
                        __result = GameInput.EnumDevice.kKeyboard;
                        break;
                    case EInputType.Controller:
                        __result = GameInput.EnumDevice.kGamepad;
                        break;
                    default:
                        __result = SingletonMonoBehaviour<GameInput>.Instance.Device;
                        break;
                }
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
                        switch (icon)
                        {
                            case EnumIcon.PAD_ENTER:
                                result = Glyphs.GlyphA;
                                break;
                            case EnumIcon.PAD_BACK:
                                result = Glyphs.GlyphB;
                                break;
                            case EnumIcon.PAD_BUTTON_L:
                                result = Glyphs.GlyphX;
                                break; // Square
                            case EnumIcon.PAD_BUTTON_U:
                                result = Glyphs.GlyphY;
                                break; // Triangle
                            case EnumIcon.PAD_BUTTON_R:
                                result = Glyphs.GlyphB;
                                break; // Circle
                            case EnumIcon.PAD_BUTTON_D:
                                result = Glyphs.GlyphA;
                                break; // Cross
                            case EnumIcon.PAD_MOVE:
                                result = Glyphs.GlyphLs;
                                break;
                            case EnumIcon.PAD_MOVE_ALL:
                                result = Glyphs.GlyphLs;
                                break;
                            case EnumIcon.PAD_MOVE_L:
                                result = Glyphs.GlyphDPadRight;
                                break; // L/U/R/D for some reason is mixed up. Here's hoping the analog stick and D-Pad directions aren't as much of a cluster fuck.
                            case EnumIcon.PAD_MOVE_U:
                                result = Glyphs.GlyphDPadUp;
                                break; // Like seriously, what was the person who coded this smoking? I thought pot was illegal in Japan, maybe paint thinner or computer duster? Unless something's not translated and just good-ole "Engrish" at play.
                            case EnumIcon.PAD_MOVE_R:
                                result = Glyphs.GlyphDPadDown;
                                break;
                            case EnumIcon.PAD_MOVE_D:
                                result = Glyphs.GlyphDPadLeft;
                                break;
                            case EnumIcon.PAD_MOVE_LR:
                                result = Glyphs.GlyphDPadLeftRightPresent;
                                break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_MOVE_UD:
                                result = Glyphs.GlyphDPadUpDownPresent;
                                break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_L1:
                                result = Glyphs.GlyphLb;
                                break;
                            case EnumIcon.PAD_R1:
                                result = Glyphs.GlyphRb;
                                break;
                            case EnumIcon.PAD_L2:
                                result = Glyphs.GlyphLt;
                                break;
                            case EnumIcon.PAD_R2:
                                result = Glyphs.GlyphRt;
                                break;
                            case EnumIcon.PAD_L3:
                                result = Glyphs.GlyphLsClick;
                                break;
                            case EnumIcon.PAD_R3:
                                result = Glyphs.GlyphRsClick;
                                break;
                            case EnumIcon.PAD_L_STICK:
                                result = Glyphs.GlyphLs;
                                break;
                            case EnumIcon.PAD_L_STICK_L:
                                result = Glyphs.GlyphLsLeft;
                                break;
                            case EnumIcon.PAD_L_STICK_U:
                                result = Glyphs.GlyphLsUp;
                                break;
                            case EnumIcon.PAD_L_STICK_R:
                                result = Glyphs.GlyphLsRight;
                                break;
                            case EnumIcon.PAD_L_STICK_D:
                                result = Glyphs.GlyphLsDown;
                                break;
                            case EnumIcon.PAD_L_STICK_LR:
                                result = Glyphs.GlyphLsLeftRightPresent;
                                break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_L_STICK_UD:
                                result = Glyphs.GlyphLsUpDownPresent;
                                break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_R_STICK:
                                result = Glyphs.GlyphRs;
                                break;
                            case EnumIcon.PAD_R_STICK_L:
                                result = Glyphs.GlyphRsLeft;
                                break;
                            case EnumIcon.PAD_R_STICK_U:
                                result = Glyphs.GlyphRsUp;
                                break;
                            case EnumIcon.PAD_R_STICK_R:
                                result = Glyphs.GlyphRsRight;
                                break;
                            case EnumIcon.PAD_R_STICK_D:
                                result = Glyphs.GlyphRsDown;
                                break;
                            case EnumIcon.PAD_R_STICK_LR:
                                result = Glyphs.GlyphRsLeftRightPresent;
                                break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_R_STICK_UD:
                                result = Glyphs.GlyphLsUpDownPresent;
                                break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_CREATE:
                                result = original;
                                break;
                            case EnumIcon.PAD_OPTIONS:
                                result = Glyphs.GlyphStart;
                                break;
                            case EnumIcon.PAD_TOUCH:
                                result = Glyphs.GlyphBack;
                                break;
                            case EnumIcon.PAD_SELECT:
                                result = Glyphs.GlyphBack;
                                break;
                            case EnumIcon.PAD_START:
                                result = Glyphs.GlyphStart;
                                break;
                            default:
                                result = original;
                                break;
                        }
                    }
                    else
                    {
                        if (UnityEngine.InputSystem.Gamepad.all[0].device != null)
                        {
                            switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                            {
                                case DualSenseGamepadHID: // TODO: Fix broken null references, so there's no errors with loading sprites.
                                    //if (iconInputPS5 != null)
                                    //{
                                    //switch (icon) {
                                    //case EnumIcon.PAD_ENTER:      result = iconInputPS5.GetSprite("button_batu");    break;
                                    //case EnumIcon.PAD_BACK:       result = iconInputPS5.GetSprite("button_maru");    break;
                                    //case EnumIcon.PAD_BUTTON_L:   result = iconInputPS5.GetSprite("button_sikaku");  break; // Square
                                    //case EnumIcon.PAD_BUTTON_U:   result = iconInputPS5.GetSprite("button_sankaku"); break; // Triangle
                                    //case EnumIcon.PAD_BUTTON_R:   result = iconInputPS5.GetSprite("button_maru");    break; // Circle
                                    //case EnumIcon.PAD_BUTTON_D:   result = iconInputPS5.GetSprite("button_batu");    break; // Cross
                                    //case EnumIcon.PAD_MOVE:       result = original;                                      break;
                                    //case EnumIcon.PAD_MOVE_ALL:   result = original;                                      break;
                                    //case EnumIcon.PAD_MOVE_L:     result = original;                                      break; // L/U/R/D for some reason is mixed up.
                                    //case EnumIcon.PAD_MOVE_U:     result = original;                                      break; // Seriously, not gonna repeat what I said earlier like a broken record.
                                    //case EnumIcon.PAD_MOVE_R:     result = original;                                      break;
                                    //case EnumIcon.PAD_MOVE_D:     result = original;                                      break;
                                    //case EnumIcon.PAD_MOVE_LR:    result = original;                                      break;
                                    //case EnumIcon.PAD_MOVE_UD:    result = original;                                      break;
                                    //case EnumIcon.PAD_L1:         result = iconInputPS5.GetSprite("L1");             break;
                                    //case EnumIcon.PAD_R1:         result = iconInputPS5.GetSprite("R1");             break;
                                    //case EnumIcon.PAD_L2:         result = iconInputPS5.GetSprite("L2");             break;
                                    //case EnumIcon.PAD_R2:         result = iconInputPS5.GetSprite("R2");             break;
                                    //case EnumIcon.PAD_L3:         result = iconInputPS5.GetSprite("L3");             break;
                                    //case EnumIcon.PAD_R3:         result = iconInputPS5.GetSprite("R3");             break;
                                    //case EnumIcon.PAD_L_STICK:    result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_L:  result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_U:  result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_R:  result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_D:  result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_LR: result = original;                                      break;
                                    //case EnumIcon.PAD_L_STICK_UD: result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK:    result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_L:  result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_U:  result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_R:  result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_D:  result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_LR: result = original;                                      break;
                                    //case EnumIcon.PAD_R_STICK_UD: result = original;                                      break;
                                    //case EnumIcon.PAD_CREATE:     result = iconInputPS5.GetSprite("create");         break;
                                    //case EnumIcon.PAD_OPTIONS:    result = iconInputPS5.GetSprite("options");        break;
                                    //case EnumIcon.PAD_TOUCH:      result = iconInputPS5.GetSprite("touch");          break;
                                    //case EnumIcon.PAD_SELECT:     result = iconInputPS5.GetSprite("touch");          break;
                                    //case EnumIcon.PAD_START:      result = iconInputPS5.GetSprite("start");          break;
                                    //default:                      result = original;                                      break;
                                    //}
                                    //}
                                    //else {
                                    //result = original;
                                    //}
                                    //break;
                                case DualShock3GamepadHID:
                                    result = original;
                                    break;
                                case DualShock4GamepadHID:
                                    result = original;
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
                }
                return result;
            }
        }
    }
}