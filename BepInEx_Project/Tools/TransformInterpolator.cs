using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a modified version of the interpolation controller as shown here: https://forum.unity.com/threads/motion-interpolation-solution-to-eliminate-fixedupdate-stutter.1325943/
/// How to use TransformInterpolator properly:
/// 1. Make sure the GameObject executes its mechanics (transform-manipulations) in FixedUpdate().
/// 2. Make sure VSYNC is enabled.
/// 3. Set the execution order for this script BEFORE all the other scripts that execute mechanics.
/// 4. Attach (and enable) this component to every GameObject that you want to interpolate.
/// (including the camera).
[DefaultExecutionOrder(-2000)]
public class TransformInterpolator : MonoBehaviour
{
    private struct TransformData
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
    }

    private TransformData transformData;
    private TransformData prevTransformData;
    private bool isTransformInterpolated;

    void OnEnable()
    {
        prevTransformData.position = transform.localPosition;
        prevTransformData.rotation = transform.localRotation;
        prevTransformData.scale = transform.localScale;
        isTransformInterpolated = false;
    }

    void FixedUpdate()
    {
        if (isTransformInterpolated)
        {
            transform.localPosition = transformData.position;
            transform.localScale = transformData.scale;
            isTransformInterpolated = false;
        }

        prevTransformData.position = transform.localPosition;
        prevTransformData.rotation = transform.localRotation;
        prevTransformData.scale = transform.localScale;
    }

    void Update()
    {
        if (!isTransformInterpolated)
        {
            transformData.position = transform.localPosition;
            transformData.rotation = transform.localRotation;
            transformData.scale = transform.localScale;
            isTransformInterpolated = true;
        }
        
        var interpolationAlpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        // Interpolate position and scale
        transform.localPosition = Vector3.Lerp(prevTransformData.position, transformData.position, interpolationAlpha);
        transform.localScale = Vector3.Lerp(prevTransformData.scale, transformData.scale, interpolationAlpha);

        // Interpolate rotation separately
        transform.localRotation = Quaternion.Slerp(prevTransformData.rotation, transformData.rotation, interpolationAlpha);
    }
}