using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game.Cinema;
using Game.Input.Local;
using Game.UI;
using Game.UI.Battle;
using Game.UI.Dungeon;
using Game.UI.Local;
using Game.UI.MainMenu;
using Game.UI.MainMenu.Common;
using Game.UI.MainMenu.Disc;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Game.UI.MainMenu.Local;
using Game.UI.MainMenu.Mintubu;
using Game.UI.MainMenu.Status;
using Game.UI.MainMenu.Status.Parts;
using Game.UI.PhotoMode;
using IF.ED;
using IF.GameMain.Splash;
using IF.GameMain.Splash.Config;
using IF.PhotoMode.Control;
using IF.PhotoMode.Imaging;
using IF.Steam;
using IF.URP.RendererFeature.GaussianBlur;
using KingKrouch.Utility.Helpers;
using Steamworks;
using SvSFix.Controllers;
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
        // These are the glyphs that are going to be updated to cycle.
        public static Sprite GlyphLsUpDownPresent;
        public static Sprite GlyphLsLeftRightPresent;
        public static Sprite GlyphRsUpDownPresent;
        public static Sprite GlyphRsLeftRightPresent;
        public static Sprite GlyphDPadUpDownPresent;
        public static Sprite GlyphDPadLeftRightPresent;
        public static Sprite GlyphDPadFullPresent;
    }
    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("neptunia-sisters-vs-sisters.exe")]
    public class SvSFix : BaseUnityPlugin
    {
        private static ManualLogSource _log;

        public enum EPostAAType
        {
            Off,
            FXAA,
            SMAA
        };

        public enum EShadowQuality
        {
            Low, // 512
            Medium, // 1024
            High, // 2048
            Original, // 4096
            Ultra, // 8192
            Extreme // 16384
        };

        public static EPostAAType _confPostAAType;
        public static EShadowQuality _confShadowQuality;

        public enum EInputType
        {
            Automatic,
            KBM,
            Controller
        }

        public enum EControllerType
        {
            Automatic,
            Xbox,
            PS3,
            PS4,
            PS5,
            Switch
        }
        
        public static EInputType _confInputType;
        public static EControllerType _confControllerType;

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
        public static ConfigEntry<bool> _bOriginalUIAspectRatio; // On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.
        public static ConfigEntry<bool> _bPresentCutscenesWithOriginalAspectRatio; // On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).
        public static ConfigEntry<bool> _bMajorAxisFOVScaling; // On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.

        // Graphics Config
        public static ConfigEntry<int> _imsaaCount; // 0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.
        public static ConfigEntry<string> _sPostAAType; // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int> _resolutionScale; // Goes from 25% to 200%. Then it's adjusted to a floating point value between 0.25-2.00x.
        public static ConfigEntry<string> _sShadowQuality; // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int> _shadowCascades; // 0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)
        public static ConfigEntry<float> _fLodBias; // Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons.
        public static ConfigEntry<int> _iForcedLodQuality; // Default is 0, goes up to LOD #3 without cutting insane amounts of level geometry.
        public static ConfigEntry<int> _iForcedTextureQuality; // Default is 0, goes up to 1/14th resolution.
        public static ConfigEntry<int> _anisotropicFiltering; // 0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF.
        public static ConfigEntry<bool> _bPostProcessing; // Quick Toggle for Post-Processing

        // Framelimiter Config
        public static ConfigEntry<int> _iFrameInterval; // "0" disables the framerate cap, "1" caps at your screen refresh rate, "2" caps at half refresh, "3" caps at 1/3rd refresh, "4" caps at quarter refresh.
        public static ConfigEntry<bool> _bvSync; // Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.
        public static readonly int MaskMatrixUV = Shader.PropertyToID("mask_matrix_uv");
        public static readonly int MaskTexture = Shader.PropertyToID("mask_texture");
        
        // Input Config
        public static ConfigEntry<string> _sInputType; // Automatic, Controller, KBM (Forces a certain type of button prompts, Controller will be used if Steam Deck is detected).
        public static ConfigEntry<string> _sControllerType; // Automatic, Xbox, PS3, PS4, PS5, Switch (If SteamInput is enabled, "Automatic" will be used regardless of settings)
        public static ConfigEntry<bool> _bDisableSteamInput; // For those that don't want to use SteamInput, absolutely hate it being forced, and would rather use Unity's built-in input system.
        
        // Resolution Config
        public static ConfigEntry<bool> _bForceCustomResolution;
        public static ConfigEntry<int> _iHorizontalResolution;
        public static ConfigEntry<int> _iVerticalResolution;
        
        private void InitConfig()
        {
            // Aspect Ratio Config
            _bOriginalUIAspectRatio = Config.Bind("Resolution", "Original UI Aspect Ratio", true,
                "On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.");
            _bMajorAxisFOVScaling = Config.Bind("Resolution", "Major-Axis FOV Scaling", true,
                "On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.");
            _bPresentCutscenesWithOriginalAspectRatio = Config.Bind("Resolution", "Present Cutscenes At Original Aspect Ratio", false,
                "On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).");

            // Graphics Config
            _imsaaCount = Config.Bind("Graphics", "MSAA Quality", 0,
                new ConfigDescription("0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.",
                    new AcceptableValueRange<int>(0, 8)));

            _sPostAAType = Config.Bind("Graphics", "Post-Process AA", "SMAA", "Off, FXAA, SMAA");
            if (!Enum.TryParse(_sPostAAType.Value, out _confPostAAType)) {
                _confPostAAType = EPostAAType.SMAA;
                SvSFix._log.LogError($"PostAA Value is invalid. Defaulting to SMAA.");
            }

            _resolutionScale = Config.Bind("Graphics", "Resolution Scale", 100,
                new ConfigDescription("Goes from 25% to 200%.", new AcceptableValueRange<int>(25, 200)));

            _sShadowQuality = Config.Bind("Graphics", "Shadow Quality", "Original",
                "Low (512), Medium (1024), High (2048), Original (4096), Ultra (8192), Extreme (16384)");
            if (!Enum.TryParse(_sShadowQuality.Value, out _confShadowQuality)) {
                _confShadowQuality = EShadowQuality.Original;
                SvSFix._log.LogError($"ShadowQuality Value is invalid. Defaulting to Original.");
            }

            _shadowCascades = Config.Bind("Graphics", "Shadow Cascades", 4,
                new ConfigDescription("0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)",
                    new AcceptableValueRange<int>(0, 4)));
            
            _fLodBias = Config.Bind("Graphics", "Draw Distance (Lod Bias)", (float)1.00,
                new ConfigDescription(
                    "Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons."));

            _iForcedLodQuality = Config.Bind("Graphics", "LOD Quality", 0,
                new ConfigDescription("0: No Forced LODs (Default), 1: Forces LOD # 1, 2: Forces LOD # 2, 3: Forces LOD # 3. Higher the value, the less mesh detail.",
                    new AcceptableValueRange<int>(0, 3)));
            
            _iForcedTextureQuality = Config.Bind("Graphics", "Texture Quality", 0,
                new ConfigDescription("0: Full Resolution (Default), 1: Half-Res, 2: Quarter Res. Goes up to 1/14th res (14).",
                    new AcceptableValueRange<int>(0, 14)));
            
            _bPostProcessing = Config.Bind("Graphics", "Post-Processing", true,
                "On: Enables Post-Processing (Default), Off: Disables Post-Processing (Which may be handy for certain configurations)");
            
            _anisotropicFiltering = Config.Bind("Graphics", "Anisotropic Filtering", 0,
                new ConfigDescription("0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF",
                    new AcceptableValueRange<int>(0, 16)));

            // Framelimiter Config
            _iFrameInterval = Config.Bind("Framerate", "Framerate Cap Interval", 1,
                new ConfigDescription(
                    "0 disables the framerate limiter, 1 caps at your screen refresh rate, 2 caps at half refresh, 3 caps at 1/3rd refresh, 4 caps at quarter refresh.",
                    new AcceptableValueRange<int>(0, 4)));

            _bvSync = Config.Bind("Framerate", "VSync", true,
                "Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.");
            
            // Input Config
            _sInputType = Config.Bind("Input", "Input Type", "Automatic", "Automatic, KBM, Controller");
            if (!Enum.TryParse(_sInputType.Value, out _confInputType)) {
                _confInputType = EInputType.Automatic;
                SvSFix._log.LogError($"Input Type Value is invalid. Defaulting to Automatic.");
            }
            
            _sControllerType = Config.Bind("Input", "Controller Prompts Type", "Automatic", "Automatic, Xbox, PS3, PS4, PS5, Switch (If SteamInput is enabled, 'Automatic' will be used regardless of settings)");
            if (!Enum.TryParse(_sControllerType.Value, out _confControllerType)) {
                _confControllerType = EControllerType.Automatic;
                SvSFix._log.LogError($"Controller Type Value is invalid. Defaulting to Automatic.");
            }
            
            _bDisableSteamInput = Config.Bind("Input", "Force Disable SteamInput", false,
                "Self Explanatory. Prevents SteamInput from ever running, forcefully, for those using DS4Windows/DualSenseX or wanting native controller support. Make sure to disable SteamInput in the controller section of the game's properties on Steam alongside this option.");
            
            // Resolution Config
            _bForceCustomResolution = Config.Bind("Resolution", "Force Custom Resolution", false,
                "Self Explanatory. A temporary toggle for custom resolutions until I can figure out how to go about removing the resolution count restrictions.");
            _iHorizontalResolution = Config.Bind("Resolution", "Horizontal Resolution", 1280);
            _iVerticalResolution = Config.Bind("Resolution", "Vertical Resolution", 720);
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
            //var createdBlackBarActor = CreateBlackBarsActor();
            //if (createdBlackBarActor) {
                //_log.LogInfo("Adding BlackBarController Hooks.");
                //Harmony.CreateAndPatchAll(typeof(BlackBarControllerFunctionality));
            //}
            //else { Log.LogError("Couldn't create Pillarbox Actor."); }
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
            GameObject frObject = new GameObject {
                name = "FramerateLimiter",
                transform = {
                    position = new Vector3(0, 0, 0),
                    rotation = Quaternion.identity
                }
            };
            DontDestroyOnLoad(frObject);
            var frLimiterComponent = frObject.AddComponent<FramerateLimiter>();
            frLimiterComponent.fpsLimit = (double)Screen.currentResolution.refreshRate / _iFrameInterval.Value;
            return true;
        }

        public static bool CreateBlackBarsActor()
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
            Texture.masterTextureLimit      = _iForcedTextureQuality.Value; // Can raise this to force lower the texture size. Goes up to 14.
            QualitySettings.maximumLODLevel = _iForcedLodQuality.Value; // Can raise this to force lower the LOD settings. 3 at max if you want it to look like a blockout level prototype.
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
                    EPostAAType.FXAA => AntialiasingMode.FastApproximateAntialiasing,
                    EPostAAType.SMAA => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
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
                    case EInputType.Automatic:  __result = SingletonMonoBehaviour<GameInput>.Instance.Device; break;
                    case EInputType.KBM:        __result = GameInput.EnumDevice.kKeyboard;                    break;
                    case EInputType.Controller: __result = GameInput.EnumDevice.kGamepad;                     break;
                    default:                    __result = SingletonMonoBehaviour<GameInput>.Instance.Device; break;
                }
            }
            
            // GameUiBattleCommandMenu.InputKey probably has what we are looking for regarding custom controller prompt injection.

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
                        switch (icon) {
                            case EnumIcon.PAD_ENTER:      result = Glyphs.GlyphA;                    break;
                            case EnumIcon.PAD_BACK:       result = Glyphs.GlyphB;                    break;
                            case EnumIcon.PAD_BUTTON_L:   result = Glyphs.GlyphX;                    break; // Square
                            case EnumIcon.PAD_BUTTON_U:   result = Glyphs.GlyphY;                    break; // Triangle
                            case EnumIcon.PAD_BUTTON_R:   result = Glyphs.GlyphB;                    break; // Circle
                            case EnumIcon.PAD_BUTTON_D:   result = Glyphs.GlyphA;                    break; // Cross
                            case EnumIcon.PAD_MOVE:       result = Glyphs.GlyphLs;                   break;
                            case EnumIcon.PAD_MOVE_ALL:   result = Glyphs.GlyphLs;                   break;
                            case EnumIcon.PAD_MOVE_L:     result = Glyphs.GlyphDPadRight;            break; // L/U/R/D for some reason is mixed up. Here's hoping the analog stick and D-Pad directions aren't as much of a cluster fuck.
                            case EnumIcon.PAD_MOVE_U:     result = Glyphs.GlyphDPadUp;               break; // Like seriously, what was the person who coded this smoking? I thought pot was illegal in Japan, maybe paint thinner or computer duster? Unless something's not translated and just good-ole "Engrish" at play.
                            case EnumIcon.PAD_MOVE_R:     result = Glyphs.GlyphDPadDown;             break;
                            case EnumIcon.PAD_MOVE_D:     result = Glyphs.GlyphDPadLeft;             break;
                            case EnumIcon.PAD_MOVE_LR:    result = Glyphs.GlyphDPadLeftRightPresent; break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_MOVE_UD:    result = Glyphs.GlyphDPadUpDownPresent;    break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_L1:         result = Glyphs.GlyphLb;                   break;
                            case EnumIcon.PAD_R1:         result = Glyphs.GlyphRb;                   break;
                            case EnumIcon.PAD_L2:         result = Glyphs.GlyphLt;                   break;
                            case EnumIcon.PAD_R2:         result = Glyphs.GlyphRt;                   break;
                            case EnumIcon.PAD_L3:         result = Glyphs.GlyphLsClick;              break;
                            case EnumIcon.PAD_R3:         result = Glyphs.GlyphRsClick;              break;
                            case EnumIcon.PAD_L_STICK:    result = Glyphs.GlyphLs;                   break;
                            case EnumIcon.PAD_L_STICK_L:  result = Glyphs.GlyphLsLeft;               break;
                            case EnumIcon.PAD_L_STICK_U:  result = Glyphs.GlyphLsUp;                 break;
                            case EnumIcon.PAD_L_STICK_R:  result = Glyphs.GlyphLsRight;              break;
                            case EnumIcon.PAD_L_STICK_D:  result = Glyphs.GlyphLsDown;               break;
                            case EnumIcon.PAD_L_STICK_LR: result = Glyphs.GlyphLsLeftRightPresent;   break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_L_STICK_UD: result = Glyphs.GlyphLsUpDownPresent;      break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_R_STICK:    result = Glyphs.GlyphRs;                   break;
                            case EnumIcon.PAD_R_STICK_L:  result = Glyphs.GlyphRsLeft;               break;
                            case EnumIcon.PAD_R_STICK_U:  result = Glyphs.GlyphRsUp;                 break;
                            case EnumIcon.PAD_R_STICK_R:  result = Glyphs.GlyphRsRight;              break;
                            case EnumIcon.PAD_R_STICK_D:  result = Glyphs.GlyphRsDown;               break;
                            case EnumIcon.PAD_R_STICK_LR: result = Glyphs.GlyphRsLeftRightPresent;   break; // We need to look into cycling between left/right
                            case EnumIcon.PAD_R_STICK_UD: result = Glyphs.GlyphLsUpDownPresent;      break; // We need to look into cycling between up/down
                            case EnumIcon.PAD_CREATE:     result = original;                         break;
                            case EnumIcon.PAD_OPTIONS:    result = Glyphs.GlyphStart;                break;
                            case EnumIcon.PAD_TOUCH:      result = Glyphs.GlyphBack;                 break;
                            case EnumIcon.PAD_SELECT:     result = Glyphs.GlyphBack;                 break;
                            case EnumIcon.PAD_START:      result = Glyphs.GlyphStart;                break;
                            default:                      result = original;                         break;
                        }

                    }
                    else
                    {
                        if (UnityEngine.InputSystem.Gamepad.all[0].device != null) {
                            switch (UnityEngine.InputSystem.Gamepad.all[0].device) {
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
            public static bool in16x9Menu = false;

            private static AspectRatioFitter GameUiMainMenuStatusScaler;
            private static AspectRatioFitter GameUiMainMenuDiscScaler;
            private static AspectRatioFitter GameUiMainMenuMintubuScaler;
            private static AspectRatioFitter GameUiFullScreenMiniMapScaler;

            [HarmonyPatch(typeof(GameUiMainMenuMintubu), nameof(GameUiMainMenuMintubu.Open), new Type[] { typeof(bool) })]
            [HarmonyPostfix]
            public static void GameUiMainMenuMintubuOpen()
            {
                _log.LogInfo("Opened Chirper Menu.");
                // So we are essentially gonana look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
                var menuChirper = FindObjectsOfType<GameUiMainMenuMintubu>();
                //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
                if (GameUiMainMenuMintubuScaler == null)
                {
                    _log.LogInfo("Found " + menuChirper[0].name + " possessing a GameUiMainMenuMintubu component.");
                    var transform = menuChirper[0].transform.Find("Canvas/Root");
                    GameUiMainMenuMintubuScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                    if (GameUiMainMenuMintubuScaler != null) {
                        GameUiMainMenuMintubuScaler.aspectRatio = OriginalAspectRatio;
                        GameUiMainMenuMintubuScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    }
                }
            }

            [HarmonyPatch(typeof(SplashSequenceManager), nameof(SplashSequenceManager.Initialize), new Type[] { typeof(SplashSequence), typeof(Action) })]
            [HarmonyPostfix]
            public static void SplashSequenceManagerInit()
            {
                // TODO: Fix the anchoring issues in the game save notice screen.
            }

            //[HarmonyPatch(typeof(GameUiPhotoMode), nameof(GameUiPhotoMode.Ready))]
            //[HarmonyPostfix]
            public static void GameUiPhotoModeReady() // TODO: Fix hook not working.
            {
                _log.LogInfo("Opened Photo Mode.");
                // So we are essentially gonana look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
                var menuPhotoMode = FindObjectsOfType<GameUiPhotoMode>();
                //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
                _log.LogInfo("Found " + menuPhotoMode[0].name + " possessing a GameUiPhotoMode component.");
                var transform = menuPhotoMode[0].transform.Find("Canvas/Root");
                RectTransform photoModeTransform = transform.GetComponent<RectTransform>();
                
                float currentAspectRatio = Screen.width / (float)Screen.height;
                if (currentAspectRatio > OriginalAspectRatio) {
                    photoModeTransform.anchorMax = new Vector2(currentAspectRatio / OriginalAspectRatio, 0.5f);
                }
                else if (currentAspectRatio < OriginalAspectRatio) {
                    // TODO: Figure out why narrower aspect ratios results in the left and right being cut off, alongside the top and bottom appearing more egg-like.
                    photoModeTransform.anchorMax = new Vector2(0.5f, OriginalAspectRatio / currentAspectRatio);
                }
            }

            //[HarmonyPatch(typeof(GameUiMainMenuCharaSelect), nameof(GameUiMainMenuCharaSelect.SetActive), new Type[] { typeof(bool), typeof(bool) })]
            //[HarmonyPostfix]
            public static void GameUiMainMenuCharaSelectSetActive() // TODO: Fix hook not working.
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var gameUiMainMenuCharaSelect = FindObjectsOfType<GameUiMainMenuCharaSelect>();
                var transformCharaSelect = gameUiMainMenuCharaSelect[0].transform.Find("Canvas/Root");
                var rectTransformCharaSelect = transformCharaSelect.GetComponent<RectTransform>();
                
                if (currentAspectRatio > OriginalAspectRatio)
                {
                    float anchor = (float)Math.Round(1 - (((1 - (OriginalAspectRatio / currentAspectRatio)) * 0.5) / 0.5));
                    rectTransformCharaSelect.anchorMin = new Vector2(anchor, 1);
                    rectTransformCharaSelect.anchorMax = new Vector2(anchor,1);
                }
                else if (currentAspectRatio < OriginalAspectRatio) {
                    rectTransformCharaSelect.anchorMin = new Vector2(1,1);
                    rectTransformCharaSelect.anchorMax = new Vector2(1,1);
                }
            }

            [HarmonyPatch(typeof(EdManager), nameof(EdManager.Play))] // We need to adjust the positioning of the credits scrolling to a 16:9 portion on-screen
            [HarmonyPostfix]
            public static void EdManagerPlay()
            {
                _log.LogInfo("Movie Playing.");
                // For the ScrollView, we need to adjust the RectTransform.AnchorMin.x property to a centered 16:9 portion between 0 and 1.
            }

            [HarmonyPatch(typeof(AspectRatioFitter), "OnEnable")] // This should fix the credits video scaling.
            [HarmonyPostfix]
            public static void ForceAspectRatioFit(AspectRatioFitter __instance)
            {
                __instance.aspectRatio = OriginalAspectRatio;
                __instance.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }

            [HarmonyPatch(typeof(GameUiKeyAssign), nameof(GameUiKeyAssign.Open), new Type[] { typeof(bool) })]
            [HarmonyPostfix]
            public static void GameUiKeyAssignOpen(GameUiKeyAssign __instance)
            {
                // Fix text hints for ultrawide monitors
                FixFrame0(__instance);
                FixFrame2(__instance);
                FixFrame3(__instance);
            }
            
            public static void FixFrame0(GameUiKeyAssign keyAssign)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                if (keyAssign != null) {
                    var transformKeyAssign = keyAssign.transform.Find("Canvas/Root/Frame0/Trunk");
                    if (transformKeyAssign != null) {
                        var rectTransformKeyAssign = transformKeyAssign.GetComponent<RectTransform>(); // TODO: Why are you NULLing me? I'm right!
                        if (rectTransformKeyAssign != null) {
                            if (currentAspectRatio > OriginalAspectRatio) {
                                rectTransformKeyAssign.pivot = new Vector2(((1 - (OriginalAspectRatio / currentAspectRatio)) + 0.5f), 0.5f);
                            }
                            else if (currentAspectRatio < OriginalAspectRatio) {
                                rectTransformKeyAssign.pivot = new Vector2(0.5f, 0.5f);
                            }
                        }
                        else{ _log.LogError("rectTransformKeyAssign returned null."); }
                        if (GameUiFullScreenMiniMapScaler == null) {
                            _log.LogInfo("GameUiFullscreenMinimapScaler returned null, creating component.");
                            GameUiFullScreenMiniMapScaler = transformKeyAssign.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                            if (GameUiFullScreenMiniMapScaler != null) {
                                GameUiFullScreenMiniMapScaler.aspectRatio = OriginalAspectRatio;
                                GameUiFullScreenMiniMapScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                            }
                        }
                    }
                    else{ _log.LogError("transformKeyAssign returned null."); }
                }
                else{ _log.LogError("keyAssign returned null."); }
            }

            public static void FixFrame2(GameUiKeyAssign keyAssign)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transformFrame2 = keyAssign.transform.Find("Canvas/Root/Frame2");
                var rectTransformFrame2 = transformFrame2.GetComponent<RectTransform>();
                
                if (currentAspectRatio > OriginalAspectRatio) {
                    rectTransformFrame2.offsetMin = new Vector2((float)Math.Round(3840f / (OriginalAspectRatio / currentAspectRatio)) * -1, rectTransformFrame2.offsetMin.y);
                }
                else if (currentAspectRatio < OriginalAspectRatio) {
                    rectTransformFrame2.offsetMin = new Vector2(3840f * -1, rectTransformFrame2.offsetMin.y);
                }
            }

            public static void FixFrame3(GameUiKeyAssign keyAssign)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transformFrame3 = keyAssign.transform.Find("Canvas/Root/Frame3");
                var rectTransformFrame3 = transformFrame3.GetComponent<RectTransform>();
                
                if (currentAspectRatio > OriginalAspectRatio) {
                    rectTransformFrame3.offsetMin = new Vector2((float)Math.Round(3840f / (OriginalAspectRatio / currentAspectRatio)) * -1, rectTransformFrame3.offsetMin.y);
                }
                else if (currentAspectRatio < OriginalAspectRatio) {
                    rectTransformFrame3.offsetMin = new Vector2(3840f * -1, rectTransformFrame3.offsetMin.y);
                }
            }

            [HarmonyPatch(typeof(GameUiDungeonMemberStatus), nameof(GameUiDungeonMemberStatus.Open))]
            [HarmonyPostfix]
            public static void CharacterDungeonUiAnchoring(GameUiDungeonMemberStatus __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((float)(((1 - (OriginalAspectRatio / currentAspectRatio)) * 0.5) / 0.5), 0.5f) : new Vector2(0.5f, 0.5f);
                    }
                    else{ _log.LogError("GameUiDungeonMemberStatusRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiDungeonMemberStatusTransform returned null."); }
            }

            [HarmonyPatch(typeof(GameUiDungeonMiniMap), nameof(GameUiDungeonMiniMap.Open))]
            [HarmonyPostfix]
            public static void GameUiDungeonMinimapAnchoring(GameUiDungeonMiniMap __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = _bOriginalUIAspectRatio.Value ? new Vector2((OriginalAspectRatio / currentAspectRatio) ,rectTransform.anchorMax.y) : new Vector2(1, rectTransform.anchorMax.y);
                    }
                    else{ _log.LogError("GameUiDungeonMiniMapRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiDungeonMiniMapTransform returned null."); }
            }

            [HarmonyPatch(typeof(GameUiBattleComboSkill), nameof(GameUiBattleComboSkill.Open))]
            [HarmonyPostfix]
            public static void GameUiBattleComboSkillAnchoring(GameUiBattleComboSkill __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((OriginalAspectRatio / currentAspectRatio) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                    }
                    else{ _log.LogError("GameUiBattleComboSkillRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiBattleComboSkillTransform returned null."); }
            }
            
            [HarmonyPatch(typeof(GameUiBattleComboNum), nameof(GameUiBattleComboNum.Open))]
            [HarmonyPostfix]
            public static void GameUiBattleComboNumAnchoring(GameUiBattleComboNum __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((OriginalAspectRatio / currentAspectRatio) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                    }
                    else{ _log.LogError("GameUiBattleComboNumRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiBattleComboNumTransform returned null."); }
            }
            
            [HarmonyPatch(typeof(GameUiBattleChainRecommend), nameof(GameUiBattleChainRecommend.OpenL))]
            [HarmonyPatch(typeof(GameUiBattleChainRecommend), nameof(GameUiBattleChainRecommend.OpenR))]
            [HarmonyPostfix]
            public static void GameUiBattleChainRecommendAnchoring(GameUiBattleChainRecommend __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((OriginalAspectRatio / currentAspectRatio) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                    }
                    else{ _log.LogError("GameUiBattleChainRecommendRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiBattleChainRecommendTransform returned null."); }
            }
            
            [HarmonyPatch(typeof(GameUiBattleTacticalSkill), nameof(GameUiBattleTacticalSkill.Open))]
            [HarmonyPostfix]
            public static void GameUiBattleTacticalSkillAnchoring(GameUiBattleTacticalSkill __instance)
            {
                var currentAspectRatio = Screen.width / (float)Screen.height;
                var transform = __instance.transform.Find("Canvas/Root");
                if (transform != null) {
                    var rectTransform = transform.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((OriginalAspectRatio / currentAspectRatio) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                    }
                    else{ _log.LogError("GameUiBattleTacticalSkillRectTransform returned null."); }
                }
                else{ _log.LogError("GameUiBattleTacticalSkillTransform returned null."); }
            }
            
            

            [HarmonyPatch(typeof(GameUiMainMenuMintubu), "Close")]
            [HarmonyPostfix]
            public static void GameUiMainMenuMintubuClose()
            {
                _log.LogInfo("Closed Chirper Menu.");
            }

            [HarmonyPatch(typeof(GameUiMainMenuStatus), nameof(GameUiMainMenuStatus.Open))]
            [HarmonyPostfix]
            public static void GameUiMainMenuStatusOpen()
            {
                _log.LogInfo("Opened Status Menu.");
                // So we are essentially gonana look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
                var menuStatus = FindObjectsOfType<GameUiMainMenuStatus>();
                //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
                if (GameUiMainMenuStatusScaler == null)
                {
                    _log.LogInfo("Found " + menuStatus[0].name + " possessing a GameUiMainMenuStatus component.");
                    var transform = menuStatus[0].transform.Find("Canvas/Root");
                    GameUiMainMenuStatusScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                    if (GameUiMainMenuStatusScaler != null) {
                        GameUiMainMenuStatusScaler.aspectRatio = OriginalAspectRatio;
                        GameUiMainMenuStatusScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    }
                }
            }

            [HarmonyPatch(typeof(GameUiMainMenuStatus), "Close")]
            [HarmonyPostfix]
            public static void GameUiMainMenuStatusClose()
            {
                _log.LogInfo("Closed Status Menu.");
            }
            
            [HarmonyPatch(typeof(GameUiMainMenuDisc), nameof (GameUiMainMenuDisc.Open))]
            [HarmonyPostfix]
            public static void GameUiMainMenuDiscOpen()
            {
                // So we are essentially gonana look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
                var menuDisc = FindObjectsOfType<GameUiMainMenuDisc>();
                if (GameUiMainMenuDiscScaler == null)
                {
                    _log.LogInfo("Found " + menuDisc[0].name + " possessing a GameUiMainMenuDisc component.");
                    var transform = menuDisc[0].transform.Find("Canvas/Root");
                    GameUiMainMenuDiscScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                    if (GameUiMainMenuDiscScaler != null) {
                        GameUiMainMenuDiscScaler.aspectRatio = OriginalAspectRatio;
                        GameUiMainMenuDiscScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    }
                }
            }

            [HarmonyPatch(typeof(GameUiDungeonAreaMove), nameof(GameUiDungeonAreaMove.Open), new Type[] { typeof(int) })]
            [HarmonyPostfix]
            public static void GameUiDungeonAreaMoveOpen()
            {
                _log.LogInfo("Opened Dungeon Area Move Menu.");
                var dungeonAreaMove = FindObjectsOfType<GameUiDungeonAreaMove>();
                var uiOverlay = dungeonAreaMove[0].gameObject.transform.Find("Canvas/Root/Back");
                uiOverlay.localScale = new Vector3(Screen.width, Screen.height, 1.00f);
            }
            
            [HarmonyPatch(typeof(GameUiDungeonAreaMove), nameof(GameUiDungeonAreaMove.Close))]
            [HarmonyPostfix]
            public static void GameUiDungeonAreaMoveClose()
            {
                _log.LogInfo("Closed Dungeon Area Move Menu.");
            }

            [HarmonyPatch(typeof(GameUiDungeonFullMap), nameof(GameUiDungeonFullMap.Open))]
            [HarmonyPostfix]
            public static void GameUiDungeonFullMapOpen()
            {
                _log.LogInfo("Closed Dungeon Full Map Menu.");
            }

            [HarmonyPatch(typeof(GameUiDungeonFullMap), nameof(GameUiDungeonFullMap.Close))]
            [HarmonyPostfix]
            public static void GameUiDungeonFullMapClose()
            {
                _log.LogInfo("Closed Dungeon Full Map Menu.");
            }

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