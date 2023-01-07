﻿using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game.UI.Config;
using Game.UI.MainMenu;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Game.UI.MainMenu.Local;
using IF.Steam;
using KingKrouch.Utility.Helpers;
using Steamworks;
using SvSFix.Controllers;
using SvSFix.ResolutionClasses;

namespace SvSFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("neptunia-sisters-vs-sisters.exe")]
    public partial class SvSFix : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        public enum ePostAAType
        {
            Off,
            FXAA,
            SMAA
        };

        public enum eShadowQuality
        {
            Low, // 512
            Medium, // 1024
            High, // 2048
            Original, // 4096
            Ultra, // 8192
            Extreme // 16384
        };

        public static ePostAAType confPostAAType;
        public static eShadowQuality confShadowQuality;

        public static Vector2 shadowResVec()
        {
            switch (confShadowQuality) {
            case eShadowQuality.Low:
                return new Vector2(512, 512);
            case eShadowQuality.Medium:
                return new Vector2(1024, 1024);
            case eShadowQuality.High: 
                return new Vector2(2048, 2048);
            case eShadowQuality.Original:
                return new Vector2(4096, 4096);
            case eShadowQuality.Ultra:
                return new Vector2(8192, 8192);
            case eShadowQuality.Extreme:
                return new Vector2(16384, 16384);
            default:
                return new Vector2(4096, 4096);
            }
        }
        
        static BlackBarController controllerComponent;
        private static SvSFix _instance;

        // Aspect Ratio Config
        public static ConfigEntry<bool> bOriginalUIAspectRatio; // On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.
        public static ConfigEntry<bool> bPresentCutscenesWithOriginalAspectRatio; // On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).
        public static ConfigEntry<bool> bMajorAxisFOVScaling; // On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.

        // Graphics Config
        public static ConfigEntry<int> iMSAACount; // 0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.
        public static ConfigEntry<string> sPostAAType; // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int> iResolutionScale; // Goes from 25% to 200%. Then it's adjusted to a floating point value between 0.25-2.00x.
        public static ConfigEntry<string> sShadowQuality; // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int> iShadowCascades; // 0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)
        public static ConfigEntry<float> fLodBias; // Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons.
        public static ConfigEntry<int> iForcedLodQuality; // Default is 0, goes up to LOD #3 without cutting insane amounts of level geometry.
        public static ConfigEntry<int> iForcedTextureQuality; // Default is 0, goes up to 1/14th resolution.
        public static ConfigEntry<int> iAnisotropicFiltering; // 0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF.
        public static ConfigEntry<bool> bPostProcessing; // Quick Toggle for Post-Processing

        // Framelimiter Config
        public static ConfigEntry<int> iFrameInterval; // "0" disables the framerate cap, "1" caps at your screen refresh rate, "2" caps at half refresh, "3" caps at 1/3rd refresh, "4" caps at quarter refresh.
        public static ConfigEntry<bool> bVSync; // Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.

        public SvSFix() {
            _instance = this;
        }

        private void InitConfig()
        {
            // Aspect Ratio Config
            bOriginalUIAspectRatio = Config.Bind("Resolution", "OriginalUIAspectRatio", true,
                "On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.");
            bMajorAxisFOVScaling = Config.Bind("Resolution", "MajorAxisFOVScaling", true,
                "On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.");
            bPresentCutscenesWithOriginalAspectRatio = Config.Bind("Resolution", "PresentCutscenesAtOriginalAspectRatio", false,
                "On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).");

            // Graphics Config
            iMSAACount = Config.Bind("Graphics", "MSAACount", (int)0,
                new ConfigDescription("0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.",
                    new AcceptableValueRange<int>(0, 8)));

            sPostAAType = Config.Bind("Graphics", "PostAA", "SMAA", "Off, FXAA, SMAA");
            if (!Enum.TryParse<ePostAAType>(sPostAAType.Value, out confPostAAType)) {
                confPostAAType = ePostAAType.SMAA;
                SvSFix.Log.LogError($"PostAA Value is invalid. Defaulting to SMAA.");
            }

            iResolutionScale = Config.Bind("Graphics", "ResolutionScale", (int)100,
                new ConfigDescription("Goes from 25% to 200%.", new AcceptableValueRange<int>(25, 200)));
            float fResolutionScale = iResolutionScale.Value / 100; // Converts the render percentage to something that Unity will take.

            sShadowQuality = Config.Bind("Graphics", "ShadowQuality", "Original",
                "Low (512), Medium (1024), High (2048), Original (4096), Ultra (8192), Extreme (16384)");
            if (!Enum.TryParse<eShadowQuality>(sShadowQuality.Value, out confShadowQuality)) {
                confShadowQuality = eShadowQuality.Original;
                SvSFix.Log.LogError($"ShadowQuality Value is invalid. Defaulting to Original.");
            }

            iShadowCascades = Config.Bind("Graphics", "ShadowCascades", (int)4,
                new ConfigDescription("0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)",
                    new AcceptableValueRange<int>(0, 4)));
            
            fLodBias = Config.Bind("Graphics", "LodBias", (float)1.00,
                new ConfigDescription(
                    "Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons."));

            iForcedLodQuality = Config.Bind("Graphics", "ForcedLODQuality", (int)0,
                new ConfigDescription("0: No Forced LODs (Default), 1: Forces LOD # 1, 2: Forces LOD # 2, 3: Forces LOD # 3. Higher the value, the less mesh detail.",
                    new AcceptableValueRange<int>(0, 3)));
            
            iForcedTextureQuality = Config.Bind("Graphics", "ForcedTextureQuality", (int)0,
                new ConfigDescription("0: Full Resolution (Default), 1: Half-Res, 2: Quarter Res. Goes up to 1/14th res (14).",
                    new AcceptableValueRange<int>(0, 14)));
            
            bPostProcessing = Config.Bind("Graphics", "PostProcessing", true,
                "On: Enables Post Processing (Default), Off: Disables Post Processing (Which may be handy for certain configurations)");

            

            iAnisotropicFiltering = Config.Bind("Graphics", "AnisotropicFiltering", (int)0,
                new ConfigDescription("0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF",
                    new AcceptableValueRange<int>(0, 16)));

            // Framelimiter Config
            iFrameInterval = Config.Bind("Framerate", "Framerate Cap Interval", (int)1,
                new ConfigDescription(
                    "0 disables the framerate cap, 1 caps at your screen refresh rate, 2 caps at half refresh, 3 caps at 1/3rd refresh, 4 caps at quarter refresh.",
                    new AcceptableValueRange<int>(0, 4)));

            bVSync = Config.Bind("Framerate", "VSync", true,
                "Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.");
        }

        private static void UpdateCutsceneCameras() // This should be an easy fix to get the cutscene cameras to display with major axis scaling.
        {
            var cameras = FindObjectsOfType<Camera>();
            foreach (var c in cameras)
            { // TODO: Figure out how to stop the NullReferenceException errors. This logic otherwise seems to work.
                if (c.transform.parent.gameObject.GetComponent<SystemCamera3D>() == null) { // Check if SystemCamera3D doesn't exist. We don't want to overwrite this if the user doesn't opt to use Major Axis Scaling but also wants pillarboxed cutscenes.
                    c.gateFit = Camera.GateFitMode.Overscan; // If it doesn't, then overwrite the Gate Fit Mode to use Overscan Scaling.
                }
            }
        }
        
        private void FixedUpdate() // We only need to do this a certain amount of times per second.
        {
            //if (bMajorAxisFOVScaling.Value == true || bPresentCutscenesWithOriginalAspectRatio.Value == true) {
                //UpdateCutsceneCameras();
            //}
        }

        private void Awake()
        {
            _instance = this;
            SvSFix.Log = base.Logger;
            // Plugin startup logic
            SvSFix.Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Reads or creates our config file.
            InitConfig();
            LoadGraphicsSettings(); //TODO: Figure out why this is spitting an error
            // Creates our custom components, and prints a log statement if it failed or not.
            var createdBlackBarActor = CreateBlackBars();
            if (createdBlackBarActor) {
                Log.LogInfo("Adding BlackBarController Hooks.");
                Harmony.CreateAndPatchAll(typeof(BlackBarControllerFunctionality));
            }
            //else { Log.LogError("Couldn't create Pillarbox Actor."); }
            var createdFramelimiter = InitializeFramelimiter();
            if (createdFramelimiter) {
                Log.LogInfo("Created Framelimiter.");
            }
            else { Log.LogError("Couldn't create Framelimiter Actor."); }
            
            // Patches the in-game camera to use Major Axis Scaling.
            if (bMajorAxisFOVScaling.Value == true) {
                Harmony.CreateAndPatchAll(typeof(FOVPatch));
            }
            // Finally, runs our UI and Framerate Patches.
            Harmony.CreateAndPatchAll(typeof(UIPatches));
            Harmony.CreateAndPatchAll(typeof(FrameratePatches));
            Harmony.CreateAndPatchAll(typeof(SteamworksFunctionality));
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
            frLimiterComponent.fpsLimit = (double)Screen.currentResolution.refreshRate / iFrameInterval.Value;
            return true;
        }

        private static bool CreateBlackBars()
        {
            // Creates our BlackBarController prefab by hooking into Unity's AssetBundles system.
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
                controllerComponent = blackBarController.AddComponent<BlackBarController>();
                controllerComponent.letterboxTop = (Image)prefab.transform.Find("Letterbox/Top")
                    .GetComponentInChildren(typeof(Image), true);
                controllerComponent.letterboxBottom = (Image)prefab.transform.Find("Letterbox/Bottom")
                    .GetComponentInChildren(typeof(Image), true);
                controllerComponent.pillarboxLeft = (Image)prefab.transform.Find("Pillarbox/Left")
                    .GetComponentInChildren(typeof(Image), true);
                controllerComponent.pillarboxRight = (Image)prefab.transform.Find("Pillarbox/Right")
                    .GetComponentInChildren(typeof(Image), true);
                controllerComponent.opacity = 1.0f;
                Log.LogInfo("Created BlackBarController Actor.");
                return true;
            }
            else {
                Log.LogError("Couldn't Spawn BlackBarController Actor.");
                return false;
            }
        }

        public static void CreateNewPromptImages()
        {
            Sprite button_sankaku_new = new Sprite();
            Sprite button_sikaku_new  = new Sprite();
            Sprite button_batu_new    = new Sprite();
            Sprite button_maru_new    = new Sprite();
            // GameUiKeyAssign/Canvas/Root/Frame0/Trunk/Node(Clone)/Pad/Key/Icon is what needs it's image reference modified to point to our own sprites rather than the game's.
            // GameUiKeyAssignParts.Node.list_sprites_ seemingly contains a list of sprites
        }

        private static void LoadGraphicsSettings()
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(iAnisotropicFiltering.Value, iAnisotropicFiltering.Value);
            Texture.masterTextureLimit      = iForcedTextureQuality.Value; // Can raise this to force lower the texture size. Goes up to 14.
            QualitySettings.maximumLODLevel = iForcedLodQuality.Value; // Can raise this to force lower the LOD settings. 3 at max if you want it to look like a blockout level prototype.
            QualitySettings.lodBias         = fLodBias.Value;
            // Let's adjust some of the Render Pipeline Settings during runtime.
            //var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var asset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

            asset.renderScale = (float)iResolutionScale.Value / 100;
            //asset.mainLightShadowmapResolution        = (int)shadowResVec().x; // TODO: Find a way to write to this.
            //asset.additionalLightsShadowmapResolution = (int)shadowResVec().y;
            asset.msaaSampleCount = iMSAACount.Value;
            asset.shadowCascadeCount = iShadowCascades.Value;
            QualitySettings.renderPipeline = asset;
            
            // Now let's adjust the post-processing settings for the camera.
            var cameraData = FindObjectsOfType<UniversalAdditionalCameraData>();
            foreach (var c in cameraData)
            {
                c.antialiasing = confPostAAType switch {
                    ePostAAType.Off => AntialiasingMode.None,
                    ePostAAType.FXAA => AntialiasingMode.FastApproximateAntialiasing,
                    ePostAAType.SMAA => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    _ => throw new ArgumentOutOfRangeException()
                };
                c.renderPostProcessing = bPostProcessing.Value;
            }
        }
        [HarmonyPatch]
        public class SteamworksFunctionality
        {
            [HarmonyPatch(typeof(SteamworksAccessor), nameof(SteamworksAccessor.Initialize))]
            [HarmonyPostfix]
            public static void SteamworksInitExtra()
            {
                GameObject advInputMgrObject = new GameObject {
                    name = "AdvancedInputManager",
                    transform = {
                        position = new Vector3(0, 0, 0),
                        rotation = Quaternion.identity
                    }
                };
                DontDestroyOnLoad(advInputMgrObject);
                var advInputMgr = advInputMgrObject.AddComponent<InputManager>();
                Log.LogInfo("Connected Controller 1: " + SteamInput.GetInputTypeForHandle(advInputMgr.inputHandles[0]));
                return;
            }
        }

        [HarmonyPatch]
        public class BlackBarControllerFunctionality
        {
            [HarmonyPatch(typeof(GameUiMainMenuController), nameof(GameUiMainMenuController.Close), new Type[]{typeof(bool)})]
            [HarmonyPostfix]
            public static void FadeOutBlackBars(GameUiMainMenuController __instance)
            {
                Log.LogInfo("Closed Pause Menu.");
                //_instance.StartCoroutine(controllerComponent.FadeOutBlackBars());
            }
            [HarmonyPatch(typeof(GameUiMainMenuController), nameof(GameUiMainMenuController.Open), new Type[]{typeof(bool), typeof(MenuContentsExceptionFlag)})]
            [HarmonyPostfix]
            public static void FadeInBlackBars(GameUiMainMenuController __instance)
            {
                Log.LogInfo("Opened Pause Menu.");
                //StartCoroutine(controllerComponent.FadeInBlackBars());
            }
        }

        [HarmonyPatch]
        public class UIPatches
        {
            private static float originalAspectRatio = 1.7777778f;
            private static float newSizeX = 3840f;
            private static float newSizeY = 2160f;
            
            [HarmonyPatch(typeof(GuideMapShaderUtility), nameof(GuideMapShaderUtility.SetMaterialMaskParameter))]
            [HarmonyPrefix]
            public static bool CustomMaterialMaskParameter(Material out_material, Vector3 mask_position, Texture mask_texture, float mask_rotation_degree_z, float mask_scale)
            {
                out_material.SetTexture("mask_texture", mask_texture);
                if (mask_texture == null)
                {
                    out_material.SetMatrix("mask_matrix_uv", Matrix4x4.identity);
                    return false;
                }
                Matrix4x4 matrix = Matrix4x4.identity;
                MGS_GM.Matrix_MulTranslate_Parent(ref matrix, 0.5f, 0.5f, 0f);
                MGS_GM.Matrix_MulRotZ_Parent(ref matrix, MGS_GM.DegToRad(mask_rotation_degree_z));
                // Add our new aspect ratio calculation logic.
                float currentAspectRatio = (float)Screen.width / (float)Screen.height;
                if (currentAspectRatio > originalAspectRatio) {
                    newSizeX = (float)Math.Round(3840f / (originalAspectRatio / currentAspectRatio));
                    newSizeY = 2160f;
                }
                else if (currentAspectRatio < originalAspectRatio) { // TODO: Figure out why narrower aspect ratios results in the left and right being cut off.
                    newSizeX = 3840f;
                    newSizeY = (float)Math.Round(2160f / (originalAspectRatio / currentAspectRatio));
                }
                MGS_GM.Matrix_MulScale_Parent(ref matrix, newSizeX / (float)mask_texture.width, newSizeY / (float)mask_texture.height, 1f);
                if (mask_scale != 0f) {
                    MGS_GM.Matrix_MulScale_Parent(ref matrix, 1f / (mask_scale * 2f), 1f / (mask_scale * 2f), 1f);
                }
                MGS_GM.Matrix_MulTranslate_Parent(ref matrix, (mask_position.x / (float)Screen.width - 0.5f) * 2f * -1f, (mask_position.y / (float)Screen.height - 0.5f) * 2f * -1f, 0f);
                out_material.SetMatrix("mask_matrix_uv", matrix);
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
                Log.LogInfo("Found " + c[0].name + " possessing a GameUiConfigScreenResolutionList component.");
                var newResList = c[0].gameObject.AddComponent(typeof(CustomConfigScreenResolutionList)) as CustomConfigScreenResolutionList;
                var ogResList  = c[0].gameObject.GetComponent(typeof(GameUiConfigScreenResolutionList)) as GameUiConfigScreenResolutionList;
                newResList.Entry();
                if (ogResList != null) {
                    ogResList.enabled = false; // Would probably be better if we just disabled the original component.
                }
                return false;
            }
            
            
        }

        [HarmonyPatch]
        public class FOVPatch
        {
            [HarmonyPatch(typeof(SystemCamera3D), "Start")]
            [HarmonyPostfix]
            private static void CameraAspectRatioFixes(SystemCamera3D __instance) // TODO: Find a way of fixing cutscene cameras.
            {
                SystemCamera3D.GetCamera().usePhysicalProperties = true;
                SystemCamera3D.GetCamera().sensorSize = new Vector2(16f, 9f);
                SystemCamera3D.GetCamera().gateFit = Camera.GateFitMode.Overscan;
                Log.LogInfo("Modified SystemCamera3D Properties.");
            }
        }

        [HarmonyPatch]
        public class FrameratePatches
        {
            [HarmonyPatch(typeof(GameFrame), nameof(GameFrame.SetGameSceneFrameRateTarget), new Type[] { typeof(GameScene) })]
            [HarmonyPrefix]
            public static bool ModifyFramerateTarget()
            {
                Application.targetFrameRate = 0; // Disables the 60FPS limiter that takes place when VSync is disabled. We will be using our own framerate limiting logic anyways.
                QualitySettings.vSyncCount = SvSFix.bVSync.Value ? 1 : 0;
                GameFrame.now_target_frame_ = 0;
                GameTime.TargetFrameRate = 0;
                return false;
            }

            // TODO: Fix ScaledDeltaTimeFix.
            //[HarmonyPatch(typeof(GameTime), nameof(GameTime.ScaledDeltaTime))]
            //[HarmonyPostfix]
            //public static float ScaledDeltaTimeFix(ref float __result) // This should in theory partially allow for time dilation adjustments during gameplay, if we want DMC-esque Turbo Mode.
            //{
                //Debug.Log("Hooked ScaledDeltaTime.");
                //return __result * GameTime.Speed;
            //}
            
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
                Log.LogInfo("Hooked!");
                // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
                var c = FindObjectsOfType<MapUnitCollisionCharacterControllerComponent>();
                Log.LogInfo("Found " + c[0].name + " possessing a CharacterController component.");
                var newMuc = c[0].gameObject.AddComponent(typeof(CustomMapUnitController)) as CustomMapUnitController;
                var ogMuc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionCharacterControllerComponent)) as MapUnitCollisionCharacterControllerComponent;
                if (ogMuc == null) { Log.LogError("Original Character Controller Component returned null."); return; }
                // Copies the properties of the original component before we opt out of using it, and use our own.
                newMuc.character_controller_                   = ogMuc.character_controller_;
                newMuc.collision_                              = ogMuc.collision_;
                newMuc.rigid_body_                             = ogMuc.rigid_body_;
                newMuc.character_controller_unit_radius_scale_ = ogMuc.character_controller_unit_radius_scale_;
                ogMuc.enabled = false; // Would probably be better if we just disabled the original component.
                
            }

            [HarmonyPatch(typeof(MapUnitCollisionRigidbodyComponent), nameof(MapUnitCollisionRigidbodyComponent.Setup), new Type[]{ typeof(GameObject), typeof(float), typeof(float), typeof(MapUnitBaseComponent) })]
            [HarmonyPostfix]
            public static void ReplaceWithCustomRigidBodyComponent()
            {
                Log.LogInfo("Hooked!");
                // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
                var c = FindObjectsOfType<MapUnitCollisionRigidbodyComponent>();
                Log.LogInfo("Found " + c[0].name + " possessing a RigidBodyController component.");
                var newRbc = c[0].gameObject.AddComponent( typeof(CustomRigidBodyController)) as CustomRigidBodyController;
                var ogRbc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionRigidbodyComponent)) as MapUnitCollisionRigidbodyComponent;
                if (newRbc == null) { Log.LogError("Original Rigid Body Component returned null."); return; }
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
        }
    }
}