using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ControllerData", menuName = "CustomSO/Controller/Controller Data", order = 1)]
public class ControllerDataSO : ScriptableObject
{
    [Header("Rotation")]
    [Range(0f, 0.1f)]
    public float rotationThreshold = 0.01f;

    [Range(0f, 30f)]
    public float rotationSpeed = 10f;

    [Header("Movement")]
    [Range(2f, 20f)]
    public float movementSpeed = 10f;
    [Range(2f, 20f)]
    public float acceleration = 16f;

    [Header("Navmesh")]
    public float navmeshCheckRadius = 0.25f;

    [Header("Input")]
    [Range(1f, 20f)]
    public float inputSmoothSpeed = 8f;

    [Header("Collision")]
    public LayerMask controllerMask;
    public LayerMask controllerMaskHeadHit;
    public LayerMask controllerHandObstacleMask;
    [Range(0.5f, 3f)]
    public float controllerHeight = 2f;
    [Range(0.01f, 0.5f)]
    public float headHitCheckRadius = 0.35f;
    [Range(0.25f, 1f)]
    public float headHitSlowdown = 0.25f;
    [Range(0.25f, 1f)]
    public float handObstacleCheckDistance = 0.5f;
    [Range(0.01f, 0.5f)]
    public float handObstacleCheckRadius = 0.1f;
    [Range(0.25f, 2f)]
    public float handObstacleCheckHeight = 1.2f;

    [Header("Model")]
    [Range(1f, 20f)]
    public float modelRootYAlignSpeed = 5f;


    [Header("Walk")]
    [Range(0.0f, 0.75f)]
    public float walkSpeedMulti = 0.35f;
    [Range(0.0f, 1f)]
    public float walkRotationSpeedMulti = 0.5f;

}
