using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerSlide : MonoBehaviour
{
    [Header("Speeds & Friction")]
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float slideForce = 8f;
    [SerializeField] private float slideDrag = 0.5f;
    [SerializeField] private float slideSlopeDownMinVel = -2f;
    [SerializeField] private float downwardAcceleration = 25f;

    [Header("Heights")]
    [SerializeField] private float crouchHeightRatio = 0.5f;
    [SerializeField] private float standUpClearance = 1.1f;

    [Header("Landing Boost")]
    [SerializeField] private float slopeLandingBoostMultiplier = 1.5f;

    private PlayerMovement pm;
    private float defaultHeight;
    private float crouchHeight;
    private bool isCrouched;

    public float CrouchSpeed => crouchSpeed;

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        defaultHeight = transform.localScale.y;
        crouchHeight = defaultHeight * crouchHeightRatio;
    }

    public void StartCrouch()
    {
        pm.SetState(PlayerMovement.PlayerState.crouching);
        ApplyCrouchScale();

        pm.Rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    public void StartSlide(Vector3? customPushDirAndForce = null)
    {
        pm.SetState(PlayerMovement.PlayerState.sliding);
        ApplyCrouchScale();

        pm.Rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        if (customPushDirAndForce.HasValue)
        {
            pm.Rb.AddForce(customPushDirAndForce.Value, ForceMode.Impulse);
        }
        else
        {
            Vector3 pushDirection = pm.MoveDir.sqrMagnitude > 0.01f
                ? pm.MoveDir.normalized
                : pm.Orientation.forward;

            float smoothForce = slideForce * 0.45f;

            if (!pm.OnSlope() || pm.Rb.linearVelocity.y > -0.1f)
            {
                pm.Rb.AddForce(pushDirection * smoothForce, ForceMode.Impulse);
            }
            else
            {
                pm.Rb.AddForce(pm.GetSlopeDir(pm.MoveDir) * smoothForce, ForceMode.Impulse);
                Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, pm.SlopeHit.normal).normalized;
                pm.Rb.AddForce(slopeDown * smoothForce, ForceMode.Acceleration);
            }
        }
    }

    public void HandleSlidingState(bool crouchHeld, Vector3 flatVel)
    {
        if (pm.OnSlope() && pm.Rb.linearVelocity.y < slideSlopeDownMinVel)
        {
            pm.Rb.linearDamping = 0.15f;
        }
        else
        {
            pm.Rb.linearDamping = slideDrag;
        }
        if (!pm.IsGrounded && pm.Rb.linearVelocity.y < -1f)
        {
            pm.SetState(PlayerMovement.PlayerState.air);
            return;
        }

        if (!crouchHeld)
        {
            if (CanStandUp())
            {
                ResetScale();
                pm.SetState(PlayerMovement.PlayerState.walking);
            }
            else if (flatVel.magnitude <= crouchSpeed + 1f)
            {
                pm.SetState(PlayerMovement.PlayerState.crouching);
            }
        }
        else if (flatVel.magnitude <= crouchSpeed + 0.5f)
        {
            pm.SetState(PlayerMovement.PlayerState.crouching);
        }
    }

    public void ApplySlidePhysics()
    {
        if (pm.OnSlope())
        {
            Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, pm.SlopeHit.normal);
            pm.Rb.AddForce(slopeDown * downwardAcceleration, ForceMode.Acceleration);
        }
    }

    public void HandleLanding()
    {
        bool crouchHeld = pm.crouchAction != null && pm.crouchAction.action.IsPressed();

        if (crouchHeld && pm.LastFallVelocity < -3f && pm.OnSlope())
        {
            Vector3 slopeDownDir = pm.GetSlopeDir(pm.Rb.linearVelocity);
            float angle = Vector3.Angle(Vector3.up, pm.SlopeHit.normal);
            float slopeFactor = angle / pm.MaxSlopeAngle;

            float impactSpeed = Mathf.Clamp(Mathf.Abs(pm.LastFallVelocity), 0f, 12f);

            Vector3 boostMomentum = slopeDownDir * (impactSpeed * slopeFactor * slopeLandingBoostMultiplier * 0.35f);

            StartSlide(boostMomentum);
        }
    }

    public bool CanStandUp()
    {
        return !Physics.SphereCast(transform.position, 0.35f, Vector3.up, out _, standUpClearance);
    }

    public void ApplyCrouchScale()
    {
        if (isCrouched) return;
        isCrouched = true;

        float oldY = transform.localScale.y;
        transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);

        transform.position += Vector3.down * ((oldY - crouchHeight) / 2f);
    }

    public void ResetScale()
    {
        if (!isCrouched) return;
        if (!CanStandUp()) return;

        float oldY = transform.localScale.y;
        transform.localScale = new Vector3(transform.localScale.x, defaultHeight, transform.localScale.z);

        transform.position += Vector3.up * ((defaultHeight - oldY) / 2f);

        isCrouched = false;
    }

    public void SetCrouchScale(bool crouching)
    {
        if (crouching) ApplyCrouchScale();
        else ResetScale();
    }
}