using UnityEngine;
using static MenuCameraPlacementHandler;
using UnityEngine.InputSystem;

public class DoorsHandler : MonoBehaviour
{
    private Camera targetCamera;
    public Collider doorColidor;
    public GameObject doorPivot;

    public Vector3 openDoj;
    public Vector3 closeDoj;

    public float delay = 1f;

    public float animationDuration = 0.5f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float hoverTimer = 0f;
    private float openProgress = 0f;
    private Quaternion targetRotation;

    private Quaternion closeRotation;
    private Quaternion openRotation;
    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        targetRotation = Quaternion.Euler(closeDoj);
        if (doorPivot != null)
            doorPivot.transform.localRotation = targetRotation;

        closeRotation = Quaternion.Euler(closeDoj);
        openRotation = Quaternion.Euler(openDoj);
    }
    private void Update()
    {

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        bool isHovering = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider == doorColidor)
            {
                isHovering = true;
            }
        }


        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            if (hoverTimer >= delay)
            {
                openProgress += Time.deltaTime / animationDuration;
            }
        }
        else
        {
            hoverTimer = 0f;
            openProgress -= Time.deltaTime / animationDuration;
        }


        openProgress = Mathf.Clamp01(openProgress);

        float curveT = openCurve.Evaluate(openProgress);

        doorPivot.transform.localRotation = Quaternion.Slerp(closeRotation, openRotation, curveT);


        if (isHovering && mouse.leftButton.wasPressedThisFrame)
        {

        }
    }


}
