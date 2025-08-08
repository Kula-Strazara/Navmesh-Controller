using UnityEngine;
using UnityEngine.AI;
using System;
using UnityEditor;
using Unity.VisualScripting;
using UnityEditor.U2D.Sprites;
using Unity.Burst.Intrinsics;

public class Controller : MonoBehaviour
{
    public Vector3 MoveVectorScaled => moveVector / controllerData.movementSpeed;
    public Vector3 PredictedEndPosition => predPositionEnd;

#if UNITY_EDITOR
    public bool showPredictionGizmosHands = false;
    public bool showPredictionGizmosInput = false;
    public bool showInputGizmos = false;
    public bool showMovementGizmos = false;
    public bool showPredictionGizmos = true;
#endif

    public ControllerDataSO controllerData;
    public InputSO input;
    protected Inputs controllerInputs;

    // public get
    public Inputs Inputs => controllerInputs;

    protected Action moveAction = delegate { };

    // we change this from an "Action"
    // to Func<T,T,T,T, TResult>
    // so we can have a return value
    // and so we can send data into the function at the same time
    // which we can use in our prediction loop
    // to figure out where the character will be
    // facing in the near future
    protected Func<Vector3, Vector3, Vector3, float, Vector3> rotationFunction = delegate { return Vector3.forward; };

    protected Action inputAction = delegate { };

    public Transform characterModelRoot;

    void Awake()
    {
        InitializeController();
    }

    void InitializeController()
    {
        moveAction = MoveController;
        inputAction = HandleInput;
        rotationFunction = RotationDefault;

        // initialize the inputs
        controllerInputs = new();
        controllerInputs.inputAxis = InputAxis;

        // cancel
        controllerInputs.cancel = Cancel;

        // context1- walk
        controllerInputs.context1 = StartWalk;
        controllerInputs.context1Released = EndWalk;


        // push into the input scriptable object
        // input.PushInput(controllerInputs);

        // detach the model root from the actual controller
        // to make animation smoother!
        characterModelRoot.transform.parent = null;
    }

    void Update()
    {
        inputAction();
        moveAction();

        // we're using the return type now
        Vector3 rotatedFw = rotationFunction(this.transform.position, characterModelRoot.transform.forward, moveVector, Time.deltaTime);
        // Vector3 rotatedFw = rotationFunction(this.transform.position, characterModelRoot.transform.forward, inputSmoothed, Time.deltaTime);
        // to make sure the model stays upright!
        characterModelRoot.transform.rotation = Quaternion.LookRotation(rotatedFw, Vector3.up);

        // smooth Y movement
        ControllerModelRootY();
    }


    void ControllerModelRootY()
    {
        // move the model root in the XZ plane so only
        // the y position component is actually affected
        characterModelRoot.transform.position = new Vector3(this.transform.position.x, characterModelRoot.transform.position.y, this.transform.position.z);

        // to fix going downwards
        float multi = 1f;

        // if the controller is below the model
        // make the transition a bit faster
        // to avoid ik issues
        if (this.transform.position.y < characterModelRoot.transform.position.y)
            multi = 2f;

        // smooth move y only
        characterModelRoot.transform.position += (this.transform.position - characterModelRoot.transform.position) * Time.deltaTime * controllerData.modelRootYAlignSpeed * multi;
    }

    #region Prediction

    [HideInInspector]
    public bool leftHandObstacle = false;
    [HideInInspector]
    public bool rightHandObstacle = false;

    // the input vector after the prediction has been computed
    protected Vector3 inputVectorTargetPredicted = Vector3.zero;

#if UNITY_EDITOR
    protected Vector3 predPositionStart = Vector3.zero;
    // protected Vector3 predPositionEnd = Vector3.zero;
    protected Vector3 predHeadHitPosition = Vector3.zero;
#endif
    // it is needed for IK
    protected Vector3 predPositionEnd = Vector3.zero;

    // so we can use this inside the animation/ik script
    protected Vector3 futureFacingDirection = Vector3.forward;
    public Vector3 FutureFacingDirection => futureFacingDirection;

#if UNITY_EDITOR
    private OldData[] oldData = new OldData[30];
    private int currentOldDir = 0;
#endif

    private float crouch = 0f;
    private float crouchSpeedMulti = 1f;
    public float Crouch => crouch;
    [Range(10, 50)]
    public int predictFrames = 25;
    protected void PredictCollisions()
    {
#if UNITY_EDITOR
        oldData[currentOldDir].dir = characterModelRoot.transform.forward;
        oldData[currentOldDir].pos = characterModelRoot.transform.position;
        currentOldDir++;
        currentOldDir %= oldData.Length;

        predPositionStart = this.transform.position;
        predPositionEnd = predPositionStart;
        predHeadHitPosition = predPositionStart + Vector3.up * controllerData.controllerHeight;
#endif
        // current input
        Vector3 temporaryInputSmoothed = inputSmoothed;
        inputVectorTargetPredicted = targetInput;

        // current pos, speed and facing direction 
        Vector3 start = this.transform.position;
        Vector3 velocity = moveVector;
        Vector3 characterFw = characterModelRoot.transform.forward;
        Vector3 avoidanceNormal = Vector3.up;

        // temp variables
        float timeDelta;
        int frames = 0;

        // this is why we needed to change the COS function
        bool hit = false;
        // how many frames was the controller 
        // not in "collision" with an obstacle
        int leftWallCounter = 0;

        //ik relevant data
        float crouchDistance = 0f;

        // reset hand bools
        leftHandObstacle = false;
        rightHandObstacle = false;

        // the prediction loop
        // we check for predictFrames
        // into the future
        while (frames++ < predictFrames)
        {
            // each frame we increase the time delta a little bit
            // increasing error but making the simulation longer
            // allowing for a better prediction
            timeDelta = 0.02f * Mathf.Lerp(1f, 1.5f, (float)frames / (float)predictFrames);

            // first we do a COS for the current target input
            // we "act" like the input is constant
            // simulating a "what if" scenario if the player
            // continued to press the same input
            Vector3 tar = CollideAndSlide(timeDelta, start, targetInput, controllerData.movementSpeed, out bool hitObstacle, out Vector3 normal, true);

            // the obstacle normal
            if (leftWallCounter != 0 && hitObstacle)
                avoidanceNormal = normal;

            // then we check what would happen to the smoothed input in the future
            temporaryInputSmoothed += controllerData.inputSmoothSpeed * timeDelta * (tar - temporaryInputSmoothed);
            // the increased time delta could make the vector have a magnitude larger than 1f
            if (temporaryInputSmoothed.magnitude > 1f)
                temporaryInputSmoothed = temporaryInputSmoothed.normalized;

#if UNITY_EDITOR
            if (showPredictionGizmosInput)
                Debug.DrawLine(start + Vector3.up, start + Vector3.up + temporaryInputSmoothed, Color.yellow);
#endif

            // after we check what would happen to the smoothed input
            // we can simulate the rotation function
            // and figure out where the character is going
            // to be facing in the near future
            characterFw = rotationFunction(start, characterFw, temporaryInputSmoothed, timeDelta);

#if UNITY_EDITOR
            // we render a line that shows where the controller
            // would be facing in the near future
            // we can only do it every few simulated frames
            // so it looks tidy
            if (showPredictionGizmos && frames % 2 == 0)
                Debug.DrawLine(start, start + characterFw * 0.75f, Color.cyan);
#endif

#if UNITY_EDITOR
            Vector3 tempSpeed = velocity;
#endif

            // the we check what would happen to the speed
            Vector3 targSpeed = controllerData.movementSpeed * crouchSpeedMulti * temporaryInputSmoothed;
            // delta (simulated)
            Vector3 speedDelta = targSpeed - velocity;
            // increase the simulated speed
            velocity += speedDelta * Time.deltaTime * controllerData.acceleration;
            // we do a COS pass like in the regular movement function 
            // to simulate collisions with the navmesh and other controllers
            // but this time with simulated "future" data
            velocity = CollideAndSlide(timeDelta, start, velocity, controllerData.movementSpeed, out _, out _, false);

#if UNITY_EDITOR
            // we render a line that shows where the controller
            // would have moved during this simulated frame
            if (showPredictionGizmos)
                Debug.DrawLine(start, start + velocity * timeDelta, Color.white);
#endif

            // then we "move" the controller
            start += velocity * timeDelta;

            // and finally we sample the position just like we did in the movement function
            if (NavMesh.SamplePosition(start, out NavMeshHit positionValidityCheck, controllerData.navmeshCheckRadius, NavMesh.AllAreas))
                start = positionValidityCheck.position;

            // HANDS
            Vector3 left = Vector3.Cross(characterFw, Vector3.up).normalized;

            // left hand
            if (!leftHandObstacle)
            {
                Vector3 handCheckStart = start + controllerData.handObstacleCheckHeight * Vector3.up;
#if UNITY_EDITOR
                if (showPredictionGizmosHands)
                    Debug.DrawLine(handCheckStart, handCheckStart + left * controllerData.handObstacleCheckDistance, new Color(1.0f, 0, 0, 0.2f), 0.05f);
#endif
                if (Physics.SphereCast(handCheckStart, controllerData.handObstacleCheckRadius, left, out RaycastHit raycastHit, controllerData.handObstacleCheckDistance, controllerData.controllerHandObstacleMask))
                {
                    leftHandObstacle = true;
#if UNITY_EDITOR
                    if (showPredictionGizmosHands)
                        Debug.DrawLine(handCheckStart, raycastHit.point, Color.green, 0.05f);
#endif
                }
            }

            // right hand
            if (!rightHandObstacle)
            {
                Vector3 handCheckStart = start + controllerData.handObstacleCheckHeight * Vector3.up;
#if UNITY_EDITOR
                if (showPredictionGizmosHands)
                    Debug.DrawLine(handCheckStart, handCheckStart - left * controllerData.handObstacleCheckDistance, new Color(1.0f, 0, 0, 0.2f), 0.05f);
#endif
                if (Physics.SphereCast(handCheckStart, controllerData.handObstacleCheckRadius, -left, out RaycastHit raycastHit, controllerData.handObstacleCheckDistance, controllerData.controllerHandObstacleMask))
                {
                    rightHandObstacle = true;
#if UNITY_EDITOR
                    if (showPredictionGizmosHands)
                        Debug.DrawLine(handCheckStart, raycastHit.point, Color.green, 0.05f);
#endif
                }
            }

            // SphereCast head hit
            // every third simulated frame
            if (frames % 3 == 0)
                if (Physics.SphereCast(start, controllerData.headHitCheckRadius, Vector3.up, out RaycastHit raycastHit, controllerData.controllerHeight, controllerData.controllerMaskHeadHit))
                {
#if UNITY_EDITOR
                    if (showPredictionGizmos)
                        Debug.DrawLine(start, raycastHit.point, Color.magenta, 0.1f);
#endif
                    float heightDifference = raycastHit.point.y - this.transform.position.y;
                    float tempCrouch = controllerData.controllerHeight - heightDifference;
                    // we only ne the maximum crouch depth required
                    // so that the controller doesn't hit his head
                    if (tempCrouch > crouchDistance)
                    {
                        crouchDistance = tempCrouch;
#if UNITY_EDITOR
                        predHeadHitPosition = raycastHit.point;
#endif
                    }
                }

            // if the character is "colliding" with an obstacle
            // during this simulated frame
            if (hitObstacle)
                //we reset the counter 
                leftWallCounter = 0;
            else
                leftWallCounter++;

            // we only need to know if 
            // the controller "collided" once
            // every other "collision" is not important
            hit = hitObstacle || hit;

            // if the controller has stopped colliding
            // with an obstacle for a long enough period
            // and he was already in collision before
            // we can bail
            if (leftWallCounter > 1 && hit)
                break;
        }

        // if the entire simulation has ended
        // and he didn't stop colliding (or was never)
        // we don't have to take this prediction into account
        if (frames >= predictFrames)
            hit = false;

        // now if we had movement input
        // and the simulation ended with hit==true
        // we can change our input vector
        // to the predicted input vector
        if (hit && gettingMovementInput)
        {
            // final simulated position - current position
            Vector3 hitVector = start - this.transform.position;
            hitVector.y = 0f;
            hitVector = hitVector.normalized * speedMulti;

            Vector3 projection = Vector3.Dot(hitVector, targetInput.normalized) * targetInput.normalized;
            Vector3 tmp = hitVector - projection;

            float dot = Vector3.Dot(targetInput.normalized, avoidanceNormal);
            float absDot = Mathf.Abs(dot);

            if (moveVector.magnitude >= speedMulti)
                inputVectorTargetPredicted = Vector3.Lerp((hitVector + tmp).normalized * speedMulti, tmp, absDot);
        }

        // for the ik script
        crouch = Mathf.Clamp01(crouchDistance / (controllerData.controllerHeight / 2f));

        // slowdown
        crouchSpeedMulti = Mathf.Lerp(1f, controllerData.headHitSlowdown, crouch);

        //finally after the entire loop is done,
        // we set the prediction end position
        // so we can visualize it in the editor
        // #if UNITY_EDITOR
        predPositionEnd = start;
        // #endif

        //set the fw direction
        futureFacingDirection = characterFw;
    }

    #endregion

    #region  Rotation

    // private float rotationThreshold = 0.05f;
    // [Range(1f, 10f)]
    // public float rotationSpeed = 5f;
    protected Vector3 RotationDefault(Vector3 position, Vector3 current, Vector3 targetDir, float timeScale)
    {
        if (gettingMovementInput && targetDir.sqrMagnitude > controllerData.rotationThreshold)
            // rotate
            return Vector3.RotateTowards(current, targetDir.normalized, controllerData.rotationSpeed * rotationMulti * crouchSpeedMulti * timeScale, 1.0f);
        return current;
    }

    public Transform lookAt;

    public void LookAt(Transform _lookAt)
    {
        if (_lookAt == null)
            rotationFunction = RotationDefault;
        else
            rotationFunction = LookAtObject;
        lookAt = _lookAt;
    }

    protected Vector3 LookAtObject(Vector3 position, Vector3 current, Vector3 _, float timeScale)
    {
        Vector3 target = lookAt.position - position;
        target.y = 0f;
        // rotate
        return Vector3.RotateTowards(current, target.normalized, controllerData.rotationSpeed * timeScale, 1.0f);
    }

    #endregion

    #region Movement

    // [Range(2f, 20f)]
    // public float movementSpeed = 5f;
    // [Range(2f, 20f)]
    // public float acceleration = 5f;

    private bool isStationary = false;
    private Vector3 moveVector = Vector3.zero;

    void MoveController()
    {
        Vector3 moveVectorTarget = controllerData.movementSpeed * crouchSpeedMulti * inputSmoothed;
        Vector3 speedDelta = moveVectorTarget - moveVector;

        //smooth increase
        moveVector += speedDelta * Time.deltaTime * controllerData.acceleration;

        //collide and slide
        moveVector = CollideAndSlide(Time.deltaTime, this.transform.position, moveVector, controllerData.movementSpeed, out _, out _);

        //in case the game lags and the speed becomes larger than the max speed due to time.deltaTime becoming huge
        if (moveVector.magnitude > controllerData.movementSpeed)
            moveVector = moveVector.normalized * controllerData.movementSpeed;

        //stationary
        if (moveVector.sqrMagnitude <= 0.01f)
            isStationary = true;
        //moving
        else
        {
            isStationary = false;
            Vector3 targetPosition = this.transform.position + moveVector * Time.deltaTime;
            MoveController(targetPosition);
#if UNITY_EDITOR
            if (showMovementGizmos)
                Debug.DrawLine(this.transform.position, this.transform.position + moveVector, Color.red, 0.1f);
#endif
        }
    }

    //how far should the SamplePosition function check to find valid positions
    // public float navmeshCheckRadius = 0.25f;

    //simple move function using NavMesh.SamplePosition
    protected void MoveController(Vector3 newPosition)
    {
        if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, controllerData.navmeshCheckRadius, NavMesh.AllAreas))
            this.transform.position = hit.position;
    }

    #endregion

    #region  CollideAndSlide

    protected Vector3 CollideAndSlide(float timeDelta, Vector3 start, Vector3 speedVector, float speed, out bool hitObstacle, out Vector3 normal, bool inputSlide = false)
    {
        Vector3 slidVector = speedVector;
        hitObstacle = false;
        normal = Vector3.up;

        int i = 0;
        while (i < 3)
        {
            // the radius of the navmesh agent
            float agentCheckRadius = GetComponent<NavMeshAgent>().radius;

            // COS with other controllers
            // we move the start a bit backwards to avoid hitting backfaces when the controllers are touching each other
            if (Physics.SphereCast(start - slidVector.normalized * 0.1f, agentCheckRadius, slidVector.normalized, out RaycastHit agentHit,
             controllerData.movementSpeed * timeDelta + 0.2f, controllerData.controllerMask, QueryTriggerInteraction.Collide))
            {
                if (agentHit.collider.gameObject != this.gameObject)
                {
                    hitObstacle = true;
                    // get normal and make it planar
                    normal = agentHit.normal;
                    normal.y = 0f;
                    normal = normal.normalized;

                    Vector3 projection = normal * Vector3.Dot(slidVector, normal);
                    // we're using a navmesh so the y component
                    // is just gonna cause problems
                    // lets keep it planar
                    projection.y = 0f;

                    //remove projection
                    slidVector -= projection;

                    i++;
                    //same as for normal collisions
                    if (i >= 2 && inputSlide)
                        return Vector3.zero;
                    continue;
                }
            }

            //check if the controller is about to hit a wall(navmesh border)
            if (NavMesh.Raycast(start, start + slidVector.normalized * speed * timeDelta, out NavMeshHit hit, NavMesh.AllAreas))
            {
                normal = hit.normal;
                hitObstacle = true;
                Vector3 projection = normal * Vector3.Dot(slidVector, normal);
                // we're using a navmesh so the y component
                // is just gonna cause problems
                // lets keep it planar
                projection.y = 0f;

                //remove projection
                slidVector -= projection;

                i++;
                // if a wall is hit 2 or more times
                // and we're doing the COS for input
                // return 0
                if (i >= 2 && inputSlide)
                    return Vector3.zero;
            }
            else
                return slidVector;
        }
        return slidVector;
    }

    #endregion

    #region  Input
    //so the controls are camera relative
    // public GameObject cameraPivot;

    // useful af to have exposed like this
    public Vector3 Target => inputSmoothed * crouchSpeedMulti;
    private Vector3 inputSmoothed = Vector3.zero;

    private Vector3 targetInput = Vector3.zero;

    // [Range(1f, 20f)]
    // public float inputSmoothSpeed = 8f;

    void HandleInput()
    {
        // we predict what would happen in the future
        // with the current target input
        PredictCollisions();

        //then we change the input smoothing to use the predicted input
        // inputSmoothed += controllerData.inputSmoothSpeed * Time.deltaTime * (targetInput - inputSmoothed);
        inputSmoothed += controllerData.inputSmoothSpeed * Time.deltaTime * (inputVectorTargetPredicted - inputSmoothed);
        inputSmoothed = CollideAndSlide(Time.deltaTime, this.transform.position, inputSmoothed, controllerData.movementSpeed, out _, out _, true);

        if (inputSmoothed.magnitude > 1f)
            inputSmoothed = inputSmoothed.normalized;

#if UNITY_EDITOR
        if (showInputGizmos)
            Debug.DrawLine(characterModelRoot.position, characterModelRoot.position + inputSmoothed * 2f, Color.magenta);
#endif
    }

    #endregion

    #region  InputRaw

    protected void Cancel()
    {
        InputAxis(Vector2.zero);
    }

    private float speedMulti = 1f;
    private float rotationMulti = 1f;
    public void StartWalk()
    {
        speedMulti = controllerData.walkSpeedMulti;
        rotationMulti = controllerData.walkRotationSpeedMulti;
        InputAxis(inputRaw);
    }

    public void EndWalk()
    {
        speedMulti = 1f;
        rotationMulti = 1f;
        InputAxis(inputRaw.normalized);
    }

    private Vector2 inputRaw = Vector2.zero;
    private bool gettingMovementInput = false;
    //temporary solution
    public void InputAxis(Vector2 axis)
    {
        gettingMovementInput = axis.sqrMagnitude > 0.01f;
        inputRaw = axis * speedMulti;
        // Vector3 target = inputRaw.x * cameraPivot.transform.right + inputRaw.y * cameraPivot.transform.forward;
        targetInput = inputRaw.x * CameraController.controller.transform.right + inputRaw.y * CameraController.controller.transform.forward;
    }

    #endregion

    #region Gizmos

#if UNITY_EDITOR

    private struct OldData
    {
        public Vector3 dir;
        public Vector3 pos;
    }
    void OnDrawGizmos()
    {
        if (!showPredictionGizmos)
            return;
        Gizmos.color = Color.gray;
        for (int i = 0; i < oldData.Length; i++)
            Gizmos.DrawLine(oldData[i].pos, oldData[i].pos + oldData[i].dir * 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(predPositionStart, 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(predPositionEnd, 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(predHeadHitPosition, controllerData.headHitCheckRadius);
    }
#endif

    #endregion

}


