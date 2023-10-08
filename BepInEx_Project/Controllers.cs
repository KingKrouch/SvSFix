using UnityEngine;
using UnityEngine.UI;

namespace SvSFix.Controllers;

public class CustomMapUnitController : MapUnitCollisionCharacterControllerComponent
{
    private TransformInterpolator transformInterpolator;
    private void Start()
    {
        if (SvSFix._bUseDeltaTimeForMovement.Value) {
            transformInterpolator = gameObject.AddComponent<TransformInterpolator>();
        }
    }

    private void MovementUpdate()
    {
        if (this.collision_ == null || this.character_controller_ == null || this.collision_.IsCollisionInvalid()) {
            return;
        }
        if (!this.collision_.IsStandGroundHeight()) {
            this.collision_.ExtrusionAdd();
        }
        var origin_position = Vector3.zero;
        var now_position = Vector3.zero;
        var ground_height = 0f;
        if (this.collision_.UpdatePrevious(ref origin_position, ref now_position, out ground_height, GameTime.ScaledDeltaTime))
        {
            this.collision_.GravityGroundCapsule(ref now_position, in ground_height, GameTime.ScaledDeltaTime);
            if ((this.collision_.bit_mode_ & MapUnitCollision.BitMode.COLLISION_EXTRUSION) != 0) {
                this.character_controller_.Move(now_position - origin_position);
            }
            else {
                this.collision_.unit_base_.transform.position = now_position;
            }
            this.collision_.UpdateAfter();
            this.collision_.collider_list_collision_stay_.Clear();
        }
    }
    
    private void FixedUpdate()
    {
        if (SvSFix._bUseDeltaTimeForMovement.Value) { // This should allow us to adjust our desired movement mode.
            return;
        }
        MovementUpdate();
    }
    private void Update()
    {
        if (!SvSFix._bUseDeltaTimeForMovement.Value) { // This should allow us to adjust our desired movement mode.
            return;
        }
        MovementUpdate();
    }
}
public class CustomRigidBodyController : MapUnitCollisionRigidbodyComponent
{
    private TransformInterpolator transformInterpolator;
    private void Start()
    {
        if (SvSFix._bUseDeltaTimeForMovement.Value) {
            transformInterpolator = gameObject.AddComponent<TransformInterpolator>();
        }
    }

    private void MovementUpdate()
    {
        if (this.collision_ == null || this.rigidbody_component_ == null || this.collision_.IsCollisionInvalid()) {
            return;
        }
        this.collision_.ExtrusionAdd();
        var zero = Vector3.zero;
        var zero2 = Vector3.zero;
        var num = 0f;
        if (!this.collision_.UpdatePrevious(ref zero, ref zero2, out num, GameTime.ScaledDeltaTime)) {
            return;
        }
        this.collision_.GravityGroundRigidBody(ref zero2, num, GameTime.ScaledDeltaTime);
        if ((this.collision_.bit_mode_ & MapUnitCollision.BitMode.COLLISION_EXTRUSION) != MapUnitCollision.BitMode.SET_COLLISION_NONE) {
            this.rigidbody_component_.velocity = Vector3.zero;
            this.rigidbody_component_.angularVelocity = Vector3.zero;
            this.rigidbody_component_.MovePosition(zero2);
        }
        else {
            this.collision_.unit_base_.transform.position = zero2;
        }
        this.collision_.UpdateAfter();
    }
    
    private void FixedUpdate()
    {
        if (SvSFix._bUseDeltaTimeForMovement.Value)
        {
            return;
        }
        MovementUpdate();
    }
    private void Update()
    {
        if (!SvSFix._bUseDeltaTimeForMovement.Value)
        {
            return;
        }
        MovementUpdate();
        // TODO: Give the player the option between Delta Time and Interpolation Ticks for Character Movement.
    }
}