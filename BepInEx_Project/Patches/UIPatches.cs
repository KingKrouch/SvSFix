// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
using UnityEngine.UI;
// Game and Plugin Stuff
using Game.ADV.Local;
using Game.Cinema;
using Game.UI;
using Game.UI.Battle;
using Game.UI.Dungeon;
using Game.UI.MainMenu.Common;
using Game.UI.MainMenu.Disc;
using Game.UI.MainMenu.Mintubu;
using Game.UI.MainMenu.Status;
using Game.UI.MainMenu.Status.Parts;
using Game.UI.PhotoMode;
using Game.UI.SaveList;
using Game.UI.World;
using Game.UI.WorldMap.Local;
using IF.ED;
using IF.GameMain.Splash;
using IF.GameMain.Splash.Config;
using IF.URP.RendererFeature.GaussianBlur;
using InHouseLibrary.ADV.Local;
// Mod Stuff
using SvSFix.Tools;

namespace SvSFix;

public partial class SvSFix
{ 
    [HarmonyPatch]
    public class UIPatches
    {
        // TODO:
        // 2. Adjust the RectTransform.OffsetMin of the In-Game Key Prompts to take up the horizontal aspect ratio equivalent of 2160p (For some reason, with 32:9, you have to have that set to 8000 instead of 7680, have to investigate)
        // 3. Adjust the RectTransform.AnchoredPosition of the Pause Menu prompts to a 16:9 position in the center of the screen. May have to adjust the vertical position and scale if the aspect ratio is less than 16:9.
        // 4. Adjust the scale of fullscreen UI elements to a 16:9 portion on the screen if the aspect ratio is less than 16:9.
        // 5. GameUiMainMenuStatus and GameUiMainMenuDisc's Canvas>Root components need an AspectRatioScaler with a 16:9 float value and an aspectMode of "FitInParent" to display properly.
        // 6. The BlackBarComponent actor should be placed on GameUiDungeonFullMap/Canvas, AdvIcon/Canvas, GameUiMainMenuBack/Cover, GameUiWorldMap.
        // 7. For some reason, when ADVs start, it does some really funny stuff with the screen aspect ratio. Try and investigate this, and have it use a 16:9 scale regardless of the resolution at the start.
        // 8. Adjust the AdvInterface elements to fit to a 16:9 aspect ratio portion on-screen. For some reason, it grows bigger the narrower the aspect ratio.
        // 9. Adjust the positions of UI elements in Dungeons and Battles based on if the user wants a spanned or centered UI.
        private const  float             OriginalAspectRatio            = 1.7777778f;
        private static float             _newSizeX                      = 3840f;
        private static float             _newSizeY                      = 2160f;
        private static bool              _in16X9Menu                    = false;
        private static bool              _inCutscene                    = false;
        private static AspectRatioFitter _gameUiMainMenuStatusScaler; 
        private static AspectRatioFitter _gameUiMainMenuDiscScaler;
        private static AspectRatioFitter _gameUiMainMenuMintubuScaler;
        private static AspectRatioFitter _gameUiFullScreenMiniMapScaler;
        private static bool              _createdBlackBarActorInWorldMap;
        private static GameObject        _blackBarActorWorldMap;
        private static Component         _blackBarActorWorldMapComponent;
        
        [HarmonyPatch(typeof(GameUiMainMenuMintubu), nameof(GameUiMainMenuMintubu.Open), new Type[] { typeof(bool) })]
        [HarmonyPostfix]
        public static void GameUiMainMenuMintubuOpen()
        {
            _log.LogInfo("Opened Chirper Menu.");
            // So we are essentially gonana look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
            var menuChirper = FindObjectsOfType<GameUiMainMenuMintubu>();
            //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
            if (_gameUiMainMenuMintubuScaler == null)
            {
                _log.LogInfo("Found " + menuChirper[0].name + " possessing a GameUiMainMenuMintubu component.");
                var transform = menuChirper[0].transform.Find("Canvas/Root");
                _gameUiMainMenuMintubuScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                if (_gameUiMainMenuMintubuScaler != null) {
                    _gameUiMainMenuMintubuScaler.aspectRatio = OriginalAspectRatio;
                    _gameUiMainMenuMintubuScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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
            // So we are essentially gonna look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
            var menuPhotoMode = FindObjectsOfType<GameUiPhotoMode>();
            //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
            _log.LogInfo("Found " + menuPhotoMode[0].name + " possessing a GameUiPhotoMode component.");
            var transform = menuPhotoMode[0].transform.Find("Canvas/Root");
            RectTransform photoModeTransform = transform.GetComponent<RectTransform>();
            float currentAspectRatio = SystemCamera3D.GetCamera().aspect;
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
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var gameUiMainMenuCharaSelect = FindObjectsOfType<GameUiMainMenuCharaSelect>();
            var transformCharaSelect = gameUiMainMenuCharaSelect[0].transform.Find("Canvas/Root");
            var rectTransformCharaSelect = transformCharaSelect.GetComponent<RectTransform>();
            
            switch (currentAspectRatio) {
                case > OriginalAspectRatio:
                    var anchor = (float)Math.Round(1 - (((1 - (OriginalAspectRatio / currentAspectRatio)) * 0.5) / 0.5));
                    rectTransformCharaSelect.anchorMin = new Vector2(anchor, 1);
                    rectTransformCharaSelect.anchorMax = new Vector2(anchor,1);
                    break;
                case < OriginalAspectRatio:
                    rectTransformCharaSelect.anchorMin = new Vector2(1,1);
                    rectTransformCharaSelect.anchorMax = new Vector2(1,1);
                    break;
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
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            if (keyAssign != null) {
                var transformKeyAssign = keyAssign.transform.Find("Canvas/Root/Frame0/Trunk");
                if (transformKeyAssign != null) {
                    var rectTransformKeyAssign = transformKeyAssign.GetComponent<RectTransform>(); // TODO: Why are you NULLing me? I'm right!
                    if (rectTransformKeyAssign != null)
                    {
                        switch (currentAspectRatio) {
                            case > OriginalAspectRatio:
                                var pivotEst = (1 - (OriginalAspectRatio / currentAspectRatio)) - ((1 - (OriginalAspectRatio / currentAspectRatio) / 10)); // This is my assumption.
                                rectTransformKeyAssign.pivot = new Vector2(pivotEst, 0.5f);
                                //rectTransformKeyAssign.pivot = new Vector2(((1 - (OriginalAspectRatio / currentAspectRatio)) + 0.5f), 0.5f); // Old Formula, doesn't work well, keeping here just in case.
                                break;
                            case < OriginalAspectRatio:
                                rectTransformKeyAssign.pivot = new Vector2(0.5f, 0.5f);
                                break;
                        }
                    }
                    else{ _log.LogError("rectTransformKeyAssign returned null."); }
                    if (_gameUiFullScreenMiniMapScaler == null) {
                        _log.LogInfo("GameUiFullscreenMinimapScaler returned null, creating component.");
                        _gameUiFullScreenMiniMapScaler = transformKeyAssign.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
                        if (_gameUiFullScreenMiniMapScaler != null) {
                            _gameUiFullScreenMiniMapScaler.aspectRatio = OriginalAspectRatio;
                            _gameUiFullScreenMiniMapScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                        }
                    }
                }
                else{ _log.LogError("transformKeyAssign returned null."); }
            }
            else{ _log.LogError("keyAssign returned null."); }
        }

        public static void FixFrame2(GameUiKeyAssign keyAssign)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transformFrame2 = keyAssign.transform.Find("Canvas/Root/Frame2");
            var rectTransformFrame2 = transformFrame2.GetComponent<RectTransform>();

            var offsetMin = rectTransformFrame2.offsetMin;
            offsetMin = currentAspectRatio switch {
                > OriginalAspectRatio => new Vector2((3840f / (OriginalAspectRatio / currentAspectRatio)) * -1,
                    offsetMin.y),
                < OriginalAspectRatio => new Vector2(3840f * -1, offsetMin.y),
                _ => offsetMin
            };
            rectTransformFrame2.offsetMin = offsetMin;
        }

        public static void FixFrame3(GameUiKeyAssign keyAssign)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transformFrame3 = keyAssign.transform.Find("Canvas/Root/Frame3");
            var rectTransformFrame3 = transformFrame3.GetComponent<RectTransform>();
                
            var offset16X9 = new Vector2(3840f * -1, rectTransformFrame3.offsetMin.y);
            var offsetSpanned = new Vector2((3840f / (OriginalAspectRatio / currentAspectRatio)) * -1, rectTransformFrame3.offsetMin.y);

            if (!_in16X9Menu)
            {
                rectTransformFrame3.offsetMin = currentAspectRatio switch {
                    > OriginalAspectRatio => offsetSpanned,
                    < OriginalAspectRatio => offset16X9,
                    _ => rectTransformFrame3.offsetMin
                };
            }
            else { rectTransformFrame3.offsetMin = offset16X9; }
        }

        [HarmonyPatch(typeof(GameUiWorldDungeonEntrance), nameof(GameUiWorldDungeonEntrance.Open), new Type[] { typeof(bool) })]
        [HarmonyPostfix]
        public static void FixBlackFadeBackgroundDuringDungeonEntrance(GameUiWorldDungeonEntrance __instance)
        {
            var transform = __instance.transform.Find("Canvas/Root/Back");
            var scale = MathF.Round(SystemCamera3D.GetCamera().aspect / OriginalAspectRatio);
            transform.localScale = new Vector3(scale, scale, transform.localScale.z); // It's a bit lazy to scale both X and Y, but it should work fine.
        }
            
        [HarmonyPatch(typeof(GameUiSaveList), nameof(GameUiSaveList.Open))]
        [HarmonyPostfix]
        public static void FixBlackFadeBackgroundDuringSaves(GameUiWorldDungeonEntrance __instance)
        {
            var transform = __instance.transform.Find("Root/Filter");
            var scale = MathF.Round(SystemCamera3D.GetCamera().aspect / OriginalAspectRatio);
            transform.localScale = new Vector3(scale, scale, transform.localScale.z); // It's a bit lazy to scale both X and Y, but it should work fine.
        }

        [HarmonyPatch(typeof(GameUiDungeonAreaMove), "Open", new Type[] { typeof(bool) })]
        [HarmonyPostfix]
        public static void FixBlackFadeBackgroundDuringDungeonAreaMove(GameUiDungeonAreaMove __instance) // TODO: Figure out why the safe area has a black background sometimes.
        {
            var transform = __instance.transform.Find("Canvas/Root/Back");
            var scale = MathF.Round(SystemCamera3D.GetCamera().aspect / OriginalAspectRatio);
            transform.localScale = new Vector3(scale, scale, transform.localScale.z); // It's a bit lazy to scale both X and Y, but it should work fine.
        }

        [HarmonyPatch(typeof(GameUiDungeonMemberStatus), nameof(GameUiDungeonMemberStatus.Open))]
        [HarmonyPostfix]
        public static void CharacterDungeonUiAnchoring(GameUiDungeonMemberStatus __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    // TODO: Fix formula to work properly with 32:9 (and wider) aspect ratios. Clearly I'm doing something wrong.
                    var anchorMax = (((1.0f - (OriginalAspectRatio / currentAspectRatio)) * 0.5f) / 0.5f);
                    // var anchorMax = (((1.0f - (OriginalAspectRatio / currentAspectRatio)) * 0.5f) / 0.5f); // original formula.
                    rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2(anchorMax, 0.5f) : new Vector2(0.5f, 0.5f);
                }
                else{ _log.LogError("GameUiDungeonMemberStatusRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiDungeonMemberStatusTransform returned null."); }
        }

        [HarmonyPatch(typeof(AdvUiIcon), MethodType.Constructor)] // TODO: Need to find a better hook for this as we do want to pillarbox the ADV/VN segments too.
        [HarmonyPostfix]
        public static void AdvCreateLetterboxes(AdvUiIcon __instance)
        {
            var blackBarActor = CreateBlackBars(__instance.gameObject); // TODO: Fix component to display properly, for now, we are gonna have to go without.
        }

        [HarmonyPatch(typeof(GameUiWorldMap), "Awake")]
        [HarmonyPostfix]
        public static void WorldMapCreateLetterboxes(GameUiWorldMap __instance)
        {
            // For now, zooming into the minimap is going to have to do, at least until I can fix the pillarbox actor.
            var objectinstance = __instance.gameObject;
            //var objectCanvas = objectinstance.transform.Find("Canvas");
            //var objectScaler = objectCanvas.GetComponent<CanvasScaler>();
            //objectScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            if (_createdBlackBarActorInWorldMap) return; // TODO: Fix component to display properly, for now, we are gonna have to Zoom in on the minimap.
            _blackBarActorWorldMap = CreateBlackBars(__instance.gameObject);
            if (_blackBarActorWorldMap == null) return;
            _blackBarActorWorldMapComponent = _blackBarActorWorldMap.GetComponent<BlackBarController>();
            if (_blackBarActorWorldMapComponent != null) {
                _createdBlackBarActorInWorldMap = true;
            }
        }
            
        public static GameObject CreateBlackBars(GameObject parent)
        {
            // Creates our BlackBarController prefab by hooking into Unity's AssetBundles system.
            if (blackBarControllerBundle != null) {
                var names = blackBarControllerBundle.GetAllAssetNames();
                Debug.Log(blackBarControllerBundle.GetAllAssetNames());
                var prefab = blackBarControllerBundle.LoadAsset<GameObject>(names[0]);
                var blackBarController = Instantiate(prefab, parent.transform, true);
                var controllerComponent = blackBarController.GetComponent<BlackBarController>();
                //DontDestroyOnLoad(blackBarController);
                if (controllerComponent == null) {
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

                    var canvases = blackBarController.GetComponentsInChildren<Canvas>();
                    foreach (var c in canvases) {
                        c.sortingOrder = 7936;
                    } // This should in theory fix the sorting issue with the auto/skip/stop elements.
                    _log.LogInfo("Created BlackBarController Actor.");
                }
                else { _log.LogError("BlackBarController Component was already created. Couldn't Spawn BlackBarController Actor."); return null; }
                return blackBarController;
            }
            else {
                _log.LogError("Couldn't Spawn BlackBarController Actor.");
                return null;
            }
        }

        [HarmonyPatch(typeof(AdvUiIcon), "Update")]
        [HarmonyPostfix]
        public static void AdvIconCustomAdjust(AdvUiIcon __instance) // We are simply gonna hook the original function that AdvIconCustom is based on.
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    // The default is 1.0, but 0.75 is what it needs to be at to be in the proper space in 32:9. 0.5 is half-way
                    float AnchorScale() {
                        var aspectRatioOffset = OriginalAspectRatio / SystemCamera3D.GetCamera().aspect;
                        var newAnchorPoint = (1.0f + aspectRatioOffset) / 2;
                        return restrictAdvUiTo16x9 ? newAnchorPoint : 1.00f;
                    }
                    rectTransform.anchorMax = new Vector2(AnchorScale(), 1);
                }
                else{ _log.LogError("AdvIconCustomRectTransform returned null."); }
            }
            else{ _log.LogError("AdvIconCustomTransform returned null."); }
        }

        [HarmonyPatch(typeof(GameCinemaAccessor), nameof(GameCinemaAccessor.IsPlaying))]
        [HarmonyPostfix]
        public static void GameCinemaPlaying() // TODO: Find working hook for checking whether a cutscene is playing, we will need this for adjusting the AdvIconCustom position.
        {
                
        }

        //[HarmonyPatch(typeof(GameAdvCameraManage), nameof(GameAdvCameraManage.Refresh))]
        //[HarmonyPostfix]
        public static void GameAdvCameraAdjust(GameAdvCameraManage __instance) // TODO: Fix broken hook
        {
            var cameraFront = __instance.GetSubCameraSideFront();
            var cameraBack  = __instance.GetSubCameraSideBack();
                
            Vector2 ApproximateOriginalScale() {
                    // Set up our current aspect ratio and aspect ratio offset.
                    var currentAspectRatio              = SystemCamera3D.GetCamera().aspect;
                    var aspectRatioOffset               = OriginalAspectRatio / currentAspectRatio;
                    // Set up the Horizontal offsets.
                    var originalAspectRatioApproximateX = SystemCamera3D.GetCamera().pixelWidth * aspectRatioOffset;
                    // Set up the Vertical offsets.
                    var originalAspectRatioApproximateY = SystemCamera3D.GetCamera().pixelHeight * aspectRatioOffset;
                    // Finally return it as a clean Vector2 that we can quickly reference later without reusing a ton of code.
                    return new Vector2(originalAspectRatioApproximateX, originalAspectRatioApproximateY);
            }

            if (cameraFront != null) {
                var cameraFrontCamComponent = cameraFront.GetComponent<Camera>();
                
                if (cameraFrontCamComponent != null) {
                    cameraFrontCamComponent.aspect = OriginalAspectRatio;
                }
                else { _log.LogError("CameraFront's Camera Component returned null."); }
                    
                var cameraFrontRawImageTransform = cameraFront.transform.Find("AdvScreen/RawImage");
                    
                if (cameraFrontRawImageTransform != null)
                {
                    var cameraFrontRawImageRectTransform = cameraFrontRawImageTransform.GetComponent<RectTransform>();
                    if (cameraFrontRawImageRectTransform != null) {
                        cameraFrontRawImageRectTransform.sizeDelta = ApproximateOriginalScale();
                    }
                    else { _log.LogError("CameraFront's RectTransform returned null."); }
                }
                else { _log.LogError("CameraFront's RawImage transform returned null."); }
            }
            else { _log.LogError("CameraFront returned null."); }
                
            if (cameraBack != null)
            {
                var cameraBackCamComponent = cameraBack.GetComponent<Camera>();
                
                if (cameraBackCamComponent != null) {
                    cameraBackCamComponent.aspect = OriginalAspectRatio;
                }
                else { _log.LogError("CameraBack's Camera Component returned null."); }
                    
                var cameraBackRawImageTransform = cameraFront.transform.Find("AdvScreen/RawImage");
                    
                if (cameraBackRawImageTransform != null)
                {
                    var cameraBackRawImageRectTransform = cameraBackRawImageTransform.GetComponent<RectTransform>();
                    if (cameraBackRawImageRectTransform != null) {
                        cameraBackRawImageRectTransform.sizeDelta = ApproximateOriginalScale();
                    }
                    else { _log.LogError("CameraBack's RectTransform returned null."); }
                }
                else { _log.LogError("CameraBack's RawImage transform returned null."); }
            }
            else { _log.LogError("CameraBack returned null."); }
        }

        [HarmonyPatch(typeof(GameUiDungeonMiniMap), nameof(GameUiDungeonMiniMap.Open))]
        [HarmonyPostfix]
        public static void GameUiDungeonMinimapAnchoring(GameUiDungeonMiniMap __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.anchorMax = _bOriginalUIAspectRatio.Value ? new Vector2((0.5f + (1 - (OriginalAspectRatio / currentAspectRatio))) ,rectTransform.anchorMax.y) : new Vector2(1, rectTransform.anchorMax.y);
                }
                else{ _log.LogError("GameUiDungeonMiniMapRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiDungeonMiniMapTransform returned null."); }
        }

        [HarmonyPatch(typeof(GameUiBattleComboSkill), nameof(GameUiBattleComboSkill.Open))]
        [HarmonyPostfix]
        public static void GameUiBattleComboSkillAnchoring(GameUiBattleComboSkill __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((0.5f + (1 - (OriginalAspectRatio / currentAspectRatio))) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                }
                else{ _log.LogError("GameUiBattleComboSkillRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiBattleComboSkillTransform returned null."); }
        }
            
        [HarmonyPatch(typeof(GameUiBattleComboNum), nameof(GameUiBattleComboNum.Open))]
        [HarmonyPostfix]
        public static void GameUiBattleComboNumAnchoring(GameUiBattleComboNum __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ?new Vector2((0.5f + (1 - (OriginalAspectRatio / currentAspectRatio))) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
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
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((0.5f + (1 - (OriginalAspectRatio / currentAspectRatio))) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                }
                else{ _log.LogError("GameUiBattleChainRecommendRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiBattleChainRecommendTransform returned null."); }
        }
            
        // Set screen match mode when object has canvas scaler enabled
        [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
        [HarmonyPostfix]
        public static void SetScreenMatchMode(CanvasScaler __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            if (currentAspectRatio > OriginalAspectRatio || currentAspectRatio < OriginalAspectRatio) {
                __instance.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }
            
        [HarmonyPatch(typeof(GameUiBattleHitDamage), "Awake")]
        [HarmonyPostfix]
        public static void GameUiBattleHitDamageAnchoring(GameUiBattleHitDamage __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root/HitDamage");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    var newAnchoredPosX = (3840f / (OriginalAspectRatio / currentAspectRatio)) - (3840f - 3234);
                    var anchoredPosition = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = !_bOriginalUIAspectRatio.Value ? new Vector2( newAnchoredPosX ,anchoredPosition.y) : new Vector2(3234f, anchoredPosition.y);
                    var anchoredPosition3D = rectTransform.anchoredPosition3D;
                    rectTransform.anchoredPosition3D = !_bOriginalUIAspectRatio.Value ? new Vector3( newAnchoredPosX ,anchoredPosition3D.y, anchoredPosition3D.z) : new Vector3(3234f, anchoredPosition3D.y, anchoredPosition3D.z);
                }
                else{ _log.LogError("GameUiBattleChainRecommendRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiBattleChainRecommendTransform returned null."); }
        }
            
        [HarmonyPatch(typeof(GameUiBattleTacticalSkill), nameof(GameUiBattleTacticalSkill.Open))]
        [HarmonyPostfix]
        public static void GameUiBattleTacticalSkillAnchoring(GameUiBattleTacticalSkill __instance)
        {
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            var transform = __instance.transform.Find("Canvas/Root");
            if (transform != null) {
                var rectTransform = transform.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.anchorMax = !_bOriginalUIAspectRatio.Value ? new Vector2((0.5f + (1 - (OriginalAspectRatio / currentAspectRatio))) ,rectTransform.anchorMax.y) : new Vector2(0.5f, rectTransform.anchorMax.y);
                }
                else{ _log.LogError("GameUiBattleTacticalSkillRectTransform returned null."); }
            }
            else{ _log.LogError("GameUiBattleTacticalSkillTransform returned null."); }
        }
            
        //(0.5 + (1 - (OriginalAspectRatio / currentAspectRatio)))

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
            // So we are essentially gonna look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
            var menuStatus = FindObjectsOfType<GameUiMainMenuStatus>();
            //menuTop[0].transform.parent == FindObjectOfType(GameUiMainMenuStatus);
            if (_gameUiMainMenuStatusScaler != null) return;
            _log.LogInfo("Found " + menuStatus[0].name + " possessing a GameUiMainMenuStatus component.");
            var transform = menuStatus[0].transform.Find("Canvas/Root");
            _gameUiMainMenuStatusScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
            if (_gameUiMainMenuStatusScaler == null) return;
            _gameUiMainMenuStatusScaler.aspectRatio = OriginalAspectRatio;
            _gameUiMainMenuStatusScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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
            // So we are essentially gonna look for an object with the MainMenuTop component, and then check if it belongs to a parent of GameUiMainMenuStatus before creating the aspect ratio fitter component.
            var menuDisc = FindObjectsOfType<GameUiMainMenuDisc>();
            if (_gameUiMainMenuDiscScaler != null) return;
            _log.LogInfo("Found " + menuDisc[0].name + " possessing a GameUiMainMenuDisc component.");
            var transform = menuDisc[0].transform.Find("Canvas/Root");
            _gameUiMainMenuDiscScaler = transform.gameObject.AddComponent(typeof(AspectRatioFitter)) as AspectRatioFitter;
            if (_gameUiMainMenuDiscScaler == null) return;
            _gameUiMainMenuDiscScaler.aspectRatio = OriginalAspectRatio;
            _gameUiMainMenuDiscScaler.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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
            var matrix = Matrix4x4.identity;
            MGS_GM.Matrix_MulTranslate_Parent(ref matrix, 0.5f, 0.5f, 0f);
            MGS_GM.Matrix_MulRotZ_Parent(ref matrix, MGS_GM.DegToRad(mask_rotation_degree_z));
            // Add our new aspect ratio calculation logic.
            var currentAspectRatio = SystemCamera3D.GetCamera().aspect;
            switch (currentAspectRatio) {
                case > OriginalAspectRatio:
                    _newSizeX = (float)Math.Round(3840f / (OriginalAspectRatio / currentAspectRatio));
                    _newSizeY = 2160f;
                    break;
                case < OriginalAspectRatio:
                    // TODO: Figure out why narrower aspect ratios results in the left and right being cut off, alongside the top and bottom appearing more egg-like.
                    _newSizeX = 3840f;
                    _newSizeY = (float)Math.Round(2160f / (OriginalAspectRatio / currentAspectRatio));
                    break;
            }
            MGS_GM.Matrix_MulScale_Parent(ref matrix, _newSizeX / mask_texture.width, _newSizeY / mask_texture.height, 1f);
            if (mask_scale != 0f) {
                MGS_GM.Matrix_MulScale_Parent(ref matrix, 1f / (mask_scale * 2f), 1f / (mask_scale * 2f), 1f);
            }
            MGS_GM.Matrix_MulTranslate_Parent(ref matrix, (mask_position.x / Screen.width - 0.5f) * 2f * -1f, (mask_position.y / Screen.height - 0.5f) * 2f * -1f, 0f);
            out_material.SetMatrix(MaskMatrixUV, matrix);
            return false;
        }

        //[HarmonyPatch(typeof(SplashSequenceManager), "HandleSkip")]
        //[HarmonyPostfix]
        //public static void SkipIntro(SplashSequenceManager __instance)
        //{
        //__instance.media_player_.SkipCurrent(); // Need to write the proper functionality of this, but I just need to find a way of calling these functions when I want to skip opening logos/videos
        //}
    }
}