using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game.Cinema;
using Game.UI;
using Game.UI.Config;
using Game.UI.Local;
using Game.UI.MainMenu;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Game.UI.MainMenu.Local;
using IF.PhotoMode.Control;
using IF.PhotoMode.Imaging;
using IF.Steam;
using KingKrouch.Utility.Helpers;
using Steamworks;
using SvSFix.Controllers;
using SvSFix.ResolutionClasses;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace SvSFix
{
    public struct Glyphs
    {
        public static Sprite   GlyphA;
        public static Sprite   GlyphB;
        public static Sprite   GlyphX;
        public static Sprite   GlyphY;
        public static Sprite   GlyphDPadUp;
        public static Sprite   GlyphDPadDown;
        public static Sprite   GlyphDPadLeft;
        public static Sprite   GlyphDPadRight;
        public static Sprite   GlyphLsClick;
        public static Sprite   GlyphLs;
        public static Sprite   GlyphRsClick;
        public static Sprite   GlyphRs;
        public static Sprite   GlyphLb;
        public static Sprite   GlyphLt;
        public static Sprite   GlyphRb;
        public static Sprite   GlyphRt;
        public static Sprite   GlyphStart;
        public static Sprite   GlyphBack;
        public static Sprite   GlyphLsUp;
        public static Sprite   GlyphLsDown;
        public static Sprite   GlyphLsLeft;
        public static Sprite   GlyphLsRight;
        public static Sprite[] GlyphLsUpDown      = new Sprite[2];
        public static Sprite[] GlyphLsLeftRight   = new Sprite[2];
        public static Sprite   GlyphRsUp;
        public static Sprite   GlyphRsDown;
        public static Sprite   GlyphRsLeft;
        public static Sprite   GlyphRsRight;
        public static Sprite[] GlyphRsUpDown      = new Sprite[2];
        public static Sprite[] GlyphRsLeftRight   = new Sprite[2];
        public static Sprite[] GlyphDPadUpDown    = new Sprite[2];
        public static Sprite[] GlyphDPadLeftRight = new Sprite[2];
        public static Sprite[] GlyphDPadFull      = new Sprite[4];
    }
    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("neptunia-sisters-vs-sisters.exe")]
    public class SvSFix : BaseUnityPlugin
    {
        private static ManualLogSource _log;

        private enum EPostAAType
        {
            Off,
            Fxaa,
            Smaa
        };

        private enum EShadowQuality
        {
            Low, // 512
            Medium, // 1024
            High, // 2048
            Original, // 4096
            Ultra, // 8192
            Extreme // 16384
        };

        private static EPostAAType _confPostAAType;
        private static EShadowQuality _confShadowQuality;

        public static Vector2 ShadowResVec()
        {
            switch (_confShadowQuality) {
            case EShadowQuality.Low:
                return new Vector2(512, 512);
            case EShadowQuality.Medium:
                return new Vector2(1024, 1024);
            case EShadowQuality.High: 
                return new Vector2(2048, 2048);
            case EShadowQuality.Original:
                return new Vector2(4096, 4096);
            case EShadowQuality.Ultra:
                return new Vector2(8192, 8192);
            case EShadowQuality.Extreme:
                return new Vector2(16384, 16384);
            default:
                return new Vector2(4096, 4096);
            }
        }
        
        static BlackBarController _controllerComponent;

        // Aspect Ratio Config
        private static ConfigEntry<bool> _bOriginalUIAspectRatio; // On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.
        private static ConfigEntry<bool> _bPresentCutscenesWithOriginalAspectRatio; // On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).
        private static ConfigEntry<bool> _bMajorAxisFOVScaling; // On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.

        // Graphics Config
        private static ConfigEntry<int> _imsaaCount; // 0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.
        private static ConfigEntry<string> _sPostAAType; // Going to convert an string to one of the enumerator values.
        private static ConfigEntry<int> _resolutionScale; // Goes from 25% to 200%. Then it's adjusted to a floating point value between 0.25-2.00x.
        private static ConfigEntry<string> _sShadowQuality; // Going to convert an string to one of the enumerator values.
        private static ConfigEntry<int> _shadowCascades; // 0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)
        private static ConfigEntry<float> _fLodBias; // Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons.
        private static ConfigEntry<int> _forcedLodQuality; // Default is 0, goes up to LOD #3 without cutting insane amounts of level geometry.
        private static ConfigEntry<int> _forcedTextureQuality; // Default is 0, goes up to 1/14th resolution.
        private static ConfigEntry<int> _anisotropicFiltering; // 0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF.
        private static ConfigEntry<bool> _bPostProcessing; // Quick Toggle for Post-Processing

        // Framelimiter Config
        private static ConfigEntry<int> _frameInterval; // "0" disables the framerate cap, "1" caps at your screen refresh rate, "2" caps at half refresh, "3" caps at 1/3rd refresh, "4" caps at quarter refresh.
        private static ConfigEntry<bool> _bvSync; // Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.
        private static readonly int MaskMatrixUV = Shader.PropertyToID("mask_matrix_uv");
        private static readonly int MaskTexture = Shader.PropertyToID("mask_texture");

        private void InitConfig()
        {
            // Aspect Ratio Config
            _bOriginalUIAspectRatio = Config.Bind("Resolution", "OriginalUIAspectRatio", true,
                "On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.");
            _bMajorAxisFOVScaling = Config.Bind("Resolution", "MajorAxisFOVScaling", true,
                "On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.");
            _bPresentCutscenesWithOriginalAspectRatio = Config.Bind("Resolution", "PresentCutscenesAtOriginalAspectRatio", false,
                "On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).");

            // Graphics Config
            _imsaaCount = Config.Bind("Graphics", "MSAACount", 0,
                new ConfigDescription("0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.",
                    new AcceptableValueRange<int>(0, 8)));

            _sPostAAType = Config.Bind("Graphics", "PostAA", "SMAA", "Off, FXAA, SMAA");
            if (!Enum.TryParse(_sPostAAType.Value, out _confPostAAType)) {
                _confPostAAType = EPostAAType.Smaa;
                SvSFix._log.LogError($"PostAA Value is invalid. Defaulting to SMAA.");
            }

            _resolutionScale = Config.Bind("Graphics", "ResolutionScale", 100,
                new ConfigDescription("Goes from 25% to 200%.", new AcceptableValueRange<int>(25, 200)));

            _sShadowQuality = Config.Bind("Graphics", "ShadowQuality", "Original",
                "Low (512), Medium (1024), High (2048), Original (4096), Ultra (8192), Extreme (16384)");
            if (!Enum.TryParse(_sShadowQuality.Value, out _confShadowQuality)) {
                _confShadowQuality = EShadowQuality.Original;
                SvSFix._log.LogError($"ShadowQuality Value is invalid. Defaulting to Original.");
            }

            _shadowCascades = Config.Bind("Graphics", "ShadowCascades", 4,
                new ConfigDescription("0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)",
                    new AcceptableValueRange<int>(0, 4)));
            
            _fLodBias = Config.Bind("Graphics", "LodBias", (float)1.00,
                new ConfigDescription(
                    "Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons."));

            _forcedLodQuality = Config.Bind("Graphics", "ForcedLODQuality", 0,
                new ConfigDescription("0: No Forced LODs (Default), 1: Forces LOD # 1, 2: Forces LOD # 2, 3: Forces LOD # 3. Higher the value, the less mesh detail.",
                    new AcceptableValueRange<int>(0, 3)));
            
            _forcedTextureQuality = Config.Bind("Graphics", "ForcedTextureQuality", 0,
                new ConfigDescription("0: Full Resolution (Default), 1: Half-Res, 2: Quarter Res. Goes up to 1/14th res (14).",
                    new AcceptableValueRange<int>(0, 14)));
            
            _bPostProcessing = Config.Bind("Graphics", "PostProcessing", true,
                "On: Enables Post Processing (Default), Off: Disables Post Processing (Which may be handy for certain configurations)");

            

            _anisotropicFiltering = Config.Bind("Graphics", "AnisotropicFiltering", 0,
                new ConfigDescription("0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF",
                    new AcceptableValueRange<int>(0, 16)));

            // Framelimiter Config
            _frameInterval = Config.Bind("Framerate", "Framerate Cap Interval", 1,
                new ConfigDescription(
                    "0 disables the framerate cap, 1 caps at your screen refresh rate, 2 caps at half refresh, 3 caps at 1/3rd refresh, 4 caps at quarter refresh.",
                    new AcceptableValueRange<int>(0, 4)));

            _bvSync = Config.Bind("Framerate", "VSync", true,
                "Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.");
        }
        private void Awake()
        {
            SvSFix._log = Logger;
            // Plugin startup logic
            SvSFix._log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Reads or creates our config file.
            InitConfig();
            LoadGraphicsSettings(); //TODO: Figure out why this is spitting an error
            // Creates our custom components, and prints a log statement if it failed or not.
            var createdBlackBarActor = CreateBlackBars();
            if (createdBlackBarActor) {
                _log.LogInfo("Adding BlackBarController Hooks.");
                Harmony.CreateAndPatchAll(typeof(BlackBarControllerFunctionality));
            }
            //else { Log.LogError("Couldn't create Pillarbox Actor."); }
            var createdFramelimiter = InitializeFramelimiter();
            if (createdFramelimiter) {
                _log.LogInfo("Created Framelimiter.");
            }
            else { _log.LogError("Couldn't create Framelimiter Actor."); }
            // Finally, runs our UI and Framerate Patches.
            Harmony.CreateAndPatchAll(typeof(UIPatches));
            Harmony.CreateAndPatchAll(typeof(FrameratePatches));
            //Harmony.CreateAndPatchAll(typeof(ResolutionPatches));
            Harmony.CreateAndPatchAll(typeof(FOVPatches));
            Harmony.CreateAndPatchAll(typeof(InputPatches));
            Harmony.CreateAndPatchAll(typeof(PhotoModePatches));
        }

        private static bool InitializeFramelimiter()
        {
            GameObject frObject = new GameObject {
                name = "FramerateLimiter",
                transform = {
                    position = new Vector3(0, 0, 0),
                    rotation = Quaternion.identity
                }
            };
            DontDestroyOnLoad(frObject);
            var frLimiterComponent = frObject.AddComponent<FramerateLimiter>();
            frLimiterComponent.fpsLimit = (double)Screen.currentResolution.refreshRate / _frameInterval.Value;
            return true;
        }

        private static bool CreateBlackBars()
        {
            // Creates our BlackBarController prefab by hooking into Unity's AssetBundles system.
            // By default, SvS uses Unity 2021.2.5f1, keeping that in mind in case I need to use AssetBundles again for whatever reason.
            var path = @"BepInEx\content\svsfix_content";
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle != null)
            {
                var names = bundle.GetAllAssetNames();
                Debug.Log(bundle.GetAllAssetNames());
                var prefab = bundle.LoadAsset<GameObject>(names[0]);
                GameObject blackBarController = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
                DontDestroyOnLoad(blackBarController);
                // Creates the Controller component script manually, and then fixes some of the variables that need references.
                _controllerComponent = blackBarController.AddComponent<BlackBarController>();
                _controllerComponent.letterboxTop = (Image)prefab.transform.Find("Letterbox/Top")
                    .GetComponentInChildren(typeof(Image), true);
                _controllerComponent.letterboxBottom = (Image)prefab.transform.Find("Letterbox/Bottom")
                    .GetComponentInChildren(typeof(Image), true);
                _controllerComponent.pillarboxLeft = (Image)prefab.transform.Find("Pillarbox/Left")
                    .GetComponentInChildren(typeof(Image), true);
                _controllerComponent.pillarboxRight = (Image)prefab.transform.Find("Pillarbox/Right")
                    .GetComponentInChildren(typeof(Image), true);
                _controllerComponent.opacity = 1.0f;
                _log.LogInfo("Created BlackBarController Actor.");
                return true;
            }
            else {
                _log.LogError("Couldn't Spawn BlackBarController Actor.");
                return false;
            }
        }

        

        private static void LoadGraphicsSettings()
        {
            // TODO:
            // 1. Figure out why the texture filtering is not working correctly. Despite our patches, the textures are still blurry as fuck and has visible seams.
            // 2. Find a way of writing to the shadow resolution variables in the UniversalRenderPipelineAsset.
            
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(_anisotropicFiltering.Value, _anisotropicFiltering.Value);
            Texture.masterTextureLimit      = _forcedTextureQuality.Value; // Can raise this to force lower the texture size. Goes up to 14.
            QualitySettings.maximumLODLevel = _forcedLodQuality.Value; // Can raise this to force lower the LOD settings. 3 at max if you want it to look like a blockout level prototype.
            QualitySettings.lodBias         = _fLodBias.Value;
            
            // Let's adjust some of the Render Pipeline Settings during runtime.
            var asset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

            asset.renderScale = (float)_resolutionScale.Value / 100;
            //ShadowSettings.mainLightShadowmapResolution        = (int)shadowResVec().x; // TODO: Find a way to write to this.
            //ShadowSettings.additionalLightShadowResolution     = (int)shadowResVec().y;
            asset.msaaSampleCount = _imsaaCount.Value;
            asset.shadowCascadeCount = _shadowCascades.Value;
            QualitySettings.renderPipeline = asset;
            
            // TODO: Figure out why this isn't working properly.
            // Now let's adjust the post-processing settings for the camera.
            var cameraData = FindObjectsOfType<UniversalAdditionalCameraData>();
            foreach (var c in cameraData)
            {
                c.antialiasing = _confPostAAType switch {
                    EPostAAType.Off => AntialiasingMode.None,
                    EPostAAType.Fxaa => AntialiasingMode.FastApproximateAntialiasing,
                    EPostAAType.Smaa => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    _ => throw new ArgumentOutOfRangeException()
                };
                c.renderPostProcessing = _bPostProcessing.Value;
            }
        }

        [HarmonyPatch]
        public class BlackBarControllerFunctionality
        {
            // TODO:
            // 1. Add hooks for fullscreen UI elements, ADV/VN segments (start and finish), and cutscenes (start and finish) to fade-in/fade-out our BlackBarController.
            // Seems like GameCinemaEventStart is responsible for cutscenes, try and investigate some patches that fade-in/fade-out our BlackBarController based on if a cutscene has started or finished.
            
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

        [HarmonyPatch]
        public class PhotoModePatches
        {
            // TODO:
            // 1. Remove Photo Mode height restrictions (or at least make them to the floor rather than an angle that can't look up skirts).
            // 2. Add camera tilting to Photo Mode
            // 3. Add a Steam Screenshot hook to the Photo Mode screenshot feature.
            // 4. Add character height and rotation control.
            
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
                    snapshot.ReadPixels(new Rect(0,0, snapshotRt.width, snapshotRt.height), 0, 0); // Finally transfer the render target to our texture.
                    // Flip our texture (as it's upside down). TODO: Troubleshoot why this isn't working properly.
                    Color[] pixels = snapshot.GetPixels();
                    Array.Reverse(pixels);
                    snapshot.SetPixels(pixels);
                    // Now we finally let Steam's Screenshots API do it's thing.
                    long snapshotSize = snapshot.GetRawTextureData().Length;
                    byte[] snapshotData = snapshot.GetRawTextureData();
                    SteamScreenshots.WriteScreenshot(snapshotData, (uint)snapshotSize, snapshot.width, snapshot.height);
                    target_camera.targetTexture = oldTargetTexture; // Sets our target texture back to it's original after taking a screenshot.
                }
                return true;
            }
            

            [HarmonyPatch(typeof(CameraControl), "SetPosition", new Type[] { typeof(Vector3) })]
            [HarmonyPrefix]
            public static bool RemoveCameraClamping(ref Vector3 position)
            {
                //_log.LogInfo("Hooked Camera Clamping!"); // This hook seemingly works, I just need to figure out how to access private info.
                //if (this.camera_ == null)
                //{
                    //return;
                //}
                //this.camera_.transform.position = position;
                //return false
                return true; // Comment this return statement when you get a fix actually working.
            }
            //CameraControl.Set
        }

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
            // 8. Find out how the game is switching between button prompts when a button is pressed, remove mouse input from affecting that, and then allow forcing prompts to a specific type.

            public static GameObject advInputMgrObject;
            public static InputManager advInputMgrComponent;

            // So both GameInput and World Manager seemingly have stuff that toggles the mouse cursor.
            [HarmonyPatch(typeof(GameInput), nameof(GameInput.RenewMouseCursorVisible))]
            [HarmonyPrefix]
            public static bool MouseCursorPatch(GameInput __instance, GameInput.EnumDevice device) // My hunch is that this may let me turn on/off the mouse cursor.
            {
                return true;
            }

            //GameInputAccessor.CurrentDevice is what I assume is passing on the currently used input device.
            
            [HarmonyPatch(typeof(SteamworksAccessor), nameof(SteamworksAccessor.Initialize))]
            [HarmonyPostfix]
            public static void SteamworksInitExtra()
            {
                advInputMgrObject = new GameObject {
                    name = "AdvancedInputManager",
                    transform = {
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
                if (advInputMgrComponent != null) // Checks if our input manager component is null before checking.
                {
                    if (advInputMgrComponent.steamInputInitialized)
                    {
                        var inputHandleP1 = SteamInput.GetInputTypeForHandle(advInputMgrComponent.inputHandles[0]);
                        if (inputHandleP1 != ESteamInputType.k_ESteamInputType_Unknown)
                        {
                            switch (icon) {
                                case EnumIcon.PAD_BUTTON_L: // Square
                                    __result = Glyphs.GlyphX;
                                    break;
                                case EnumIcon.PAD_BUTTON_U: // Triangle
                                    __result = Glyphs.GlyphY;
                                    break;
                                case EnumIcon.PAD_BUTTON_R: // Circle
                                    __result = Glyphs.GlyphB;
                                    break;
                                case EnumIcon.PAD_BUTTON_D: // Cross
                                    __result = Glyphs.GlyphA;
                                    break;
                                case EnumIcon.PAD_MOVE:
                                    __result = Glyphs.GlyphLs;
                                    break;
                                case EnumIcon.PAD_MOVE_ALL:
                                    __result = Glyphs.GlyphLs;
                                    break;
                                case EnumIcon.PAD_MOVE_L: // L/U/R/D for some reason is mixed up. Here's hoping the analog stick and D-Pad directions aren't as much of a cluster fuck.
                                    __result = Glyphs.GlyphDPadRight;
                                    break;
                                case EnumIcon.PAD_MOVE_U:
                                    __result = Glyphs.GlyphDPadUp;
                                    break;
                                case EnumIcon.PAD_MOVE_R:
                                    __result = Glyphs.GlyphDPadDown;
                                    break;
                                case EnumIcon.PAD_MOVE_D:
                                    __result = Glyphs.GlyphDPadLeft;
                                    break;
                                case EnumIcon.PAD_MOVE_LR: // Like seriously, what was the person who coded this smoking? I thought pot was illegal in Japan, maybe paint thinner or computer duster? Unless something's not translated and just good-ole "Engrish" at play.
                                    // We need to look into cycling between left/right
                                    break;
                                case EnumIcon.PAD_MOVE_UD:
                                    // We need to look into cycling between up/down
                                    break;
                                case EnumIcon.PAD_L1:
                                    __result = Glyphs.GlyphLb;
                                    break;
                                case EnumIcon.PAD_R1:
                                    __result = Glyphs.GlyphRb;
                                    break;
                                case EnumIcon.PAD_L2:
                                    __result = Glyphs.GlyphLt;
                                    break;
                                case EnumIcon.PAD_R2:
                                    __result = Glyphs.GlyphRt;
                                    break;
                                case EnumIcon.PAD_L3:
                                    __result = Glyphs.GlyphLsClick;
                                    break;
                                case EnumIcon.PAD_R3:
                                    __result = Glyphs.GlyphRsClick;
                                    break;
                                case EnumIcon.PAD_L_STICK:
                                    __result = Glyphs.GlyphLs;
                                    break;
                                case EnumIcon.PAD_L_STICK_L:
                                    __result = Glyphs.GlyphLsLeft;
                                    break;
                                case EnumIcon.PAD_L_STICK_U:
                                    __result = Glyphs.GlyphLsUp;
                                    break;
                                case EnumIcon.PAD_L_STICK_R:
                                    __result = Glyphs.GlyphLsRight;
                                    break;
                                case EnumIcon.PAD_L_STICK_D:
                                    __result = Glyphs.GlyphLsDown;
                                    break;
                                case EnumIcon.PAD_L_STICK_LR:
                                    // We need to look into cycling between left/right
                                    break;
                                case EnumIcon.PAD_L_STICK_UD:
                                    // We need to look into cycling between up/down
                                    break;
                                case EnumIcon.PAD_R_STICK:
                                    __result = Glyphs.GlyphRs;
                                    break;
                                case EnumIcon.PAD_R_STICK_L:
                                    __result = Glyphs.GlyphRsLeft;
                                    break;
                                case EnumIcon.PAD_R_STICK_U:
                                    __result = Glyphs.GlyphRsUp;
                                    break;
                                case EnumIcon.PAD_R_STICK_R:
                                    __result = Glyphs.GlyphRsRight;
                                    break;
                                case EnumIcon.PAD_R_STICK_D:
                                    __result = Glyphs.GlyphRsDown;
                                    break;
                                case EnumIcon.PAD_R_STICK_LR:
                                    // We need to look into cycling between left/right
                                    break;
                                case EnumIcon.PAD_R_STICK_UD:
                                    // We need to look into cycling between up/down
                                    break;
                                case EnumIcon.PAD_CREATE:
                                    break;
                                case EnumIcon.PAD_OPTIONS:
                                    __result = Glyphs.GlyphStart;
                                    break;
                                case EnumIcon.PAD_TOUCH:
                                    __result = Glyphs.GlyphBack;
                                    break;
                                case EnumIcon.PAD_SELECT:
                                    __result = Glyphs.GlyphBack;
                                    break;
                                case EnumIcon.PAD_START:
                                    __result = Glyphs.GlyphStart;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (UnityEngine.InputSystem.Gamepad.all[0].device)
                        {
                            case DualSenseGamepadHID dualSenseGamepadHid:
                                switch (icon) {
                                    case EnumIcon.PAD_BUTTON_L: // Square
                                        break;
                                    case EnumIcon.PAD_BUTTON_U: // Triangle
                                        break;
                                    case EnumIcon.PAD_BUTTON_R: // Circle
                                        break;
                                    case EnumIcon.PAD_BUTTON_D: // Cross
                                        break;
                                    case EnumIcon.PAD_MOVE:
                                        break;
                                    case EnumIcon.PAD_MOVE_ALL:
                                        break;
                                    case EnumIcon.PAD_MOVE_L: // L/U/R/D for some reason is mixed up. Here's hoping the analog stick and D-Pad directions aren't as much of a cluster fuck.
                                        break;
                                    case EnumIcon.PAD_MOVE_U:
                                        break;
                                    case EnumIcon.PAD_MOVE_R:
                                        break;
                                    case EnumIcon.PAD_MOVE_D:
                                        break;
                                    case EnumIcon.PAD_MOVE_LR: // Like seriously, what was the person who coded this smoking? I thought pot was illegal in Japan, maybe paint thinner or computer duster? Unless something's not translated and just good-ole "Engrish" at play.
                                        break;
                                    case EnumIcon.PAD_MOVE_UD:
                                        break;
                                    case EnumIcon.PAD_L1:
                                        break;
                                    case EnumIcon.PAD_R1:
                                        break;
                                    case EnumIcon.PAD_L2:
                                        break;
                                    case EnumIcon.PAD_R2:
                                        break;
                                    case EnumIcon.PAD_L3:
                                        break;
                                    case EnumIcon.PAD_R3:
                                        break;
                                    case EnumIcon.PAD_L_STICK:
                                        break;
                                    case EnumIcon.PAD_L_STICK_L:
                                        break;
                                    case EnumIcon.PAD_L_STICK_U:
                                        break;
                                    case EnumIcon.PAD_L_STICK_R:
                                        break;
                                    case EnumIcon.PAD_L_STICK_D:
                                        break;
                                    case EnumIcon.PAD_L_STICK_LR:
                                        break;
                                    case EnumIcon.PAD_L_STICK_UD:
                                        break;
                                    case EnumIcon.PAD_R_STICK:
                                        break;
                                    case EnumIcon.PAD_R_STICK_L:
                                        break;
                                    case EnumIcon.PAD_R_STICK_U:
                                        break;
                                    case EnumIcon.PAD_R_STICK_R:
                                        break;
                                    case EnumIcon.PAD_R_STICK_D:
                                        break;
                                    case EnumIcon.PAD_R_STICK_LR:
                                        break;
                                    case EnumIcon.PAD_R_STICK_UD:
                                        break;
                                    case EnumIcon.PAD_CREATE:
                                        break;
                                    case EnumIcon.PAD_OPTIONS:
                                        break;
                                    case EnumIcon.PAD_TOUCH:
                                        break;
                                    case EnumIcon.PAD_SELECT:
                                        break;
                                    case EnumIcon.PAD_START:
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case DualShock3GamepadHID dualShock3GamepadHid:
                                
                                break;
                            case DualShock4GamepadHID dualShock4GamepadHid:
                                
                                break;
                            case SwitchProControllerHID switchProControllerHid:
                                
                                break;
                            case XInputControllerWindows xInputControllerWindows:
                                // Change Nothing.
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        public class UIPatches
        {
            // TODO:
            // 1. Adjust fullscreen fade effects (like during cutscenes, GameUiDungeonAreaMove/Canvas/Root/Back, or the load/save game prompt (GameUiSaveList/Root/Filter)) to take up the entire screen rather than a 16:9 portion.
            // 2. Adjust the RectTransform.OffsetMin of the In-Game Key Prompts to take up the horizontal aspect ratio equivalent of 2160p (For some reason, with 32:9, you have to have that set to 8000 instead of 7680, have to investigate)
            // 3. Adjust the RectTransform.AnchoredPosition of the Pause Menu prompts to a 16:9 position in the center of the screen. May have to adjust the vertical position and scale if the aspect ratio is less than 16:9.
            // 4. Adjust the scale of fullscreen UI elements to a 16:9 portion on the screen if the aspect ratio is less than 16:9.
            // 5. GameUiMainMenuStatus and GameUiMainMenuDisc's Canvas>Root components need an AspectRatioScaler with a 16:9 float value and an aspectMode of "FitInParent" to display properly.
            // 6. The BlackBarComponent actor should be placed on GameUiDungeonFullMap/Canvas, AdvIcon/Canvas, GameUiMainMenuBack/Cover, GameUiWorldMap.
            // 7. For some reason, when ADVs start, it does some really funny stuff with the screen aspect ratio. Try and investigate this, and have it use a 16:9 scale regardless of the resolution at the start.
            // 8. Adjust the AdvInterface elements to fit to a 16:9 aspect ratio portion on-screen. For some reason, it grows bigger the narrower the aspect ratio.
            // 9. Adjust the positions of UI elements in Dungeons and Battles based on if the user wants a spanned or centered UI.

            private const float OriginalAspectRatio = 1.7777778f;
            private static float _newSizeX = 3840f;
            private static float _newSizeY = 2160f;

            [HarmonyPatch(typeof(GuideMapShaderUtility), nameof(GuideMapShaderUtility.SetMaterialMaskParameter))]
            [HarmonyPrefix]
            public static bool CustomMaterialMaskParameter(Material out_material, Vector3 mask_position, Texture mask_texture, float mask_rotation_degree_z, float mask_scale)
            {
                out_material.SetTexture(MaskTexture, mask_texture);
                if (mask_texture == null) {
                    out_material.SetMatrix(MaskMatrixUV, Matrix4x4.identity);
                    return false;
                }
                Matrix4x4 matrix = Matrix4x4.identity;
                MGS_GM.Matrix_MulTranslate_Parent(ref matrix, 0.5f, 0.5f, 0f);
                MGS_GM.Matrix_MulRotZ_Parent(ref matrix, MGS_GM.DegToRad(mask_rotation_degree_z));
                // Add our new aspect ratio calculation logic.
                float currentAspectRatio = Screen.width / (float)Screen.height;
                if (currentAspectRatio > OriginalAspectRatio) {
                    _newSizeX = (float)Math.Round(3840f / (OriginalAspectRatio / currentAspectRatio));
                    _newSizeY = 2160f;
                }
                else if (currentAspectRatio < OriginalAspectRatio) {
                    // TODO: Figure out why narrower aspect ratios results in the left and right being cut off, alongside the top and bottom appearing more egg-like.
                    _newSizeX = 3840f;
                    _newSizeY = (float)Math.Round(2160f / (OriginalAspectRatio / currentAspectRatio));
                }
                MGS_GM.Matrix_MulScale_Parent(ref matrix, _newSizeX / mask_texture.width, _newSizeY / mask_texture.height, 1f);
                if (mask_scale != 0f) {
                    MGS_GM.Matrix_MulScale_Parent(ref matrix, 1f / (mask_scale * 2f), 1f / (mask_scale * 2f), 1f);
                }
                MGS_GM.Matrix_MulTranslate_Parent(ref matrix, (mask_position.x / Screen.width - 0.5f) * 2f * -1f, (mask_position.y / Screen.height - 0.5f) * 2f * -1f, 0f);
                out_material.SetMatrix(MaskMatrixUV, matrix);
                return false;
            }

            [HarmonyPatch(typeof(Game.UI.KeyAssign.Local.GameUiKeyAssignParts.Frame), "Awake")]
            [HarmonyPrefix]
            public static bool UpdatePosition(Game.UI.KeyAssign.Local.GameUiKeyAssignParts.Frame __instance)
            {
                // Check if belongs to a object named Frame2, and if so, change RectTransform.offsetMin to horizontal aspect ratio equivalent of 3840x2160
                // Check if belongs to a object named Frame0 (Pause Menu), and if so, change RectTransform.anchoredPosition to (-660,0) at 3440x1440, for example
                return true;
            }
            
            //[HarmonyPatch(typeof(SplashSequenceManager), "ShowImage", new Type[] { typeof(SplashMedia) })]
            //[HarmonyPatch(typeof(SplashSequenceManager), "PlayVideo", new Type[] { typeof(string), typeof(bool) })]
            //[HarmonyPostfix]
            //public static void SkipIntro()
            //{
                //instance.SkipCurrent(); // Need to write the proper functionality of this, but I just need to find a way of calling these functions when I want to skip opening logos/videos
                //}
        }

        [HarmonyPatch]
        public class ResolutionPatches
        {
            //[HarmonyPatch(typeof(GameScreen), nameof(GameScreen.EnumPattern), MethodType.Enumerator)]
            //[HarmonyTranspiler]
            //static IEnumerator customEnumPattern()
            //{
                
            //}
            
            //[HarmonyPatch(typeof(GameScreen), nameof(CalcSelectablePatternsCount))]

            //static GameScreen()
            //{
            //List<ResolutionManager.resolution> list = ResolutionManager.ScreenResolutions().ToList<ResolutionManager.resolution>();
            //GameScreen.pattern_ = new GameScreen.Pattern(list.Count, 0, 0);
            //GameScreen.patterns_ = new GameScreen.Pattern[list.Count];
            //for (int i = 0; i < GameScreen.patterns_.Length; i++)
            //{
            //GameScreen.patterns_[i] = new GameScreen.Pattern(i, list[i].width, list[i].height);
            //}
            //GameScreen.selectable_patterns_count_ = (uint)list.Count;
            //}

            [HarmonyPatch(typeof(GameUiConfigScreenResolutionList), "Entry")] // Modify Screen Resolution Selection
            [HarmonyPrefix]
            public static bool CustomResolutions()
            {
                var c = FindObjectsOfType<GameUiConfigScreenResolutionList>();
                _log.LogInfo("Found " + c[0].name + " possessing a GameUiConfigScreenResolutionList component.");
                var newResList = c[0].gameObject.AddComponent(typeof(CustomConfigScreenResolutionList)) as CustomConfigScreenResolutionList;
                var ogResList  = c[0].gameObject.GetComponent(typeof(GameUiConfigScreenResolutionList)) as GameUiConfigScreenResolutionList;
                if (newResList != null) {
                    newResList.Entry();
                    if (ogResList != null) {
                        ogResList.enabled = false; // Would probably be better if we just disabled the original component.
                    }
                }
                else { _log.LogError("New Resolution List returned null."); }
                return false;
            }
        }

        [HarmonyPatch]
        public class FOVPatches
        {
            // TODO:
            // Expose DungeonCamera's kFieldOfView parameter to a custom FOV option.
            // Look into figuring out where the Battle Camera's FOV parameter is being stored, and then expose that too.
            // Fix the "Object reference not set to an instance of an object" error with Cutscene FOV patching.
            // Check BattleCameraBase or some other class for battle FOV adjustment.

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
                if (_bMajorAxisFOVScaling.Value || _bPresentCutscenesWithOriginalAspectRatio.Value) {
                    Camera[] cameras = cinema.GetComponentsInChildren<Camera>(true);
                    foreach (Camera c in cameras) {
                        if (c != null) {
                            c.gateFit = Camera.GateFitMode.Overscan; // By default, cutscenes use the "Fill" GateFitMode, which is a terrible idea outside of 16:9. While setting it to "Overscan" does mildly affect composition, it's not a major concern.
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(SystemCamera3D), "Start")]
            [HarmonyPostfix]
            private static void CameraAspectRatioFixes()
            {
                if (_bMajorAxisFOVScaling.Value)
                {
                    SystemCamera3D.GetCamera().usePhysicalProperties = true;
                    SystemCamera3D.GetCamera().sensorSize = new Vector2(16f, 9f);
                    SystemCamera3D.GetCamera().gateFit = Camera.GateFitMode.Overscan;
                    _log.LogInfo("Modified SystemCamera3D Properties.");
                }
            }
        }

        [HarmonyPatch]
        public class FrameratePatches
        {
            // TODO:
            // 1. Fix ScaledDeltaTime to take GameTime.Speed into consideration, alongside patching any gameplay function that doesn't take time dilation into account.
            // This should in theory partially allow for time dilation adjustments during gameplay, if we want DMC-esque Turbo Mode.
            
            [HarmonyPatch(typeof(GameFrame), nameof(GameFrame.SetGameSceneFrameRateTarget), new Type[] { typeof(GameScene) })]
            [HarmonyPrefix]
            public static bool ModifyFramerateTarget()
            {
                Application.targetFrameRate = 0; // Disables the 60FPS limiter that takes place when VSync is disabled. We will be using our own framerate limiting logic anyways.
                QualitySettings.vSyncCount = SvSFix._bvSync.Value ? 1 : 0;
                GameFrame.now_target_frame_ = 0;
                GameTime.TargetFrameRate = 0;
                return false;
            }

            [HarmonyPatch(typeof(MapUnitCollisionCharacterControllerComponent), "FixedUpdate")]
            [HarmonyPatch(typeof(MapUnitCollisionRigidbodyComponent), "FixedUpdate")]
            [HarmonyPrefix]
            public static bool NullifyFixedUpdate()
            {
                return false; // We are simply going to tell FixedUpdate to fuck off, and then reimplement everything in an Update method.
            }
            
            [HarmonyPatch(typeof(MapUnitCollisionCharacterControllerComponent), nameof(MapUnitCollisionCharacterControllerComponent.Setup), new Type[]{ typeof(GameObject), typeof(float), typeof(float), typeof(MapUnitBaseComponent) })]
            [HarmonyPostfix]
            public static void ReplaceWithCustomCharacterControllerComponent()
            {
                _log.LogInfo("Hooked!");
                // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
                var c = FindObjectsOfType<MapUnitCollisionCharacterControllerComponent>();
                _log.LogInfo("Found " + c[0].name + " possessing a CharacterController component.");
                var newMuc = c[0].gameObject.AddComponent(typeof(CustomMapUnitController)) as CustomMapUnitController;
                var ogMuc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionCharacterControllerComponent)) as MapUnitCollisionCharacterControllerComponent;
                if (ogMuc != null) {
                    if (newMuc != null)
                    {
                        // Copies the properties of the original component before we opt out of using it, and use our own.
                        newMuc.character_controller_                   = ogMuc.character_controller_;
                        newMuc.collision_                              = ogMuc.collision_;
                        newMuc.rigid_body_                             = ogMuc.rigid_body_;
                        newMuc.character_controller_unit_radius_scale_ = ogMuc.character_controller_unit_radius_scale_;
                        ogMuc.enabled = false; // Would probably be better if we just disabled the original component.
                    }
                    else { _log.LogError("New Character Controller Component returned null."); }
                }
                else { _log.LogError("Original Character Controller Component returned null."); }
            }

            [HarmonyPatch(typeof(MapUnitCollisionRigidbodyComponent), nameof(MapUnitCollisionRigidbodyComponent.Setup), new Type[]{ typeof(GameObject), typeof(float), typeof(float), typeof(MapUnitBaseComponent) })]
            [HarmonyPostfix]
            public static void ReplaceWithCustomRigidBodyComponent()
            {
                _log.LogInfo("Hooked!");
                // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
                var c = FindObjectsOfType<MapUnitCollisionRigidbodyComponent>();
                _log.LogInfo("Found " + c[0].name + " possessing a RigidBodyController component.");
                var newRbc = c[0].gameObject.AddComponent( typeof(CustomRigidBodyController)) as CustomRigidBodyController;
                var ogRbc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionRigidbodyComponent)) as MapUnitCollisionRigidbodyComponent;
                if (ogRbc != null)
                {
                    if (newRbc != null)
                    {
                        // Copies the properties of the original component before we opt out of using it, and use our own.
                        newRbc.collision_ = ogRbc.collision_;
                        newRbc.character_controller_unit_radius_scale_ = ogRbc.character_controller_unit_radius_scale_;
                        newRbc.extrusion_speed_ = ogRbc.extrusion_speed_;
                        newRbc.hit_extrusion_count_ = ogRbc.hit_extrusion_count_;
                        newRbc.hit_extrusion_move_vector_power_ = ogRbc.hit_extrusion_move_vector_power_;
                        newRbc.hit_extrusion_vector_ = ogRbc.hit_extrusion_vector_;
                        newRbc.rigidbody_component_ = ogRbc.rigidbody_component_;
                        ogRbc.enabled = false; // Would probably be better if we just disabled the original component.
                    }
                    else { _log.LogError("New Rigid Body Component returned null."); }
                }
                else { _log.LogError("Original Rigid Body Component returned null."); }
            }
        }
    }
}