using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 1f;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform cameraHolder;

    [Header("Wallrun Tilt")]
    [SerializeField] private PlayerWallRun playerWallRun;
    [SerializeField] private float tiltAngle = 10f;
    [SerializeField] private float tiltSpeed = 10f;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    private float xRotation;
    private float yRotation;
    private float currentTilt;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerWallRun == null)
        {
            playerWallRun = GetComponentInParent<PlayerWallRun>();
        }
    }

    private void OnEnable()
    {
        if (lookAction != null) lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (lookAction != null) lookAction.action.Disable();
    }

    private void Update()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * sensitivity * 0.05f;
        float mouseY = lookInput.y * sensitivity * 0.05f;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        float targetTilt = 0f;
        if (playerWallRun != null)
        {
            targetTilt = playerWallRun.WallRunCameraTilt * tiltAngle;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        }

        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}