using Unity.Mathematics;
using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
#if UNITY_EDITOR
    public bool tweakingAnimations = false;
#endif

    public AnimationControllerDataSO data;
    public Controller controller;
    public Transform ikRoot;
    private Animator ani;

    // smoothing 
    private Vector3 smoothedSpeedVector = Vector3.zero;
    private float smoothingFactor = 10f;

    private void Start()
    {
        InitializeAnimationController();
    }

    void InitializeAnimationController()
    {
        ani = GetComponent<Animator>();

        footObjLeft = new GameObject();
        footObjRight = new GameObject();
        leftKnee = new GameObject();
        rightKnee = new GameObject();

        footObjLeft.transform.position = this.transform.position;
        footObjRight.transform.position = this.transform.position;
        leftKnee.transform.position = this.transform.position + Vector3.up * 0.5f + this.transform.forward * 0.5f;
        rightKnee.transform.position = this.transform.position + Vector3.up * 0.5f + this.transform.forward * 0.5f;
    }

    void Update()
    {
#if UNITY_EDITOR
        Time.timeScale = tweakingAnimations ? 0.2f : 1f;
#endif
        SetupAnimationFloats();
        DynamicCrouch();
        DynamicLean();
    }

    #region  FK

    void SetupAnimationFloats()
    {
        smoothedSpeedVector += (controller.MoveVectorScaled - smoothedSpeedVector) * Time.deltaTime * smoothingFactor;
        float x = Vector3.Dot(smoothedSpeedVector, controller.characterModelRoot.right);
        float y = Vector3.Dot(smoothedSpeedVector, controller.characterModelRoot.forward);
        ani.SetFloat("speedX", x);
        ani.SetFloat("speedY", y);
    }

    #endregion

    #region  IK

    void OnAnimatorIK(int layerIndex)
    {
        FootFix();
        // look down before crouching
        CrouchHeadMovement();
        // simple hand ik
        HandIK();
    }

    #region  Dynamic Crouch and Lean

    private float crouchCurrent = 0f;
    void DynamicCrouch()
    {
        // crouch when changing direction/accelerating/decelerating
        float accelerationCrouch = (controller.Target - controller.MoveVectorScaled).sqrMagnitude * data.accelerationCrouch;

        Vector3 currentPosition = controller.transform.position;
        Vector3 predictedPosition = controller.PredictedEndPosition;
        // if the predicted position is lower
        // than the current position
        // the character should mildly crouch
        // to "simulate" going downhill/down a stair
        float slopeCrouch = Mathf.Clamp01((currentPosition.y - predictedPosition.y) / data.slopeFactor) * data.slopeCrouchIntensity;

        // head hit 
        float headHitCrouch = controller.Crouch * data.headHitCrouchIntensity;

        // smooth
        crouchCurrent += (slopeCrouch + headHitCrouch + accelerationCrouch - crouchCurrent) * Time.deltaTime * data.crouchSpeed;

        // clamp
        crouchCurrent = Mathf.Clamp(crouchCurrent, 0f, data.maxCrouch);

        // move ik root downwards to crouch
        ikRoot.localPosition = new Vector3(0f, -crouchCurrent - data.baseCrouch, 0f);
    }

    private float headLookWeight = 0f;
    private float headLookWeightSpeed = 5f;
    void CrouchHeadMovement()
    {
        // make the character lower his head a bit before actually crouching
        headLookWeight += (Mathf.Clamp01(controller.Crouch / 0.2f) - headLookWeight) * Time.deltaTime * headLookWeightSpeed;
        ani.SetLookAtPosition(controller.transform.position + this.transform.forward * 0.25f);
        ani.SetLookAtWeight(headLookWeight);
    }

    private Vector3 dynamicLeanDelta = Vector3.zero;
    private Quaternion leanQuaternion = Quaternion.identity;
    private float currentTorsoRotAngle = 0f;

    void DynamicLean()
    {
        // speed delta
        Vector3 delta = controller.Target - controller.MoveVectorScaled;

        // smooth, making it quadratic makes it a bit smoother
        dynamicLeanDelta += (delta.normalized * delta.sqrMagnitude - dynamicLeanDelta) * data.dynamicLeanSpeed * Time.deltaTime;

        // lean     
        Vector3 dynamicLeanUp = Vector3.up + dynamicLeanDelta * data.leanFactor;
        Vector3 newForward = Vector3.Cross(controller.characterModelRoot.right, dynamicLeanUp);

#if UNITY_EDITOR
        // not needed for the actual code, just for the gizmo
        Vector3 newRight = Vector3.Cross(controller.characterModelRoot.forward, dynamicLeanUp);

        Vector3 center = this.transform.position + Vector3.up;
        Debug.DrawLine(center, center + newRight, Color.red, 0.05f);
        Debug.DrawLine(center, center + newForward, Color.blue, 0.05f);
#endif

        // rotate torso from predicted fw
        Vector3 currentFw = controller.characterModelRoot.forward;
        Vector3 targetFw = controller.FutureFacingDirection;

        // the scaled signed angle between the current and the predicted facing position
        float scaledAngle = Vector3.SignedAngle(currentFw, targetFw, Vector3.up) / 180f;

        // smooth the rotation angle 
        currentTorsoRotAngle += (data.torsoRotationAngle * scaledAngle - currentTorsoRotAngle) * Time.deltaTime * data.torsoRotationAngleSmoothSpeed;

        // rotate the fw vector
        // so that once we compute the look rotation
        // the entire model is rotated towards the predicted 
        // facing direction 
        newForward = Quaternion.AngleAxis(currentTorsoRotAngle, Vector3.up) * newForward;

        // set
        ikRoot.rotation = Quaternion.LookRotation(newForward, dynamicLeanUp);

        // foot rotation fix
        leanQuaternion = Quaternion.Inverse(ikRoot.localRotation);
        // this caused the bug!!
        // leanQuaternion = torsoRotFootFix;
    }

    #endregion

    #region feet

    private GameObject footObjLeft;
    private GameObject leftKnee;
    private GameObject footObjRight;
    private GameObject rightKnee;

    void FootFix()
    {
        // floor raycasts
        CheckFloor(footObjLeft.transform.position, out leftFootOffsetGround, out leftGroundQuat);
        CheckFloor(footObjRight.transform.position, out rightFootOffsetGround, out rightGroundQuat);

        Vector3 tempLeftFoot = ani.GetIKPosition(AvatarIKGoal.LeftFoot) + leftFootOffsetGround;
        Vector3 tempRightFoot = ani.GetIKPosition(AvatarIKGoal.RightFoot) + rightFootOffsetGround;

        MoveIKTarget(footObjLeft.transform.position, tempLeftFoot, out Vector3 leftFootNewPos);
        MoveIKTarget(footObjRight.transform.position, tempRightFoot, out Vector3 rightFootNewPos);

        footObjLeft.transform.position = leftFootNewPos;
        footObjRight.transform.position = rightFootNewPos;

        // smooth if necessary
        footObjLeft.transform.rotation = leanQuaternion * leftGroundQuat * ani.GetIKRotation(AvatarIKGoal.LeftFoot);
        footObjRight.transform.rotation = leanQuaternion * rightGroundQuat * ani.GetIKRotation(AvatarIKGoal.RightFoot);

        // same for the knees (no rotation required)
        Vector3 tempLeftKnee = ani.GetIKHintPosition(AvatarIKHint.LeftKnee) + leftFootOffsetGround / 2f;
        Vector3 tempRightKnee = ani.GetIKHintPosition(AvatarIKHint.RightKnee) + rightFootOffsetGround / 2f;

        MoveIKTarget(leftKnee.transform.position, tempLeftKnee, out Vector3 leftKneeNewPos);
        MoveIKTarget(rightKnee.transform.position, tempRightKnee, out Vector3 rightKneeNewPos);

        leftKnee.transform.position = leftKneeNewPos;
        rightKnee.transform.position = rightKneeNewPos;

        SetTargets();
        SetWeights();
    }

    void MoveIKTarget(Vector3 oldPos, Vector3 targetPos, out Vector3 newPos)
    {
        // the xz components need to change instantly so the feet
        // don't lag behind because we're doing smoothing
        // we only want to affect the y position of the foot
        float yIncrease = (targetPos.y - oldPos.y) * Time.deltaTime * data.feetYSpeed;
        newPos = new Vector3(targetPos.x, oldPos.y + yIncrease, targetPos.z);

        // another nerdy way of handling the issue
        // newPos = oldPos;
        // Vector3 planarOffset = targetPos - oldPos;
        // planarOffset.y = 0f;
        // newPos += planarOffset;
        // y smooth
        // newPos += (targetPos - newPos) * Time.deltaTime * data.feetYSpeed;
    }

    private Vector3 leftFootOffsetGround = Vector3.zero;
    private Vector3 rightFootOffsetGround = Vector3.zero;

    private Quaternion leftGroundQuat = Quaternion.identity;
    private Quaternion rightGroundQuat = Quaternion.identity;

    void CheckFloor(Vector3 footPos, out Vector3 footOffset, out Quaternion groundQuat)
    {
        Vector3 rayStart = footPos + data.baseFeetOffset * Vector3.down;
        rayStart.y = ikRoot.position.y;
        if (Physics.Raycast(rayStart + Vector3.up, Vector3.down, out RaycastHit hitLeft, 1.5f, data.floorMask, QueryTriggerInteraction.Ignore))
        {
            float yDelta = hitLeft.point.y - rayStart.y;
            footOffset = yDelta * Vector3.up;
            // the slope bi-tangent
            Vector3 bitangent = Vector3.Cross(Vector3.up, hitLeft.normal);
            float angle = Vector3.SignedAngle(Vector3.up, hitLeft.normal, bitangent);
            groundQuat = Quaternion.AngleAxis(angle, bitangent);
        }
        else
        {
            groundQuat = Quaternion.identity;
            footOffset = Vector3.zero;
        }
    }

    #endregion

    #region Foot Set

    void SetTargets()
    {
        // since we're rotating the entire character to simulate rotating the torso
        // we also move the knee and foot targets around the character center
        // We want those to remain in place so that we only rotate the upper body

        // 1. we make temp variables
        Vector3 leftFootPos = footObjLeft.transform.position + data.baseFeetOffset * Vector3.down;
        Vector3 rightFootPos = footObjRight.transform.position + data.baseFeetOffset * Vector3.down;
        Vector3 leftKneePos = leftKnee.transform.position + data.baseFeetOffset * Vector3.down / 2f;
        Vector3 rightKneePos = rightKnee.transform.position + data.baseFeetOffset * Vector3.down / 2f;

        // 2. we transform the position so they're relative 
        // to the model root
        leftFootPos = controller.characterModelRoot.InverseTransformPoint(leftFootPos);
        rightFootPos = controller.characterModelRoot.InverseTransformPoint(rightFootPos);
        leftKneePos = controller.characterModelRoot.InverseTransformPoint(leftKneePos);
        rightKneePos = controller.characterModelRoot.InverseTransformPoint(rightKneePos);

        // we need temp quats so we can reset the foot placement 
        // to before the actual "torso" rotation has happened
        Quaternion footRot = Quaternion.AngleAxis(-currentTorsoRotAngle, Vector3.up);
        // we allow some of the rotation to remain on the knee targets
        // it makes everything look a bit more realistic
        Quaternion kneeRot = Quaternion.AngleAxis(-currentTorsoRotAngle * 0.75f, Vector3.up);

        // 3. we rotate it around the y axis
        leftFootPos = footRot * leftFootPos;
        rightFootPos = footRot * rightFootPos;
        leftKneePos = kneeRot * leftKneePos;
        rightKneePos = kneeRot * rightKneePos;

        // 4. we translate everything back to world space
        leftFootPos = controller.characterModelRoot.TransformPoint(leftFootPos);
        rightFootPos = controller.characterModelRoot.TransformPoint(rightFootPos);
        leftKneePos = controller.characterModelRoot.TransformPoint(leftKneePos);
        rightKneePos = controller.characterModelRoot.TransformPoint(rightKneePos);

        ani.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPos);
        ani.SetIKRotation(AvatarIKGoal.LeftFoot, footObjLeft.transform.rotation);
        ani.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPos);
        ani.SetIKRotation(AvatarIKGoal.RightFoot, footObjRight.transform.rotation);
        ani.SetIKHintPosition(AvatarIKHint.LeftKnee, leftKneePos);
        ani.SetIKHintPosition(AvatarIKHint.RightKnee, rightKneePos);
    }

    void SetWeights()
    {
        ani.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        ani.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
        ani.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        ani.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
        ani.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 1f);
        ani.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 1f);
    }

    #endregion

    #region  HandIK

    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftElbowTarget;
    public Transform rightElbowTarget;


    private float leftHandWeight = 0f;
    private float rightHandWeight = 0f;

    public void HandIK()
    {
        leftHandWeight += (controller.leftHandObstacle ? 1f : -1f) * Time.deltaTime * data.handWeightChangeSpeed;
        rightHandWeight += (controller.rightHandObstacle ? 1f : -1f) * Time.deltaTime * data.handWeightChangeSpeed;

        leftHandWeight = Mathf.Clamp(leftHandWeight, 0f, data.handMaxWeight);
        rightHandWeight = Mathf.Clamp(rightHandWeight, 0f, data.handMaxWeight);

        ani.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
        ani.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
        ani.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        ani.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        ani.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowTarget.position);
        ani.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowTarget.position);

        ani.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
        ani.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
        ani.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
        ani.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);
        ani.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftHandWeight);
        ani.SetIKHintPositionWeight(AvatarIKHint.RightElbow, rightHandWeight);
    }


    #endregion

    #endregion
}
