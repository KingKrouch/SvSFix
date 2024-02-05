// BepInEx and Harmony Stuff
using HarmonyLib;
// Unity and System Stuff
using System;
using UnityEngine;
// Mod Stuff
using SvSFix.Controllers;

namespace SvSFix;

public partial class SvSFix
{
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
            return _bUseDeltaTimeForMovement.Value switch {
                true => false // We are simply going to tell FixedUpdate to fuck off, and then reimplement everything in an Update method.
                ,
                false => true // This should in theory fix the stuck player movement when using interpolation.
            };
        }
            
        [HarmonyPatch(typeof(MapUnitCollisionCharacterControllerComponent), nameof(MapUnitCollisionCharacterControllerComponent.Setup), new Type[]{ typeof(GameObject), typeof(float), typeof(float), typeof(MapUnitBaseComponent) })]
        [HarmonyPostfix]
        public static void ReplaceWithCustomCharacterControllerComponent()
        {
            _log.LogInfo("MapUnitCollisionCharacterControllerComponent has been hooked!");
            // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
            var c = FindObjectsOfType<MapUnitCollisionCharacterControllerComponent>();
            _log.LogInfo("Found " + c[0].name + " possessing a CharacterController component.");
            var newMuc = c[0].gameObject.AddComponent(typeof(CustomMapUnitController)) as CustomMapUnitController;
            var ogMuc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionCharacterControllerComponent)) as MapUnitCollisionCharacterControllerComponent;
            if (ogMuc != null) {
                if (newMuc != null) {
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
            _log.LogInfo("MapUnitCollisionRigidbodyComponent has been hooked!");
            // This may or may not work properly. Normally I'd get the instance per HarmonyX's documentation, but that doesn't work here for some arbitrary reason.
            var c = FindObjectsOfType<MapUnitCollisionRigidbodyComponent>();
            _log.LogInfo("Found " + c[0].name + " possessing a RigidBodyController component.");
            var newRbc = c[0].gameObject.AddComponent( typeof(CustomRigidBodyController)) as CustomRigidBodyController;
            var ogRbc  = c[0].gameObject.GetComponent(typeof(MapUnitCollisionRigidbodyComponent)) as MapUnitCollisionRigidbodyComponent;
            if (ogRbc != null) {
                if (newRbc != null) {
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


