// BepInEx and Harmony Stuff
using BepInEx.Configuration;
// Unity and System Stuff
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SvSFix;

public partial class SvSFix
{
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
            return _confShadowQuality switch {
                EShadowQuality.Low      => new Vector2(512,   512),
                EShadowQuality.Medium   => new Vector2(1024,  1024),
                EShadowQuality.High     => new Vector2(2048,  2048),
                EShadowQuality.Original => new Vector2(4096,  4096),
                EShadowQuality.Ultra    => new Vector2(8192,  8192),
                EShadowQuality.Extreme  => new Vector2(16384, 16384),
                _                       => new Vector2(4096,  4096)
            };
        }

        private static string path = @"BepInEx\content\svsfix_content";
        public static AssetBundle blackBarControllerBundle = AssetBundle.LoadFromFile(path);
        
        public static bool restrictAdvUiTo16x9 = true;

        // Aspect Ratio Config
        public static ConfigEntry<bool> _bOriginalUIAspectRatio; // On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.
        public static ConfigEntry<bool> _bPresentCutscenesWithOriginalAspectRatio; // On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).
        public static ConfigEntry<bool> _bMajorAxisFOVScaling; // On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.

        // Graphics Config
        public static ConfigEntry<int>    _imsaaCount;                  // 0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.
        public static ConfigEntry<string> _sPostAAType;                 // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int>    _resolutionScale;             // Goes from 25% to 200%. Then it's adjusted to a floating point value between 0.25-2.00x.
        public static ConfigEntry<string> _sShadowQuality;              // Going to convert an string to one of the enumerator values.
        public static ConfigEntry<int>    _shadowCascades;              // 0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)
        public static ConfigEntry<float>  _fLodBias;                    // Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons.
        public static ConfigEntry<int>    _iForcedLodQuality;           // Default is 0, goes up to LOD #3 without cutting insane amounts of level geometry.
        public static ConfigEntry<int>    _iForcedTextureQuality;       // Default is 0, goes up to 1/14th resolution.
        public static ConfigEntry<int>    _anisotropicFiltering;        // 0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF.
        public static ConfigEntry<bool>   _bPostProcessing;             // Quick Toggle for Post-Processing
        public static ConfigEntry<bool>   _screenSpaceAmbientOcclusion; // Toggle for SSAO.
        public static ConfigEntry<int>    _grassDensity;                // Goes from 0% to 100%.

        // Framelimiter Config
        public static ConfigEntry<int> _iFrameInterval; // "0" disables the framerate cap, "1" caps at your screen refresh rate, "2" caps at half refresh, "3" caps at 1/3rd refresh, "4" caps at quarter refresh.
        public static ConfigEntry<bool> _bvSync; // Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.
        public static ConfigEntry<bool> _bUseDeltaTimeForMovement; // If disabled (default), it will use FixedUpdate with interpolation for character and NPC movement.
        
        // Input Config
        public static ConfigEntry<string> _sInputType;                // Automatic, Controller, KBM (Forces a certain type of button prompts, Controller will be used if Steam Deck is detected).
        public static ConfigEntry<string> _sControllerType;           // Automatic, Xbox, PS3, PS4, PS5, Switch (If SteamInput is enabled, "Automatic" will be used regardless of settings)
        public static ConfigEntry<bool>   _bDisableSteamInput;        // For those that don't want to use SteamInput, absolutely hate it being forced, and would rather use Unity's built-in input system.
        public static ConfigEntry<bool>   _bJapaneseControllerLayout; // True: Japanese Layout (Circle = Select, Cross = Back) in menus, False: Western Layout. Ignored when using a Nintendo controller.
        
        // Resolution Config
        public static          ConfigEntry<bool> _bForceCustomResolution;
        public static          ConfigEntry<int>  _iHorizontalResolution;
        public static          ConfigEntry<int>  _iVerticalResolution;
        public static readonly int               MaskMatrixUV = Shader.PropertyToID("mask_matrix_uv");
        public static readonly int               MaskTexture  = Shader.PropertyToID("mask_texture");
        
        // Misc Config
        public static ConfigEntry<bool> _bSkipSplashScreenSequence; // True: Skips Opening Splash Screen Logos and Movie, False: Default Behavior.
        
        private void InitConfig()
        {
            // Aspect Ratio Config
            _bOriginalUIAspectRatio                   = Config.Bind("Resolution", "Original UI Aspect Ratio",                   true,  "On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.");
            _bMajorAxisFOVScaling                     = Config.Bind("Resolution", "Major-Axis FOV Scaling",                     true,  "On: Vert- Behavior below 16:9, Off: Default Hor+ Behavior.");
            _bPresentCutscenesWithOriginalAspectRatio = Config.Bind("Resolution", "Present Cutscenes At Original Aspect Ratio", false, "On: Letterboxes/Pillarboxes cutscenes to display in 16:9, Off: Presents cutscenes without black bars (Default).");

            // Graphics Config
            _imsaaCount = Config.Bind("Graphics", "MSAA Quality", 0, new ConfigDescription("0: Off, 2: 2x MSAA (In-game default), 4: 4x MSAA, 8: 8x MSAA.", new AcceptableValueRange<int>(0, 8)));

            _sPostAAType = Config.Bind("Graphics", "Post-Process AA", "SMAA", "Off, FXAA, SMAA");
            if (!Enum.TryParse(_sPostAAType.Value, out _confPostAAType)) {
                _confPostAAType = EPostAAType.SMAA;
                SvSFix._log.LogError($"PostAA Value is invalid. Defaulting to SMAA.");
            }

            _resolutionScale = Config.Bind("Graphics", "Resolution Scale", 100, new ConfigDescription("Goes from 25% to 200%.", new AcceptableValueRange<int>(25, 200)));

            _sShadowQuality = Config.Bind("Graphics", "Shadow Quality", "Original",
                "Low (512), Medium (1024), High (2048), Original (4096), Ultra (8192), Extreme (16384)");
            if (!Enum.TryParse(_sShadowQuality.Value, out _confShadowQuality)) {
                _confShadowQuality = EShadowQuality.Original;
                SvSFix._log.LogError($"ShadowQuality Value is invalid. Defaulting to Original.");
            }

            _shadowCascades = Config.Bind("Graphics", "Shadow Cascades", 4, new ConfigDescription("0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)", new AcceptableValueRange<int>(0, 4)));
            
            _fLodBias = Config.Bind("Graphics", "Draw Distance (Lod Bias)", (float)1.00, new ConfigDescription("Default is 1.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons."));

            _iForcedLodQuality = Config.Bind("Graphics", "LOD Quality", 0, new ConfigDescription("0: No Forced LODs (Default), 1: Forces LOD # 1, 2: Forces LOD # 2, 3: Forces LOD # 3. Higher the value, the less mesh detail.", new AcceptableValueRange<int>(0, 3)));
            
            _iForcedTextureQuality = Config.Bind("Graphics", "Texture Quality", 0, new ConfigDescription("0: Full Resolution (Default), 1: Half-Res, 2: Quarter Res. Goes up to 1/14th res (14).", new AcceptableValueRange<int>(0, 14)));
            
            _bPostProcessing = Config.Bind("Graphics", "Post-Processing", true, "On: Enables Post-Processing (Default), Off: Disables Post-Processing (Which may be handy for certain configurations)");
            
            _anisotropicFiltering = Config.Bind("Graphics", "Anisotropic Filtering", 0, new ConfigDescription("0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF", new AcceptableValueRange<int>(0, 16)));

            _screenSpaceAmbientOcclusion = Config.Bind("Graphics", "Screen Space Ambient Occlusion (SSAO)", true, new ConfigDescription("On: Enables SSAO (Default), Off: Disables SSAO."));

            _grassDensity = Config.Bind("Graphics", "Grass Density", 100, new ConfigDescription("Goes from 0% to 100%.", new AcceptableValueRange<int>(0, 100)));
            
            // Framelimiter Config
            _iFrameInterval = Config.Bind("Framerate", "Framerate Cap Interval", 1, new ConfigDescription("0 disables the framerate limiter, 1 caps at your screen refresh rate, 2 caps at half refresh, 3 caps at 1/3rd refresh, 4 caps at quarter refresh.", new AcceptableValueRange<int>(0, 4)));

            _bvSync = Config.Bind("Framerate", "VSync", true, "Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.");
            
            _bUseDeltaTimeForMovement = Config.Bind("Framerate", "Delta Time Movement", false, "If disabled (default), it will use FixedUpdate with interpolation for character and NPC movement. Only enable this if interpolation doesn't give you smooth player movement.");
            
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
            
            _bDisableSteamInput = Config.Bind("Input", "Force Disable SteamInput", false, "Self Explanatory. Prevents SteamInput from ever running, forcefully, for those using DS4Windows/DualSenseX or wanting native controller support. Make sure to disable SteamInput in the controller section of the game's properties on Steam alongside this option.");

            _bJapaneseControllerLayout = Config.Bind("Input", "Japanese Controller Layout in Menus", false, "True: Enables Japanese Layout Menu Navigation (Circle = Select, Cross = Back), False: Uses Western Layout (Cross = Select, Circle = Back). Ignored when using a Nintendo controller.");
            
            // Resolution Config
            _bForceCustomResolution = Config.Bind("Resolution", "Force Custom Resolution", false, "Self Explanatory. A temporary toggle for custom resolutions until I can figure out how to go about removing the resolution count restrictions.");
            _iHorizontalResolution  = Config.Bind("Resolution", "Horizontal Resolution",   1280);
            _iVerticalResolution    = Config.Bind("Resolution", "Vertical Resolution",     720);
            
            // Misc Config
            _bSkipSplashScreenSequence = Config.Bind("Misc", "Skip Splash Screens and Opening Video", false, "True: Skips Splash Screen and Opening Videos for faster startup times, False: Default Functionality.");
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
            var renderPipeline = QualitySettings.renderPipeline;
            Debug.Log("Render pipeline type: " + renderPipeline.GetType().ToString());
            switch (renderPipeline is UniversalRenderPipelineAsset) {
                case true:
                    var asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                    asset.renderScale                     = (float)_resolutionScale.Value / 100;
                    asset.msaaSampleCount                 = _imsaaCount.Value;
                    asset.shadowCascadeCount              = _shadowCascades.Value;
                    
                    // TODO: Figure out a way of getting the MainLightShadow and AdditionalLightShadow properties to work.
                    var mainLightShadowResolutionProperty    = typeof(UniversalRenderPipelineAsset).GetProperty("mainLightShadowmapResolution");
                    var additionalLightShadowResolutionProperty = typeof(UniversalRenderPipelineAsset).GetProperty("additionalLightShadowResolution");
                    var opaqueDownsamplingProperty              = typeof(UniversalRenderPipelineAsset).GetProperty("m_OpaqueDownsampling");
                    if (mainLightShadowResolutionProperty != null &&
                        additionalLightShadowResolutionProperty != null &&
                        opaqueDownsamplingProperty != null) {
                        // Modify the properties using reflection
                        mainLightShadowResolutionProperty.SetValue(asset, (int)ShadowResVec().x);
                        additionalLightShadowResolutionProperty.SetValue(asset, (int)ShadowResVec().x);
                        opaqueDownsamplingProperty.SetValue(asset, Downsampling._4xBilinear);
                        Debug.Log("Shadow and Downsampling Properties Modified Successfully.");
                    }
                    else {
                        Debug.LogError("Failed to find one or more properties.");
                    }
                    QualitySettings.renderPipeline        = asset;
                    break;
                case false:
                    Debug.LogError("Render Pipeline is not of type UniversalRenderPipelineAsset.");
                    break;
            }
            
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
}