using UnityEngine;
namespace SvSFix.Controllers;
public class CustomMapUnitController : MapUnitCollisionCharacterControllerComponent
{
    private void FixedUpdate()
    {
        return;
    }
    private void Update()
    {
        if (this.collision_ == null || this.character_controller_ == null || this.collision_.IsCollisionInvalid()) {
            return;
        }
        if (!this.collision_.IsStandGroundHeight()) {
            this.collision_.ExtrusionAdd();
        }
        Vector3 origin_position = Vector3.zero;
        Vector3 now_position = Vector3.zero;
        float ground_height = 0f;
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
}
public class CustomRigidBodyController : MapUnitCollisionRigidbodyComponent
{
    private void FixedUpdate()
    {
        return;
    }
    private void Update()
    {
        if (this.collision_ == null || this.rigidbody_component_ == null || this.collision_.IsCollisionInvalid()) {
            return;
        }
        this.collision_.ExtrusionAdd();
        Vector3 zero = Vector3.zero;
        Vector3 zero2 = Vector3.zero;
        float num = 0f;
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
}