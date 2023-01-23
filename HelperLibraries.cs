using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// Framerate Cap Stuff
using System.Runtime.InteropServices;
using System.Threading;
// SteamInput and Input Stuff
using IF.Steam;
using Steamworks;
using SvSFix;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace KingKrouch.Utility.Helpers;

public class InputManager : MonoBehaviour
{
    public bool steamInputInitialized = false;
    public InputHandle_t[] inputHandles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
    public InputHandle_t[] inputHandlesPrev = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];

    public struct ActionOrigins
    {
        public static EInputActionOrigin   originA;
        public static EInputActionOrigin   originB;
        public static EInputActionOrigin   originX;
        public static EInputActionOrigin   originY;
        public static EInputActionOrigin   originDpadUp;
        public static EInputActionOrigin   originDpadDown;
        public static EInputActionOrigin   originDpadLeft;
        public static EInputActionOrigin   originDpadRight;
        public static EInputActionOrigin   originLsClick;
        public static EInputActionOrigin   originLs;
        public static EInputActionOrigin   originRsClick;
        public static EInputActionOrigin   originRs;
        public static EInputActionOrigin   originLb;
        public static EInputActionOrigin   originLt;
        public static EInputActionOrigin   originRb;
        public static EInputActionOrigin   originRt;
        public static EInputActionOrigin   originStart;
        public static EInputActionOrigin   originBack;
        public static EInputActionOrigin   originLsUp;
        public static EInputActionOrigin   originLsDown;
        public static EInputActionOrigin   originLsLeft;
        public static EInputActionOrigin   originLsRight;
        public static EInputActionOrigin[] originLsUpDown      = new EInputActionOrigin[2];
        public static EInputActionOrigin[] originLsLeftRight   = new EInputActionOrigin[2];
        public static EInputActionOrigin   originRsUp;
        public static EInputActionOrigin   originRsDown;
        public static EInputActionOrigin   originRsLeft;
        public static EInputActionOrigin   originRsRight;
        public static EInputActionOrigin[] originRsUpDown      = new EInputActionOrigin[2];
        public static EInputActionOrigin[] originRsLeftRight   = new EInputActionOrigin[2];
        public static EInputActionOrigin[] originDPadUpDown    = new EInputActionOrigin[2];
        public static EInputActionOrigin[] originDPadLeftRight = new EInputActionOrigin[2];
        public static EInputActionOrigin[] originDPadFull      = new EInputActionOrigin[4];
    }

    public struct GlyphLocations
    {
        public static string   promptA;
        public static string   promptB;
        public static string   promptX;
        public static string   promptY;
        public static string   promptDPadUp;
        public static string   promptDPadDown;
        public static string   promptDPadLeft;
        public static string   promptDPadRight;
        public static string   promptLsClick;
        public static string   promptLs;
        public static string   promptRsClick;
        public static string   promptRs;
        public static string   promptLb;
        public static string   promptLt;
        public static string   promptRb;
        public static string   promptRt;
        public static string   promptStart;
        public static string   promptBack;
        public static string   promptLsUp;
        public static string   promptLsDown;
        public static string   promptLsLeft;
        public static string   promptLsRight;
        public static string[] promptLsUpDown      = new string[2];
        public static string[] promptLsLeftRight   = new string[2];
        public static string   promptRsUp;
        public static string   promptRsDown;
        public static string   promptRsLeft;
        public static string   promptRsRight;
        public static string[] promptRsUpDown      = new string[2];
        public static string[] promptRsLeftRight   = new string[2];
        public static string[] promptDPadUpDown    = new string[2];
        public static string[] promptDPadLeftRight = new string[2];
        public static string[] promptDPadFull      = new string[4];
    }

    private ESteamInputGlyphSize  GlyphSize  = ESteamInputGlyphSize.k_ESteamInputGlyphSize_Large;
    private ESteamInputGlyphStyle GlyphStyle = ESteamInputGlyphStyle.ESteamInputGlyphStyle_Light;

    public void UpdateActionOrigins()
    {
        // Grabs the Action Origins from Xbox Origin
        ActionOrigins.originA                = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_A);
        ActionOrigins.originB                = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_B);
        ActionOrigins.originX                = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_X);
        ActionOrigins.originY                = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_Y);
        ActionOrigins.originDpadUp           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_DPad_North);
        ActionOrigins.originDpadDown         = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_DPad_East);
        ActionOrigins.originDpadLeft         = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_DPad_South);
        ActionOrigins.originDpadRight        = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_DPad_West);
        ActionOrigins.originLsClick          = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_Click);
        ActionOrigins.originLs               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_Move);
        ActionOrigins.originRsClick          = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_Click);
        ActionOrigins.originRs               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_Move);
        ActionOrigins.originLb               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftBumper);
        ActionOrigins.originLt               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftTrigger_Pull);
        if (ActionOrigins.originLt == EInputActionOrigin.k_EInputActionOrigin_None) {
            ActionOrigins.originLt           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftTrigger_Click);
        }
        ActionOrigins.originRb               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightBumper);
        ActionOrigins.originRt               = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightTrigger_Pull);
        if (ActionOrigins.originRt == EInputActionOrigin.k_EInputActionOrigin_None) {
            ActionOrigins.originRt           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightTrigger_Click);
        }
        ActionOrigins.originStart            = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_Menu);
        ActionOrigins.originBack             = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_View);
        ActionOrigins.originLsUp             = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_DPadNorth);
        ActionOrigins.originLsDown           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_DPadSouth);
        ActionOrigins.originLsLeft           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_DPadWest);
        ActionOrigins.originLsRight          = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_LeftStick_DPadEast);
        ActionOrigins.originRsUp             = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_DPadNorth);
        ActionOrigins.originRsDown           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_DPadSouth);
        ActionOrigins.originRsLeft           = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_DPadWest);
        ActionOrigins.originRsRight          = GetActionOrigin(inputHandles[0], EXboxOrigin.k_EXboxOrigin_RightStick_DPadEast);
        ActionOrigins.originDPadUpDown[0]    = ActionOrigins.originDpadUp;
        ActionOrigins.originDPadUpDown[1]    = ActionOrigins.originDpadDown;
        ActionOrigins.originDPadLeftRight[0] = ActionOrigins.originDpadLeft;
        ActionOrigins.originDPadLeftRight[1] = ActionOrigins.originDpadRight;
        ActionOrigins.originDPadFull[0]      = ActionOrigins.originDpadUp;
        ActionOrigins.originDPadFull[1]      = ActionOrigins.originDpadLeft;
        ActionOrigins.originDPadFull[2]      = ActionOrigins.originDpadDown;
        ActionOrigins.originDPadFull[3]      = ActionOrigins.originDpadRight;
        ActionOrigins.originLsUpDown[0]      = ActionOrigins.originLsUp;
        ActionOrigins.originLsUpDown[1]      = ActionOrigins.originLsDown;
        ActionOrigins.originLsLeftRight[0]   = ActionOrigins.originLsLeft;
        ActionOrigins.originLsLeftRight[1]   = ActionOrigins.originLsRight;
        ActionOrigins.originRsUpDown[0]      = ActionOrigins.originRsUp;
        ActionOrigins.originRsUpDown[1]      = ActionOrigins.originRsDown;
        ActionOrigins.originRsLeftRight[0]   = ActionOrigins.originRsLeft;
        ActionOrigins.originRsLeftRight[1]   = ActionOrigins.originRsRight;
    }

    public EInputActionOrigin GetActionOrigin(InputHandle_t inputHandle, EXboxOrigin xboxOrigin)
    {
        var actionOrigin = SteamInput.GetActionOriginFromXboxOrigin(inputHandle, xboxOrigin);
        if (actionOrigin >= EInputActionOrigin.k_EInputActionOrigin_Count) {
            actionOrigin = SteamInput.TranslateActionOrigin(ESteamInputType.k_ESteamInputType_Unknown, actionOrigin);
        }
        return actionOrigin;
    }

    public void UpdateGlyphLocations()
    {
        var inputTypeP1 = SteamInput.GetInputTypeForHandle(inputHandles[0]);
        switch (inputTypeP1) { // We want to use black button prompts if a PS3 or PS4 controller are connected.
            case ESteamInputType.k_ESteamInputType_PS4Controller: GlyphStyle = ESteamInputGlyphStyle.ESteamInputGlyphStyle_Dark;  break;
            case ESteamInputType.k_ESteamInputType_PS3Controller: GlyphStyle = ESteamInputGlyphStyle.ESteamInputGlyphStyle_Dark;  break;
            default:                                              GlyphStyle = ESteamInputGlyphStyle.ESteamInputGlyphStyle_Light; break;
        }
        
        // Grabbing the file locations for glyphs based on the Action Origin.
        GlyphLocations.promptA                = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originA,         GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptB                = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originB,         GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptX                = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originX,         GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptY                = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originY,         GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptDPadUp           = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originDpadUp,    GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptDPadDown         = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originDpadDown,  GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptDPadLeft         = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originDpadLeft,  GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptDPadRight        = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originDpadRight, GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLsClick          = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLsClick,   GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLs               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLs,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRsClick          = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRsClick,   GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRs               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRs,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLb               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLb,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLt               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLt,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRb               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRb,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRt               = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRt,        GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptStart            = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originStart,     GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptBack             = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originBack,      GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLsUp             = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLsUp,      GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLsDown           = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLsDown,    GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLsLeft           = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLsLeft,    GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptLsRight          = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originLsRight,   GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRsUp             = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRsUp,      GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRsDown           = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRsDown,    GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRsLeft           = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRsLeft,    GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptRsRight          = SteamInput.GetGlyphPNGForActionOrigin(ActionOrigins.originRsRight,   GlyphSize, (uint)GlyphStyle);
        GlyphLocations.promptDPadUpDown[0]    = GlyphLocations.promptDPadUp;
        GlyphLocations.promptDPadUpDown[1]    = GlyphLocations.promptDPadDown;
        GlyphLocations.promptDPadLeftRight[0] = GlyphLocations.promptDPadLeft;
        GlyphLocations.promptDPadLeftRight[1] = GlyphLocations.promptDPadRight;
        GlyphLocations.promptDPadFull[0]      = GlyphLocations.promptDPadUp;
        GlyphLocations.promptDPadFull[1]      = GlyphLocations.promptDPadLeft;
        GlyphLocations.promptDPadFull[2]      = GlyphLocations.promptDPadDown;
        GlyphLocations.promptDPadFull[3]      = GlyphLocations.promptDPadRight;
        GlyphLocations.promptLsUpDown[0]      = GlyphLocations.promptLsUp;
        GlyphLocations.promptLsUpDown[1]      = GlyphLocations.promptLsDown;
        GlyphLocations.promptLsLeftRight[0]   = GlyphLocations.promptLsLeft;
        GlyphLocations.promptLsLeftRight[1]   = GlyphLocations.promptLsRight;
        GlyphLocations.promptRsUpDown[0]      = GlyphLocations.promptRsUp;
        GlyphLocations.promptRsUpDown[1]      = GlyphLocations.promptRsDown;
        GlyphLocations.promptRsLeftRight[0]   = GlyphLocations.promptRsLeft;
        GlyphLocations.promptRsLeftRight[1]   = GlyphLocations.promptRsRight;
    }

    public void UpdateGlyphSprites()
    {
        // Now we can create the sprites given the glyph PNG location.
        Glyphs.GlyphA                = CreateNewSpriteFromImageLocation(GlyphLocations.promptA);           // Batsu   (Cross)
        Glyphs.GlyphB                = CreateNewSpriteFromImageLocation(GlyphLocations.promptB);           // Maru    (Circle)
        Glyphs.GlyphX                = CreateNewSpriteFromImageLocation(GlyphLocations.promptX);           // Sikaku  (Square)
        Glyphs.GlyphY                = CreateNewSpriteFromImageLocation(GlyphLocations.promptY);           // Sankaku (Triangle)
        Glyphs.GlyphDPadUp           = CreateNewSpriteFromImageLocation(GlyphLocations.promptDPadUp);      // D-Pad Up
        Glyphs.GlyphDPadDown         = CreateNewSpriteFromImageLocation(GlyphLocations.promptDPadDown);    // D-Pad Down
        Glyphs.GlyphDPadLeft         = CreateNewSpriteFromImageLocation(GlyphLocations.promptDPadLeft);    // D-Pad Left
        Glyphs.GlyphDPadRight        = CreateNewSpriteFromImageLocation(GlyphLocations.promptDPadRight);   // D-Pad Right
        Glyphs.GlyphLsClick          = CreateNewSpriteFromImageLocation(GlyphLocations.promptLsClick);     // LS Click
        Glyphs.GlyphLs               = CreateNewSpriteFromImageLocation(GlyphLocations.promptLs);          // LS
        Glyphs.GlyphRsClick          = CreateNewSpriteFromImageLocation(GlyphLocations.promptRsClick);     // RS Click
        Glyphs.GlyphRs               = CreateNewSpriteFromImageLocation(GlyphLocations.promptRs);          // RS
        Glyphs.GlyphLb               = CreateNewSpriteFromImageLocation(GlyphLocations.promptLb);          // LB
        Glyphs.GlyphLt               = CreateNewSpriteFromImageLocation(GlyphLocations.promptLt);          // LT
        Glyphs.GlyphRb               = CreateNewSpriteFromImageLocation(GlyphLocations.promptRb);          // RB
        Glyphs.GlyphRt               = CreateNewSpriteFromImageLocation(GlyphLocations.promptRt);          // RT
        Glyphs.GlyphStart            = CreateNewSpriteFromImageLocation(GlyphLocations.promptStart);       // Start
        Glyphs.GlyphBack             = CreateNewSpriteFromImageLocation(GlyphLocations.promptBack);        // Back
        Glyphs.GlyphLsUp             = CreateNewSpriteFromImageLocation(GlyphLocations.promptLsUp);
        Glyphs.GlyphLsDown           = CreateNewSpriteFromImageLocation(GlyphLocations.promptLsDown);
        Glyphs.GlyphLsLeft           = CreateNewSpriteFromImageLocation(GlyphLocations.promptLsLeft);
        Glyphs.GlyphLsRight          = CreateNewSpriteFromImageLocation(GlyphLocations.promptLsRight);
        Glyphs.GlyphLsUpDown[0]      = Glyphs.GlyphLsUp;
        Glyphs.GlyphLsUpDown[1]      = Glyphs.GlyphLsDown;
        Glyphs.GlyphLsLeftRight[0]   = Glyphs.GlyphLsLeft;
        Glyphs.GlyphLsLeftRight[1]   = Glyphs.GlyphLsRight;
        Glyphs.GlyphRsUp             = CreateNewSpriteFromImageLocation(GlyphLocations.promptRsUp);
        Glyphs.GlyphRsDown           = CreateNewSpriteFromImageLocation(GlyphLocations.promptRsDown);
        Glyphs.GlyphRsLeft           = CreateNewSpriteFromImageLocation(GlyphLocations.promptRsLeft);
        Glyphs.GlyphRsRight          = CreateNewSpriteFromImageLocation(GlyphLocations.promptRsRight);
        Glyphs.GlyphRsUpDown[0]      = Glyphs.GlyphRsUp;
        Glyphs.GlyphRsUpDown[1]      = Glyphs.GlyphRsDown;
        Glyphs.GlyphRsLeftRight[0]   = Glyphs.GlyphRsLeft;
        Glyphs.GlyphRsLeftRight[1]   = Glyphs.GlyphRsRight;
        Glyphs.GlyphDPadUpDown[0]    = Glyphs.GlyphDPadUp;
        Glyphs.GlyphDPadUpDown[1]    = Glyphs.GlyphDPadDown;
        Glyphs.GlyphDPadLeftRight[0] = Glyphs.GlyphDPadLeft;
        Glyphs.GlyphDPadLeftRight[1] = Glyphs.GlyphDPadRight;
        // D-Pad Full will go in a cycle counter-clockwise from top to left to down to right, on repeat.
        Glyphs.GlyphDPadFull[0]      = Glyphs.GlyphDPadUp;
        Glyphs.GlyphDPadFull[1]      = Glyphs.GlyphDPadLeft;
        Glyphs.GlyphDPadFull[2]      = Glyphs.GlyphDPadDown;
        Glyphs.GlyphDPadFull[3]      = Glyphs.GlyphDPadRight;
    }

    public void CreateNewPromptImages()
    {
        if (steamInputInitialized && !SvSFix.SvSFix._bDisableSteamInput.Value) {
            if (SteamInput.GetConnectedControllers(inputHandles) > 0)
            {
                UpdateActionOrigins();
                UpdateGlyphLocations();
                UpdateGlyphSprites();
            }
        }
    }

    private Sprite CreateNewSpriteFromImageLocation(string fileLocation) // TODO: Figure out why the sprites are too big relative to the originals. Size doesn't matter, that's what she (Unity) said.
    {
        fileLocation = fileLocation.Replace("color_button", "color_outlined_button"); // Since Valve's styling isn't flexible enough, gonna force the color outlined buttons.
        var rawData = File.ReadAllBytes(fileLocation);
        Texture2D prompt = new Texture2D(2, 2);
        prompt.LoadImage(rawData);
        // TODO: The difference between the original sprites and the actual size they take up is 70%. So ideally, I want to find a way to shrink the glyphs to 70% size in the center of the texture.
        Vector2 size = new Vector2(prompt.width, prompt.height);
        Rect imageRect = new Rect(new Vector2(0,0), size);
        Vector2 pivot = new Vector2(((float)prompt.width / 2), ((float)prompt.height / 2));
        Sprite output = Sprite.Create(prompt, imageRect, pivot, 100);
        return output;
    }
    
    public void InitInput()
    {
        bool initialized = SteamworksAccessor.IsSteamworksReady;
        ESteamInputType inputTypeP1 = ESteamInputType.k_ESteamInputType_Unknown;
        if (initialized)
        {
            if (!SvSFix.SvSFix._bDisableSteamInput.Value) { steamInputInitialized = SteamInput.Init(false); }
            if (steamInputInitialized)
            {
                SteamInput.RunFrame();
                int result = SteamInput.GetConnectedControllers(inputHandles);
                inputHandlesPrev = inputHandles;
                // Grabs Player 1 Controller Type.
                inputTypeP1 = SteamInput.GetInputTypeForHandle(inputHandles[0]);
                switch (inputTypeP1)
                {
                    case ESteamInputType.k_ESteamInputType_Unknown:
                        // This is when the controller isn't detected at all. If SteamInput is disabled, it's going to return with this controller type.
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamController:
                        break;
                    case ESteamInputType.k_ESteamInputType_XBox360Controller:
                        break;
                    case ESteamInputType.k_ESteamInputType_XBoxOneController:
                        break;
                    case ESteamInputType.k_ESteamInputType_PS4Controller:
                        SteamInput.SetLEDColor(inputHandles[0], 147, 117, 219, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConPair:
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConSingle:
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchProController:
                        break;
                    case ESteamInputType.k_ESteamInputType_PS3Controller: // TODO: Figure out why this won't get recognized.
                        break;
                    case ESteamInputType.k_ESteamInputType_PS5Controller:
                        SteamInput.SetLEDColor(inputHandles[0], 147, 117, 219, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamDeckController:
                        break;
                    case ESteamInputType.k_ESteamInputType_Count:
                        break;
                    case ESteamInputType.k_ESteamInputType_MaximumPossibleValue:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Debug.Log("Connected Controller 1: " + inputTypeP1);
                CreateNewPromptImages();
            }
            if (!steamInputInitialized || !SvSFix.SvSFix._bDisableSteamInput.Value  || inputTypeP1 == ESteamInputType.k_ESteamInputType_Unknown)
            {
                if (UnityEngine.InputSystem.Gamepad.all[0].device != null)
                {
                    Debug.Log(UnityEngine.InputSystem.Gamepad.all[0].device.name);
                    switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                    {
                        case DualSenseGamepadHID dualSenseGamepadHid:
                            var dualSense = DualShockGamepad.current;
                            dualSense?.SetLightBarColor(new Color(147, 117, 219));
                            break;
                        case DualShock3GamepadHID dualShock3GamepadHid:
                            break;
                        case DualShock4GamepadHID dualShock4GamepadHid:
                            var dualShock = DualShockGamepad.current;
                            dualShock?.SetLightBarColor(new Color(147, 117, 219));
                            break;
                        case SwitchProControllerHID switchProControllerHid:
                            break;
                        case XInputControllerWindows xInputControllerWindows:
                            break;
                        default:
                            break;
                    }
                }
            }
            if (SteamUtils.IsSteamRunningOnSteamDeck()) {
                Debug.Log("Running on Steam Deck!"); // Should probably find a way to load optimized settings on first run.
            }
        }
    }
    
    // TODO: Fix the sprite flipping, or find something in-game that actually uses these.
    public float timeRemainingUntilSpriteFlip = 150; // Three seconds in FixedUpdate time (50Hz)
    public int arraySize2 = 0;
    public int arraySize4 = 0;

    private void FixedUpdate() // We are simply going to use FixedUpdate for our button prompt changes.
    {
        if (SteamworksAccessor.IsSteamworksReady && steamInputInitialized && !SvSFix.SvSFix._bDisableSteamInput.Value)
        {
            if (timeRemainingUntilSpriteFlip > 0) { // We are going to want to have a 150 frame time until prompts flips back.
                timeRemainingUntilSpriteFlip -= Time.fixedDeltaTime;
            }
            else
            {
                if (arraySize2 >= 2 - 1) { arraySize2 = 0; } else { arraySize2 += 1; } // We are doing the up/down, left/right glyphs separately, because there's more than one, and I'm too lazy to rewrite this for now.
                if (arraySize4 >= 4 - 1) { arraySize4 = 0; } else { arraySize4 += 1; }
                Glyphs.GlyphLsUpDownPresent = Glyphs.GlyphLsUpDown[arraySize2];
                Glyphs.GlyphLsLeftRightPresent = Glyphs.GlyphLsLeftRight[arraySize2];
                Glyphs.GlyphRsUpDownPresent = Glyphs.GlyphRsUpDown[arraySize2];
                Glyphs.GlyphRsLeftRightPresent = Glyphs.GlyphRsLeftRight[arraySize2];
                Glyphs.GlyphDPadUpDownPresent = Glyphs.GlyphDPadUpDown[arraySize2];
                Glyphs.GlyphDPadLeftRightPresent = Glyphs.GlyphDPadLeftRight[arraySize2];
                Glyphs.GlyphDPadFullPresent = Glyphs.GlyphDPadFull[arraySize4];
            }
        }
    }

    private void Update()
    {
        if (SteamworksAccessor.IsSteamworksReady && steamInputInitialized && !SvSFix.SvSFix._bDisableSteamInput.Value) {
            SteamInput.RunFrame();
            if (inputHandles != inputHandlesPrev) { // Checks if inputHandles is old, and if so, updates our inputHandles, and generates new prompt images for Player 1.
                int result = SteamInput.GetConnectedControllers(inputHandles);
                inputHandlesPrev = inputHandles;
                Debug.Log("Reconnected Controller 1: " + SteamInput.GetInputTypeForHandle(inputHandles[0]));
                CreateNewPromptImages();
            }
        }
    }

    private void Start()
    { 
        InitInput();
    }
}

public class ResolutionManager : MonoBehaviour
{
    public bool enableDebug = false;
    public struct Resolution
    {
        // // Example Usage:
        // static Resolution resolutionNew = new Resolution(1920, 1080);
        // private int resolutionNewX      = resolutionNew.Width;
        public int Width { get; set; }
        public int Height { get; set; }

        public Resolution(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }

    public static List<ResolutionManager.Resolution> ScreenResolutions()
    {
        var eResolutions = Screen.resolutions.Where(resolution => resolution.refreshRate == Screen.currentResolution.refreshRate); // Filter out any resolution that isn't supported by the current refresh rate.
        eResolutions.OrderBy(s => s); // Order by least to greatest.
        var aScreenResolutions = eResolutions as UnityEngine.Resolution[] ?? eResolutions.ToArray(); // Convert our Enumerable to an Array.
        var screenResolutions = new List<ResolutionManager.Resolution>(); // Creates the List we will be sorting resolutions in.
        for (int i = 0; i < aScreenResolutions.Length; i++) { // Run a for loop for each screen resolution in the array, since Unity's resolutions are incompatible with our own.
            var screenResolution = new ResolutionManager.Resolution(aScreenResolutions[i].width, aScreenResolutions[i].height);
            screenResolutions.Add(screenResolution);
        }

        // Our Hardcoded list of resolutions. We plan on appending these values to our resolution list only if the largest available display resolution is greater than one of these.
        var aHcResolutions = new ResolutionManager.Resolution[14];
        var hcResolutions   = new List<ResolutionManager.Resolution>();
        aHcResolutions[0].Width  = 640;   aHcResolutions[0].Height  = 360;
        aHcResolutions[1].Width  = 720;   aHcResolutions[1].Height  = 405;
        aHcResolutions[2].Width  = 800;   aHcResolutions[2].Height  = 450;
        aHcResolutions[3].Width  = 960;   aHcResolutions[3].Height  = 540;
        aHcResolutions[4].Width  = 1024;  aHcResolutions[4].Height  = 576;
        aHcResolutions[5].Width  = 1152;  aHcResolutions[5].Height  = 648;
        aHcResolutions[6].Width  = 1280;  aHcResolutions[6].Height  = 720;
        aHcResolutions[7].Width  = 1360;  aHcResolutions[7].Height  = 765;
        aHcResolutions[8].Width  = 1366;  aHcResolutions[8].Height  = 768;
        aHcResolutions[9].Width  = 1600;  aHcResolutions[9].Height  = 900;
        aHcResolutions[10].Width = 1920;  aHcResolutions[10].Height = 1080;
        aHcResolutions[11].Width = 2560;  aHcResolutions[11].Height = 1440;
        aHcResolutions[12].Width = 3840;  aHcResolutions[12].Height = 2160;
        aHcResolutions[13].Width = 7680;  aHcResolutions[13].Height = 4320;
        for (int i = 0; i < aHcResolutions.Length; i++) {
            hcResolutions.Add(aHcResolutions[i]);
        }
        int screenResolutionsCount = screenResolutions.Count - 1;
        for (int i = 0; i < hcResolutions.Count; i++) {
            if (screenResolutions[screenResolutionsCount].Width + screenResolutions[screenResolutionsCount].Height >
                hcResolutions[i].Width + hcResolutions[i].Height) {
                screenResolutions.Add(hcResolutions[i]);
            }
        }
        var resolutions = screenResolutions.Distinct().ToList();

        var resSort = from r in resolutions orderby r.Width + r.Height ascending select r;
        var resolutionsSorted   = new List<ResolutionManager.Resolution>();
        foreach (var r in resSort) {
            resolutionsSorted.Add(r);
        }
        return resolutionsSorted;
    }
    // Start is called before the first frame update
    void Start()
    {
        var sr = ResolutionManager.ScreenResolutions().ToList();

        for (int i = 0; i < sr.Count; i++) { // Now we will finally do what we want with the display resolution list.
            if (enableDebug) { Debug.Log(sr[i].Width + "x" + sr[i].Height); } // In this case, print a debug log to show we are doing things right.
        }
    }
}
    
public class BlackBarController : MonoBehaviour
{
    public bool enableDebug = false;
    //public Camera camera;
    public Image pillarboxLeft;
    public Image pillarboxRight;
    public Image letterboxTop;
    public Image letterboxBottom;
    public float originalAspectRatio = 1.7777778f;
    [Range(0.0f, 1.0f)]
    public float opacity = 1.0f;
    public float fadeSpeed = 2.5f;
    void SetupCoordinates()
    {
    
        float resX = SystemCamera3D.GetCamera().pixelWidth; // You can grab a camera and use camera.pixelWidth during editor builds, but Screen calls should be just fine.
        float resY = SystemCamera3D.GetCamera().pixelHeight;
        if (enableDebug) { Debug.Log( resX + "x" + resY); }
        float currentAspectRatio = SystemCamera3D.GetCamera().aspect;
        float aspectRatioOffset = originalAspectRatio / currentAspectRatio;

        // Set up the Vertical offsets.
        float originalAspectRatioApproximateY = resY * aspectRatioOffset;
        float verticalResDifference = resY - originalAspectRatioApproximateY;

        // Set up the Horizontal offsets.
        float originalAspectRatioApproximateX = resX * aspectRatioOffset;
        float horizontalResDifference = resX - originalAspectRatioApproximateX;
        
        // Set up our top side letterbox.
        letterboxTop.rectTransform.sizeDelta =  new Vector2(0.0f,-(verticalResDifference / 2));
        letterboxTop.rectTransform.anchoredPosition = new Vector2(0.0f, verticalResDifference / 2);
        
        // Set up our bottom side letterbox.
        letterboxBottom.rectTransform.sizeDelta = new Vector2(0.0f, -(verticalResDifference / 2));
        letterboxBottom.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
        
        // Set up our left side pillarbox.
        pillarboxLeft.rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);; // Positions the left bar on the left side of the screen.
        pillarboxLeft.rectTransform.sizeDelta = new Vector2(horizontalResDifference / 2, 0.0f);

        // Set up our right side pillarbox.
        pillarboxRight.rectTransform.anchoredPosition = new Vector2(-(horizontalResDifference / 2), 0.0f); // Positions the right bar on the right side of the screen.
        pillarboxRight.rectTransform.sizeDelta = new Vector2(horizontalResDifference / 2, 0.0f);
        
        // Toggle our Letterbox and Pillarbox Components based on the display aspect ratio.
        if (currentAspectRatio < originalAspectRatio) {
            pillarboxLeft.enabled = false; pillarboxRight.enabled  = false;
            letterboxTop.enabled  = true;  letterboxBottom.enabled = true;
        }
        else if (currentAspectRatio > originalAspectRatio) {
            pillarboxLeft.enabled = true; pillarboxRight.enabled  = true;
            letterboxTop.enabled  = false; letterboxBottom.enabled = false;
        }
        else {
            pillarboxLeft.enabled = false; pillarboxRight.enabled  = false;
            letterboxTop.enabled  = false; letterboxBottom.enabled = false;
        }
    }
    void SetupOpacity() // Need to set up some events that will fade in or out the opacity based on a set timeframe.
    {
        letterboxTop.color = new Color(0, 0, 0, opacity);
        letterboxBottom.color = new Color(0, 0, 0, opacity);
        pillarboxLeft.color = new Color(0, 0, 0, opacity);
        pillarboxRight.color = new Color(0, 0, 0, opacity);
    }
    void Setup()
    {
        SetupCoordinates();
        SetupOpacity();
    }
    // Fade Out Black Bars event
    public IEnumerator FadeOutBlackBars()
    {
        while (opacity > 0.00f) {
            opacity = opacity - (fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }
    // Fade In Back Bars event
    public IEnumerator FadeInBlackBars()
    {
        while (opacity < 1.00f) {
            opacity = opacity + (fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }
    // Update is called once per frame
    void Update()
    {
        Setup();
    } 
}

public class FramerateLimiter : MonoBehaviour
{
    private FramerateLimiter m_Instance;
    public FramerateLimiter Instance { get { return m_Instance; } }
    public double fpsLimit  = 0.0f;

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
    
    private static long SystemTimePrecise()
    {
        long stp = 0;
        GetSystemTimePreciseAsFileTime(out stp);
        return stp;
    }
    
    private long _lastTime = SystemTimePrecise();

    void Awake()
    {
        m_Instance = this;
    }

    void OnDestroy()
    {
        m_Instance = null;
    }

    void Update()
    {
        if (fpsLimit == 0.0) return;
        _lastTime += TimeSpan.FromSeconds(1.0 / fpsLimit).Ticks;
        long now = SystemTimePrecise();

        if (now >= _lastTime)
        {
            _lastTime = now;
            return;
        }
        else
        {
            SpinWait.SpinUntil(() => { return (SystemTimePrecise() >= _lastTime); });
        }
    }
}