using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 14f;
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float groundDeceleration = 10f;

    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Camera playerCamera;

    [Header("Jump & Ground Check")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float standingCastLength = 1.1f;
    [SerializeField] private float crouchCastLength = 0.6f;
    [SerializeField] private float castRadius = 0.35f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Step Handling")]
    [Tooltip("Maximální výška schodu/pøekážky (napø. 0.45).")]
    [SerializeField] private float maxStepHeight = 0.45f;
    [Tooltip("Jak hluboko pod støedem hráèe jsou boty? (u 2m postavy cca 0.95)")]
    [SerializeField] private float stepLowerOffset = 0.95f;
    [Tooltip("Dosah paprsku pøed nohy (MUSÍ být delší než polomìr hráèe, napø. 0.6).")]
    [SerializeField] private float stepSearchDistance = 0.6f;
    [Tooltip("Rychlost, jakou se hráè vyhladí na schod.")]
    [SerializeField] private float stepSmooth = 18f;

    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;
    public InputActionReference jumpAction;

    [Header("Camera Settings")]
    [SerializeField] private float cameraFov = 70f;
    [SerializeField] private float cameraSprintFov = 85f;
    [SerializeField] private float fovTransitionSpeed = 10f;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 40f;
    private RaycastHit slopeHit;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    public Rigidbody Rb { get; private set; }
    public PlayerSlide SlideModule { get; private set; }
    public PlayerWallRun WallRunModule { get; private set; }
    public PlayerStrafe StrafeModule { get; private set; }
    public PlayerClimbLedge ClimbModule { get; private set; }

    public Transform Orientation => orientation != null ? orientation : transform;
    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDir { get; private set; }
    public bool IsGrounded { get; private set; }
    public float LastFallVelocity { get; private set; }
    public float MaxSlopeAngle => maxSlopeAngle;
    public RaycastHit SlopeHit => slopeHit;
    public float SprintSpeed => sprintSpeed;

    private bool wasGrounded;
    private float lastJumpTime;
    private float targetFov;

    public enum PlayerState
    {
        walking,
        running,
        crouching,
        sliding,
        wallrunning,
        climbing,
        air
    }
    public PlayerState state { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;

        SlideModule = GetComponent<PlayerSlide>();
        WallRunModule = GetComponent<PlayerWallRun>();
        StrafeModule = GetComponent<PlayerStrafe>();
        ClimbModule = GetComponent<PlayerClimbLedge>();

        if (playerCamera == null) playerCamera = Camera.main;
        targetFov = cameraFov;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (crouchAction != null) crouchAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
        if (crouchAction != null) crouchAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
    }

    private void Update()
    {
        if (moveAction != null) MoveInput = moveAction.action.ReadValue<Vector2>();

        IsGrounded = CheckGround();

        if (IsGrounded && !wasGrounded && SlideModule != null)
        {
            SlideModule.HandleLanding();
        }

        if (!IsGrounded)
        {
            LastFallVelocity = Rb.linearVelocity.y;
        }

        if (WallRunModule != null) WallRunModule.CheckWall();

        StateHandler();
        HandleJump();
        HandleCameraFov();

        wasGrounded = IsGrounded;
    }

    private void FixedUpdate()
    {
        if (state == PlayerState.climbing) return;

        MoveDir = Orientation.forward * MoveInput.y + Orientation.right * MoveInput.x;

        ApplyMovementPhysics();
        HandleSteps();
        SpeedControl();
        DrawDebugRays();
    }

    private bool CheckGround()
    {
        if (Time.time < lastJumpTime + 0.2f) return false;

        float currentCastLength = (state == PlayerState.crouching || state == PlayerState.sliding)
            ? crouchCastLength
            : standingCastLength;

        return Physics.SphereCast(transform.position, castRadius, Vector3.down, out _, currentCastLength, whatIsGround);
    }

    private void StateHandler()
    {
        if (state == PlayerState.climbing) return;

        bool sprintHeld = sprintAction != null && sprintAction.action.IsPressed();
        bool crouchHeld = crouchAction != null && crouchAction.action.IsPressed();
        bool crouchPressed = crouchAction != null && crouchAction.action.WasPressedThisFrame();
        Vector3 flatVel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);

        Rb.useGravity = !OnSlope();

        if (WallRunModule != null && WallRunModule.CanWallRun())
        {
            SetState(PlayerState.wallrunning);
            Rb.useGravity = false;
            Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
            Rb.linearDamping = 0f;
            return;
        }

        if (state == PlayerState.sliding && SlideModule != null)
        {
            SlideModule.HandleSlidingState(crouchHeld, flatVel);
            return;
        }

        if (!IsGrounded)
        {
            SetState(PlayerState.air);
            if (SlideModule != null) SlideModule.ResetScale();
            Rb.linearDamping = 0.1f;
            return;
        }

        Rb.linearDamping = groundDrag;

        switch (state)
        {
            case PlayerState.air:
                if (SlideModule != null) SlideModule.SetCrouchScale(crouchHeld);
                SetState(crouchHeld ? PlayerState.crouching : (sprintHeld ? PlayerState.running : PlayerState.walking));

                if (crouchHeld && flatVel.magnitude > 10f && SlideModule != null)
                {
                    SlideModule.StartSlide();
                }
                break;

            case PlayerState.walking:
                if (crouchPressed || crouchHeld)
                {
                    if (flatVel.magnitude > 10f && SlideModule != null)
                        SlideModule.StartSlide();
                    else if (SlideModule != null)
                        SlideModule.StartCrouch();
                }
                else if (sprintHeld && MoveInput.sqrMagnitude > 0.01f)
                {
                    SetState(PlayerState.running);
                }
                break;

            case PlayerState.running:
                if (crouchPressed && SlideModule != null)
                {
                    SlideModule.StartSlide();
                }
                else if (!sprintHeld || MoveInput.sqrMagnitude < 0.01f)
                {
                    SetState(PlayerState.walking);
                }
                break;

            case PlayerState.crouching:
                if (!crouchHeld)
                {
                    if (SlideModule == null || SlideModule.CanStandUp())
                    {
                        if (SlideModule != null) SlideModule.ResetScale();
                        SetState(PlayerState.walking);
                    }
                }
                break;
        }
    }

    public void SetState(PlayerState newState) => state = newState;

    private void ApplyMovementPhysics()
    {
        if (state == PlayerState.wallrunning && WallRunModule != null)
        {
            WallRunModule.ApplyWallRunPhysics();
            return;
        }

        if (state == PlayerState.sliding && SlideModule != null)
        {
            SlideModule.ApplySlidePhysics();
            return;
        }

        if (!IsGrounded || state == PlayerState.air)
        {
            if (MoveInput.sqrMagnitude > 0.01f && StrafeModule != null)
            {
                StrafeModule.ApplyAirAccelerate();
            }
            return;
        }

        if (MoveInput.sqrMagnitude < 0.01f) return;

        float targetSpeed = state switch
        {
            PlayerState.crouching => SlideModule != null ? SlideModule.CrouchSpeed : walkSpeed,
            PlayerState.running => sprintSpeed,
            _ => walkSpeed
        };

        if (OnSlope())
        {
            Rb.AddForce(GetSlopeDir(MoveDir) * (targetSpeed * 10f), ForceMode.Force);

            if (Rb.linearVelocity.y < 0)
                Rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            else if (Rb.linearVelocity.y > 0)
                Rb.AddForce(Vector3.down * 40f, ForceMode.Force);
        }
        else
        {
            Rb.AddForce(MoveDir.normalized * (targetSpeed * 10f), ForceMode.Force);
        }
    }

    private void HandleSteps()
    {
        if (!IsGrounded || MoveInput.sqrMagnitude < 0.01f || state == PlayerState.climbing || state == PlayerState.sliding) return;

        Vector3 moveDir = MoveDir.normalized;

        Vector3 lowerCenter = transform.position + Vector3.down * stepLowerOffset;
        Vector3 upperCenter = lowerCenter + Vector3.up * maxStepHeight;
        Vector3 rightOffset = Orientation.right * 0.3f;


        if (showDebug)
        {
            Debug.DrawRay(lowerCenter, moveDir * stepSearchDistance, Color.red);
            Debug.DrawRay(upperCenter, moveDir * (stepSearchDistance + 0.1f), Color.green);
        }


        if (CheckStep(lowerCenter, upperCenter, moveDir)) return;
        if (CheckStep(lowerCenter + rightOffset, upperCenter + rightOffset, moveDir)) return;
        if (CheckStep(lowerCenter - rightOffset, upperCenter - rightOffset, moveDir)) return;
    }

    private bool CheckStep(Vector3 lower, Vector3 upper, Vector3 dir)
    {

        if (Physics.Raycast(lower, dir, out RaycastHit hitLower, stepSearchDistance, whatIsGround))
        {

            float angle = Vector3.Angle(hitLower.normal, Vector3.up);
            if (angle > 65f)
            {

                if (!Physics.Raycast(upper, dir, stepSearchDistance + 0.15f, whatIsGround))
                {

                    Vector3 downStart = upper + dir * (hitLower.distance + 0.15f);
                    if (Physics.Raycast(downStart, Vector3.down, out RaycastHit hitDown, maxStepHeight + 0.2f, whatIsGround))
                    {

                        if (Vector3.Angle(hitDown.normal, Vector3.up) <= maxSlopeAngle)
                        {
                            float targetY = hitDown.point.y + stepLowerOffset + 0.05f;
                            Vector3 targetPos = new Vector3(Rb.position.x, targetY, Rb.position.z);

                            Rb.position = Vector3.Lerp(Rb.position, targetPos, Time.fixedDeltaTime * stepSmooth);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private void SpeedControl()
    {
        if (state == PlayerState.sliding || state == PlayerState.wallrunning || (!IsGrounded && state == PlayerState.air)) return;

        Vector3 flatVel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);

        float limitSpeed = state switch
        {
            PlayerState.crouching => SlideModule != null ? SlideModule.CrouchSpeed : walkSpeed,
            PlayerState.running => sprintSpeed,
            _ => walkSpeed
        };

        if (flatVel.magnitude > limitSpeed)
        {
            Vector3 targetVel = flatVel.normalized * limitSpeed;
            Vector3 smoothedVel = Vector3.MoveTowards(flatVel, targetVel, groundDeceleration * Time.fixedDeltaTime);
            Rb.linearVelocity = new Vector3(smoothedVel.x, Rb.linearVelocity.y, smoothedVel.z);
        }
    }

    private void HandleJump()
    {
        if (jumpAction == null || !jumpAction.action.WasPressedThisFrame()) return;
        if (state == PlayerState.climbing) return;
        if (ClimbModule != null && ClimbModule.CanClimb(out _)) return;

        if (IsGrounded)
        {
            Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
            Rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (SlideModule != null) SlideModule.ResetScale();
            SetState(PlayerState.air);
            lastJumpTime = Time.time;
        }
        else if (state == PlayerState.wallrunning && WallRunModule != null)
        {
            WallRunModule.WallJump();
        }
    }

    private void HandleCameraFov()
    {
        if (playerCamera == null) return;
        Vector3 flatVel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);

        bool shouldSprintFov = state == PlayerState.running ||
                               state == PlayerState.sliding ||
                               state == PlayerState.wallrunning ||
                               flatVel.magnitude > 10f;

        targetFov = shouldSprintFov ? cameraSprintFov : cameraFov;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * fovTransitionSpeed);
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 1.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeDir(Vector3 dir)
    {
        return Vector3.ProjectOnPlane(dir, slopeHit.normal).normalized;
    }

    private void DrawDebugRays()
    {
        if (!showDebug) return;
        Debug.DrawRay(transform.position, MoveDir.normalized * 2f, Color.green);
        Debug.DrawRay(transform.position, Rb.linearVelocity.normalized * 2f, Color.red);
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        Vector3 flatVel = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
        GUIStyle style = new GUIStyle { fontSize = 16 };
        style.normal.textColor = Color.yellow;

        GUILayout.BeginArea(new Rect(15, 15, 320, 240));
        GUILayout.Label($"STAV: {state}", style);
        GUILayout.Label($"Grounded: {IsGrounded}", style);
        GUILayout.Label($"Rychlost: {flatVel.magnitude:F2}", style);
        GUILayout.Label($"LinearDamping: {Rb.linearDamping:F1}", style);
        GUILayout.Label($"Velocity y: {Rb.linearVelocity.y:F1}", style);
        GUILayout.EndArea();
    }
}