using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerWallRun : MonoBehaviour
{
    [Header("Wall Run Settings")]
    [SerializeField] private float wallRunRange = 0.7f;
    [SerializeField] private float wallJumpUpForce = 8f;
    [SerializeField] private float wallJumpSideForce = 10f;
    [SerializeField] private LayerMask whatIsWall;

    private PlayerMovement pm;
    private RaycastHit wallHit;
    private bool wallLeft;
    private bool wallRight;
    private float wallRunCooldownTimer;

    public float WallRunCameraTilt
    {
        get
        {
            if (pm.state != PlayerMovement.PlayerState.wallrunning) return 0f;
            if (wallLeft) return -1f;
            if (wallRight) return 1f;
            return 0f;
        }
    }

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (wallRunCooldownTimer > 0)
        {
            wallRunCooldownTimer -= Time.deltaTime;
        }
    }

    public void CheckWall()
    {
        Vector3 leftDir = (-pm.Orientation.right + pm.Orientation.forward * 0.35f).normalized;
        Vector3 rightDir = (pm.Orientation.right + pm.Orientation.forward * 0.35f).normalized;

        wallLeft = Physics.Raycast(transform.position, leftDir, out RaycastHit hitL, wallRunRange, whatIsWall);
        wallRight = Physics.Raycast(transform.position, rightDir, out RaycastHit hitR, wallRunRange, whatIsWall);

        if (wallLeft) wallHit = hitL;
        else if (wallRight) wallHit = hitR;

        Debug.DrawRay(transform.position, -pm.Orientation.right * wallRunRange, Color.yellow);
        Debug.DrawRay(transform.position, pm.Orientation.right * wallRunRange, Color.yellow);
    }

    public bool CanWallRun()
    {
        return (wallLeft || wallRight) && !pm.IsGrounded && pm.MoveInput.y > 0.1f && wallRunCooldownTimer <= 0;
    }

    public void ApplyWallRunPhysics()
    {
        Vector3 wallNormal = wallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

        if ((pm.Orientation.forward - wallForward).sqrMagnitude > (pm.Orientation.forward - -wallForward).sqrMagnitude)
        {
            wallForward = -wallForward;
        }

        bool pushingAway = Vector3.Dot(pm.MoveDir.normalized, wallNormal) > 0.25f;
        Vector3 tangentVelocity = wallForward * pm.SprintSpeed;

        Vector3 stickVelocity = Vector3.zero;
        if (!pushingAway)
        {
            float desiredDist = 0.35f + 0.2f;
            float distDelta = wallHit.distance - desiredDist;
            stickVelocity = -wallNormal * (distDelta * 12f);
        }

        float yVel = Mathf.Clamp(pm.Rb.linearVelocity.y, -1.5f, 0f);

        pm.Rb.linearVelocity = new Vector3(
            tangentVelocity.x + stickVelocity.x,
            yVel,
            tangentVelocity.z + stickVelocity.z
        );
    }

    public void WallJump()
    {
        Vector3 wallNormal = wallHit.normal;
        Vector3 jumpDir = (wallNormal * wallJumpSideForce) + (Vector3.up * wallJumpUpForce) + (pm.Orientation.forward * 4f);

        pm.Rb.linearVelocity = new Vector3(pm.Rb.linearVelocity.x, 0f, pm.Rb.linearVelocity.z);
        pm.Rb.AddForce(jumpDir, ForceMode.Impulse);

        wallRunCooldownTimer = 0.3f;
        pm.SetState(PlayerMovement.PlayerState.air);
    }
}