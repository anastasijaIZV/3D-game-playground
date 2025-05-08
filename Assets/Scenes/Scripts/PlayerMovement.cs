using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public Transform head;
    public Transform orientation;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float jumpPower = 4f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 90f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 2f;
    public float slideSpeed = 9f;
    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;
    public float impactThreshold;
    private bool isSliding = false;
    private float originalWalkSpeed = 4f;
    private float originalRunSpeed = 6f;
    public Transform holdPoint;



    [Header("Camera Effects")]
    public float baseCameraFov = 60f;
    public float baseCameraHeight = 0.85f;
    public float walkBobbingRate = 0.75f;
    public float runBobbingRate = 1f;
    public float maxWalkBobbingOffset = 0.2f;
    public float maxRunBobbingOffset = 0.3f;
    public float cameraShakeThreshold = 10f;
    [Range(0f, 0.03f)] public float cameraShakeRate = 0.015f;
    public float maxVerticalFallShakeAngle = 40f;
    public float maxHorizontalFallShakeAngle = 40f;

    [Header("Climbing")]
    public float climbSpeed = 5f;
    public float maxClimbTime = 3f;
    private float climbTimer;
    private bool climbing;
    public LayerMask whatIsWall;

    [Header("Detection")]
    public float detectionLength = 0.5f;
    public float sphereCastRadius = 1f;
    public float maxWallLookAngle = 20f;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange = 45f;

    [Header("Exiting")]
    public bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private bool canMove = true;
    private bool isCrouching = false;

    private Transform attachedObject = null;
    private float attachedDistance = 0f;
    public float itemPickupDistance = 5f;

    private Rigidbody rb;
    private RaycastHit slopeHit;
    public float maxSlopeAngle = 45f;
    private Vector3 e;
    private bool grounded => characterController.isGrounded;

    void Start()
{
    characterController = GetComponent<CharacterController>();
    rb = GetComponent<Rigidbody>();
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    originalWalkSpeed = walkSpeed;
    originalRunSpeed = runSpeed;
}


    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleJump();
        HandleCrouch();
        HandlePickup();
        HandleClimbing();
        HandleCameraEffects();
        sliding();

        if (attachedObject != null)
{
    attachedObject.position = playerCamera.transform.position + playerCamera.transform.forward * attachedDistance;
}

    }

    void HandleMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    void HandleJump()
    {
        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
    }

    void HandleCrouch()
{
    if (isSliding) return;

    if (Input.GetKey(KeyCode.C) && canMove)
    {
        if (!isCrouching)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
            isCrouching = true;
        }
    }
    else if (isCrouching)
    {
        characterController.height = defaultHeight;
        walkSpeed = originalWalkSpeed;
        runSpeed = originalRunSpeed;
        isCrouching = false;
    }
}



    void HandlePickup()
{

    Debug.DrawRay(head.position, head.forward * itemPickupDistance, Color.red);


    RaycastHit hit;
    bool cast = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, itemPickupDistance);


    if (Input.GetKeyDown(KeyCode.F))
    {
        // Drop the object
        if (attachedObject != null)
        {
            attachedObject.SetParent(null);

            if (attachedObject.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = false;

            if (attachedObject.TryGetComponent<Collider>(out var col))
                col.enabled = true;

            attachedObject = null;
        }
        // Pick up a new object
        else if (cast && hit.transform.CompareTag("pickable"))
        {
            attachedObject = hit.transform;
            attachedObject.SetParent(holdPoint);
            attachedObject.localPosition = Vector3.zero;
            attachedDistance = Vector3.Distance(attachedObject.position, head.position);

            if (attachedObject.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;

            if (attachedObject.TryGetComponent<Collider>(out var col))
                col.enabled = false;
        }
    }
}


    void HandleCameraEffects()
    {
        float fovOffset = (rb != null && rb.velocity.y < 0f) ? Mathf.Sqrt(Mathf.Abs(rb.velocity.y)) : 0f;
        // playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, baseCameraFov + fovOffset, 0.25f);

        if (!grounded && rb != null && Mathf.Abs(rb.velocity.y) >= cameraShakeThreshold)
        {
            Vector3 newAngle = head.localEulerAngles;
            newAngle += Vector3.right * Random.Range(-maxVerticalFallShakeAngle, maxVerticalFallShakeAngle);
            newAngle += Vector3.up * Random.Range(-maxHorizontalFallShakeAngle, maxHorizontalFallShakeAngle);
            head.localEulerAngles = Vector3.Lerp(head.localEulerAngles, newAngle, cameraShakeRate);
        }
        else
        {
            e = head.localEulerAngles;
            e.y = 0f;
            head.localEulerAngles = e;
        }
    }

    void HandleClimbing()
    {
        if (Input.GetKey(KeyCode.G))
        {
            walkSpeed = climbSpeed;
            WallCheck();
            StateMachine();
            if (climbing && !exitingWall) ClimbingMovement();
        }
    }

    void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if ((wallFront && newWall) || grounded)
        {
            climbTimer = maxClimbTime;
        }
    }

    void StateMachine()
    {
        if (wallFront && Input.GetKey(KeyCode.G) && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing && climbTimer > 0)
            {
                StartClimbing();
            }

            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            if (climbTimer < 0) StopClimbing();
        }
        else if (exitingWall)
        {
            if (Input.GetKey(KeyCode.G)) StopClimbing();

            if (exitWallTimer > 0) exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0) exitingWall = false;
        }
        else
        {
            if (climbing) StopClimbing();
        }
    }

    void StartClimbing()
    {
        climbing = true;
        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
    }

    void ClimbingMovement()
    {
        if (rb != null)
        {
            rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
        }
        moveDirection.y = climbSpeed;
    }

    void StopClimbing()
    {
        climbing = false;
    }

    void sliding()
{
    if (Input.GetKey(KeyCode.L) && canMove)
    {
        isSliding = true;
        characterController.height = crouchHeight;
        walkSpeed = slideSpeed;
    }
    else if (isSliding)
    {
        isSliding = false;
        characterController.height = defaultHeight;
        walkSpeed = originalWalkSpeed;
        runSpeed = originalRunSpeed;
    }
}



    public static float RestrictAngle(float angle, float angleMin, float angleMax)
    {
        if (angle > 180) angle -= 360;
        else if (angle < -180) angle += 360;

        if (angle > angleMax) angle = angleMax;
        if (angle < angleMin) angle = angleMin;

        return angle;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, defaultHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
