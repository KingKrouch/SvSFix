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
            // 6. Investigate adding Switch button prompts when using native controller support.
            // 7. Implement an option that allows reversing Cross/Circle in menus, enabled by default on Nintendo Switch controllers.
            // 8. Get Simultaneous KB/M + Controller input working, so the Steam Deck and Steam Controller trackpads are accounted for.

            public static GameObject advInputMgrObject;
            public static InputManager advInputMgrComponent;
            
            static GameUiIcon.Input PS4Prompt = new GameUiIcon.Input();
            static GameUiIcon.Input PS5Prompt = new GameUiIcon.Input();
            
            // Interestingly, when using a debug build, BepInEx just completely shits itself. I need to find out how to fix that. :-(
            [HarmonyPatch(typeof(GameUiGlobalResource), "Load")]
            [HarmonyPostfix]
            public static void LoadPSInputPrompts(GameUiGlobalResource __instance)
            {
                // Load the SpriteAtlases for the PS4/PS5 button prompts already in the game.
                PS4Prompt.Load("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS4.spriteatlas");
                PS5Prompt.Load("Assets/Project/AppData/Game/Interface/Icon/icon_input_PS5.spriteatlas");
            }

            [HarmonyPatch(typeof(GameUiGlobalResource), "Exec")]
            [HarmonyPostfix]
            public static void SetupPSInputPrompts(GameUiGlobalResource __instance)
            {
                PS4Prompt.Setup();
                PS5Prompt.Setup();
            }

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
            
            // TODO: Patch the menu UI stuff to recognize Cross as Confirm, and Circle as Back when the Japanese layout is used. Also default to it with Switch controllers.
            [HarmonyPatch(typeof(LibInput), nameof(LibInput.IsConfirmButtonX))]
            [HarmonyPrefix]
            public static bool ChangeConfirmButton(ref bool __result)
            {
                if (advInputMgrComponent != null) // Checks if our input manager component is null before checking.
                {
                    switch (advInputMgrComponent.steamInputInitialized && !_bDisableSteamInput.Value) {
                        case true: { // Check the current device through SteamInput first.
                            var inputTypeP1 = SteamInput.GetInputTypeForHandle(advInputMgrComponent.inputHandles[0]);
                            switch (inputTypeP1){
                                case ESteamInputType.k_ESteamInputType_XBox360Controller:
                                    _bJapaneseControllerLayout.Value = false;
                                    __result                         = true;
                                    return true;
                                case ESteamInputType.k_ESteamInputType_XBoxOneController:
                                    _bJapaneseControllerLayout.Value = false;
                                    __result                         = true;
                                    return true;
                                case ESteamInputType.k_ESteamInputType_GenericGamepad:
                                    _bJapaneseControllerLayout.Value = false;
                                    __result                         = true;
                                    return true;
                                case ESteamInputType.k_ESteamInputType_SwitchJoyConPair:
                                    _bJapaneseControllerLayout.Value = true;
                                    __result                         = false;
                                    return false;
                                case ESteamInputType.k_ESteamInputType_SwitchJoyConSingle:
                                    _bJapaneseControllerLayout.Value = true;
                                    __result                         = false;
                                    return false;
                                case ESteamInputType.k_ESteamInputType_SwitchProController:
                                    _bJapaneseControllerLayout.Value = true;
                                    __result                         = false;
                                    return false;
                                case ESteamInputType.k_ESteamInputType_SteamDeckController:
                                    _bJapaneseControllerLayout.Value = false;
                                    __result                         = true;
                                    return true;
                                default:
                                    break;
                            }
                            break;
                        }
                    }
                }
                else { // Check the current device through Unity's own input API, as a fallback.
                    if (UnityEngine.InputSystem.Gamepad.all[0].device != null) {
                        switch (UnityEngine.InputSystem.Gamepad.all[0].device) {
                            case SwitchProControllerHID:
                                _bJapaneseControllerLayout.Value = true;
                                __result                         = false;
                                return false;
                            case XInputControllerWindows:
                                _bJapaneseControllerLayout.Value = false;
                                __result                         = true;
                                return true;
                        }
                    }
                }
                // Finally, if none of these things apply to our current controller, we go through with the change.
                // I should probably do some checks to make sure that any of the SteamInput stuff above that aren't Sony controllers are also being handled well.
                switch (_bJapaneseControllerLayout.Value) {
                    case true:
                        __result = false;
                        return false;
                    case false:
                        return true;
                }
            }

            [HarmonyPatch(typeof(GameUiIcon.Input), "GetSprite", new Type[] { typeof(EnumIcon) })]
            [HarmonyPostfix]
            public static void GetSprite(EnumIcon icon, ref Sprite __result)
            {
                __result = GetGlyph(icon, __result);
            }

            // TODO: Figure out why SteamInput wont show any prompts anymore. It used to work.
            static Sprite GetGlyph(EnumIcon icon, Sprite original) // TODO: Figure out why the keyboard prompt square after switching from controller input disappears.
            {
                var result = new Sprite();
                if (advInputMgrComponent == null) return result; // Checks if our input manager component is null before checking.
                if (advInputMgrComponent.steamInputInitialized && !_bDisableSteamInput.Value)
                {
                    if (SteamInput.GetConnectedControllers(advInputMgrComponent.inputHandles) <= 0) return original;
                    result = icon switch
                    {
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
                        _                       => original
                    };
                    // We are going to do the enter and back things separately, as according to the Japanese layout.
                    result = _bJapaneseControllerLayout.Value switch {
                        true => icon switch {
                            EnumIcon.PAD_ENTER => Glyphs.GlyphB,
                            EnumIcon.PAD_BACK  => Glyphs.GlyphA,
                            _                  => result
                        },
                        false => icon switch {
                            EnumIcon.PAD_ENTER => Glyphs.GlyphA,
                            EnumIcon.PAD_BACK  => Glyphs.GlyphB,
                            _                  => result
                        }
                    };
                }
                else {
                    if (UnityEngine.InputSystem.Gamepad.all[0].device == null) return result;
                    if (PS5Prompt == null || PS4Prompt == null) {
                        return original;
                    }
                    switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                    {
                        case DualSenseGamepadHID:
                            if (PS5Prompt != null)
                            {
                                result = icon switch
                                {
                                    EnumIcon.PAD_BUTTON_L   => PS5Prompt.GetSprite("button_sikaku"),
                                    EnumIcon.PAD_BUTTON_U   => PS5Prompt.GetSprite("button_sankaku"),
                                    EnumIcon.PAD_BUTTON_R   => PS5Prompt.GetSprite("button_maru"),
                                    EnumIcon.PAD_BUTTON_D   => PS5Prompt.GetSprite("button_batu"),
                                    EnumIcon.PAD_MOVE       => original,
                                    EnumIcon.PAD_MOVE_ALL   => original,
                                    EnumIcon.PAD_MOVE_L     => original,
                                    EnumIcon.PAD_MOVE_U     => original,
                                    EnumIcon.PAD_MOVE_R     => original,
                                    EnumIcon.PAD_MOVE_D     => original,
                                    EnumIcon.PAD_MOVE_LR    => original,
                                    EnumIcon.PAD_MOVE_UD    => original,
                                    EnumIcon.PAD_L1         => PS5Prompt.GetSprite("L1"),
                                    EnumIcon.PAD_R1         => PS5Prompt.GetSprite("R1"),
                                    EnumIcon.PAD_L2         => PS5Prompt.GetSprite("L2"),
                                    EnumIcon.PAD_R2         => PS5Prompt.GetSprite("R2"),
                                    EnumIcon.PAD_L3         => PS5Prompt.GetSprite("L3"),
                                    EnumIcon.PAD_R3         => PS5Prompt.GetSprite("R3"),
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
                                    EnumIcon.PAD_CREATE     => PS5Prompt.GetSprite("create"),
                                    EnumIcon.PAD_OPTIONS    => PS5Prompt.GetSprite("options"),
                                    EnumIcon.PAD_TOUCH      => PS5Prompt.GetSprite("touch"),
                                    EnumIcon.PAD_SELECT     => PS5Prompt.GetSprite("touch"),
                                    EnumIcon.PAD_START      => PS5Prompt.GetSprite("start"),
                                    _                       => original
                                };
                                // Japanese Layout Check.
                                result = _bJapaneseControllerLayout.Value switch {
                                    true => icon switch {
                                        EnumIcon.PAD_ENTER => PS5Prompt.GetSprite("button_maru"),
                                        EnumIcon.PAD_BACK  => PS5Prompt.GetSprite("button_batu"),
                                        _                  => result
                                    },
                                    false => icon switch {
                                        EnumIcon.PAD_ENTER => PS5Prompt.GetSprite("button_batu"),
                                        EnumIcon.PAD_BACK  => PS5Prompt.GetSprite("button_maru"),
                                        _                  => result
                                    }
                                };
                            }
                            else { result = original; }
                            break;
                        case DualShock3GamepadHID:
                            result = original;
                            break;
                        case DualShock4GamepadHID:
                            if (PS4Prompt != null) {
                                result = icon switch {
                                    EnumIcon.PAD_BUTTON_L   => PS4Prompt.GetSprite("button_sikaku"),
                                    EnumIcon.PAD_BUTTON_U   => PS4Prompt.GetSprite("button_sankaku"),
                                    EnumIcon.PAD_BUTTON_R   => PS4Prompt.GetSprite("button_maru"),
                                    EnumIcon.PAD_BUTTON_D   => PS4Prompt.GetSprite("button_batu"),
                                    EnumIcon.PAD_MOVE       => original,
                                    EnumIcon.PAD_MOVE_ALL   => original,
                                    EnumIcon.PAD_MOVE_L     => original,
                                    EnumIcon.PAD_MOVE_U     => original,
                                    EnumIcon.PAD_MOVE_R     => original,
                                    EnumIcon.PAD_MOVE_D     => original,
                                    EnumIcon.PAD_MOVE_LR    => original,
                                    EnumIcon.PAD_MOVE_UD    => original,
                                    EnumIcon.PAD_L1         => PS4Prompt.GetSprite("L1"),
                                    EnumIcon.PAD_R1         => PS4Prompt.GetSprite("R1"),
                                    EnumIcon.PAD_L2         => PS4Prompt.GetSprite("L2"),
                                    EnumIcon.PAD_R2         => PS4Prompt.GetSprite("R2"),
                                    EnumIcon.PAD_L3         => PS4Prompt.GetSprite("L3"),
                                    EnumIcon.PAD_R3         => PS4Prompt.GetSprite("R3"),
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
                                    EnumIcon.PAD_CREATE     => PS4Prompt.GetSprite("share"), // The big difference is the use of "share" instead of "create"
                                    EnumIcon.PAD_OPTIONS    => PS4Prompt.GetSprite("options"),
                                    EnumIcon.PAD_TOUCH      => PS4Prompt.GetSprite("touch"),
                                    EnumIcon.PAD_SELECT     => PS4Prompt.GetSprite("touch"),
                                    EnumIcon.PAD_START      => PS4Prompt.GetSprite("start"),
                                    _                       => original
                                };
                                // Japanese Layout Check.
                                result = _bJapaneseControllerLayout.Value switch {
                                    true => icon switch {
                                        EnumIcon.PAD_ENTER => PS4Prompt.GetSprite("button_maru"),
                                        EnumIcon.PAD_BACK  => PS4Prompt.GetSprite("button_batu"),
                                        _                  => result
                                    },
                                    false => icon switch {
                                        EnumIcon.PAD_ENTER => PS4Prompt.GetSprite("button_batu"),
                                        EnumIcon.PAD_BACK  => PS4Prompt.GetSprite("button_maru"),
                                        _                  => result
                                    }
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
                return result;
            }
        }
    }
}