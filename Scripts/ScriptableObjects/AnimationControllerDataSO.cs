using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AnimationControllerDataSO", menuName = "CustomSO/Controller/Animation Data", order = 1)]
public class AnimationControllerDataSO : ScriptableObject
{
    [Header("Foot ik")]
    public LayerMask floorMask;
    [Range(-0.1f, 0.1f)]
    public float baseFeetOffset = -0.035f;
    public float feetYSpeed = 20f;

    [Header("Dynamic lean")]
    [Range(1f, 20f)]
    public float dynamicLeanSpeed = 6f;
    [Range(0.0f, 2f)]
    public float leanFactor = 0.75f;

    [Range(0f, 20f)]
    public float torsoRotationAngleSmoothSpeed = 8f;
    [Range(0f, 90f)]
    public float torsoRotationAngle = 25f;


    [Header("Crouch")]
    [Range(0.0f, 0.2f)]
    public float baseCrouch = 0.025f;
    [Range(0.0f, 0.5f)]
    public float accelerationCrouch = 0.35f;
    [Range(0.5f, 4f)]
    public float slopeFactor = 1.5f;
    [Range(0.0f, 0.2f)]
    public float slopeCrouchIntensity = 0.125f;
    [Range(0.0f, 0.75f)]
    public float headHitCrouchIntensity = 0.5f;
    [Range(1f, 10f)]
    public float crouchSpeed = 4f;
    [Range(0f, 0.75f)]
    public float maxCrouch = 0.4f;

    [Header("HandIK")]
    [Range(0f, 1f)]
    public float handMaxWeight = 0.8f;
    [Range(1f, 10f)]
    public float handWeightChangeSpeed = 3f;
}
