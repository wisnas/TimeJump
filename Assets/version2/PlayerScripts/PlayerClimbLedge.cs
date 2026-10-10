using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerClimbLedge : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float wallCheckDistance = 0.9f;
    [Tooltip("Záporná hodnota = detekce øímsy i když padáš a máš ji u nohou")]
    [SerializeField] private float minLedgeHeight = -0.85f;
    [SerializeField] private float maxLedgeHeight = 2.4f;
    [SerializeField] private float maxAngle = 25f;
    [SerializeField] private LayerMask whatIsLedge;

    [Header("Sprint Settings")]
    [Tooltip("O kolik dál dosáhneš pøi sprintu (napø. 1.5 = o 50% dál)")]
    [SerializeField] private float sprintRangeMultiplier = 1.5f;
    [Tooltip("O kolik vìtší je výstupní impuls pøi sprintu")]
    [SerializeField] private float sprintExitBoostMultiplier = 1.35f;

    [Header("Climb Motion")]
    [SerializeField] private float climbDuration = 0.3f;
    [SerializeField] private float exitForwardBoost = 7f;
    [SerializeField] private AnimationCurve climbCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cooldown")]
    [SerializeField] private float climbCooldown = 0.35f;

    private PlayerMovement pm;
    private bool isClimbing;
    private float lastClimbTime;

    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (isClimbing) return;
        if (Time.time < lastClimbTime + climbCooldown) return;

        bool jumpPressed = pm.jumpAction != null && pm.jumpAction.action.WasPressedThisFrame();
        if (!jumpPressed) return;

        TryClimbLedge();
    }

    private bool IsSprintingNow()
    {
        bool sprintHeld = pm.sprintAction != null && pm.sprintAction.action.IsPressed();
        Vector3 flatVel = new Vector3(pm.Rb.linearVelocity.x, 0f, pm.Rb.linearVelocity.z);
        return sprintHeld || flatVel.magnitude > (pm.SprintSpeed * 0.75f);
    }

    public bool CanClimb(out Vector3 targetStandPos)
    {
        targetStandPos = Vector3.zero;
        Vector3 forward = pm.Orientation.forward;
        Vector3 origin = transform.position;

        bool isSprinting = IsSprintingNow();
        float currentWallDist = isSprinting ? wallCheckDistance * sprintRangeMultiplier : wallCheckDistance;

        float[] rayHeights = { 0.6f, 0f, -0.6f, -0.85f };
        RaycastHit wallHit = new RaycastHit();
        bool foundWall = false;

        foreach (float h in rayHeights)
        {
            if (Physics.Raycast(origin + Vector3.up * h, forward, out RaycastHit hit, currentWallDist, whatIsLedge))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) >= 70f)
                {
                    wallHit = hit;
                    foundWall = true;
                    break;
                }
            }
        }

        if (!foundWall) return false;

        Vector3 downRayStart = origin + (forward * (wallHit.distance + 0.25f)) + (Vector3.up * maxLedgeHeight);
        float rayLength = maxLedgeHeight - minLedgeHeight;

        if (Physics.Raycast(downRayStart, Vector3.down, out RaycastHit ledgeHit, rayLength, whatIsLedge))
        {
            Debug.Log("gala");
            if (Vector3.Angle(ledgeHit.normal, Vector3.up) > maxAngle)
            {
                Debug.Log("ang");
                return false;
            }

            targetStandPos = ledgeHit.point + (Vector3.up * 1.05f) + (forward * 0.35f);
            if (Physics.CheckCapsule(targetStandPos + Vector3.down * 0.5f, targetStandPos + Vector3.up * 0.5f, 0.35f, whatIsLedge))
            {
                Debug.Log("dsds");
                return false;
            }

            return true;
        }

        return false;
    }

    private void TryClimbLedge()
    {
        if (CanClimb(out Vector3 targetStandPos))
        {
            StartCoroutine(PerformClimb(targetStandPos));
        }
    }

    private IEnumerator PerformClimb(Vector3 targetPos)
    {
        isClimbing = true;
        pm.SetState(PlayerMovement.PlayerState.climbing);

        bool wasSprinting = IsSprintingNow();

        pm.Rb.isKinematic = true;
        pm.Rb.linearVelocity = Vector3.zero;

        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float duration = wasSprinting ? climbDuration * 0.85f : climbDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = climbCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
            yield return null;
        }

        transform.position = targetPos;

        pm.Rb.isKinematic = false;
        isClimbing = false;
        lastClimbTime = Time.time;

        if (pm.MoveInput.y > 0.1f)
        {
            float boost = wasSprinting ? exitForwardBoost * sprintExitBoostMultiplier : exitForwardBoost;
            Vector3 forwardBoost = pm.Orientation.forward * boost;
            pm.Rb.linearVelocity = new Vector3(forwardBoost.x, 2.5f, forwardBoost.z);
            pm.SetState(wasSprinting ? PlayerMovement.PlayerState.running : PlayerMovement.PlayerState.walking);
        }
        else
        {
            pm.Rb.linearVelocity = new Vector3(0f, 1.5f, 0f);
            pm.SetState(PlayerMovement.PlayerState.walking);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pm == null) pm = GetComponent<PlayerMovement>();
        if (pm == null) return;

        Gizmos.color = Color.cyan;
        Vector3 fwd = pm.Orientation != null ? pm.Orientation.forward : transform.forward;
        Vector3 origin = transform.position;

        float[] rayHeights = { 0.6f, 0f, -0.6f, -0.85f };
        foreach (float h in rayHeights)
        {
            Gizmos.DrawRay(origin + Vector3.up * h, fwd * wallCheckDistance);
        }
        Gizmos.color = Color.yellow;
        Vector3 downStart = origin + (fwd * wallCheckDistance) + (Vector3.up * maxLedgeHeight);
        Gizmos.DrawRay(downStart, Vector3.down * (maxLedgeHeight - minLedgeHeight));
    }
}