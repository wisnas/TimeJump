using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerStrafe : MonoBehaviour
{
    [Header("Air Strafe")]
    [SerializeField] private float airCap = 2f;
    [SerializeField] private float airAcceleration = 70f;
    [SerializeField] private float airTurnSmoothness = 12f;

    private PlayerMovement pm;

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
    }

    public void ApplyAirAccelerate()
    {
        Vector3 wishDir = (pm.Orientation.forward * pm.MoveInput.y + pm.Orientation.right * pm.MoveInput.x).normalized;

        if (wishDir.sqrMagnitude < 0.001f) return;

        Vector3 currentFlatVel = new Vector3(pm.Rb.linearVelocity.x, 0f, pm.Rb.linearVelocity.z);

        float currentSpeedInWishDir = Vector3.Dot(currentFlatVel, wishDir);
        float addSpeed = airCap - currentSpeedInWishDir;

        if (addSpeed > 0f)
        {
            float accelSpeed = airAcceleration * Time.fixedDeltaTime;
            accelSpeed = Mathf.Min(accelSpeed, addSpeed);
            currentFlatVel += wishDir * accelSpeed;
        }

        if (Mathf.Abs(pm.MoveInput.x) > 0.05f && currentFlatVel.sqrMagnitude > 1f)
        {
            float currentMag = currentFlatVel.magnitude;
            Vector3 targetDirection = Vector3.RotateTowards(
                currentFlatVel.normalized,
                wishDir,
                (airTurnSmoothness * Mathf.Deg2Rad) * Time.fixedDeltaTime,
                0f
            );
            currentFlatVel = targetDirection * currentMag;
        }

        pm.Rb.linearVelocity = new Vector3(currentFlatVel.x, pm.Rb.linearVelocity.y, currentFlatVel.z);
    }
}