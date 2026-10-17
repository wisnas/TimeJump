using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCameraPlacementHandler : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera;

    [Header("Zoom / FOV Settings")]
    [SerializeField] private float cameraZoomOut = 75f;
    [SerializeField] private float cameraZoomIn = 50f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Input")]
    [SerializeField] private InputActionReference menuTestAction;

    [Header("Placement Targets")]
    [SerializeField] private Vector3 awayPos;
    [SerializeField] private Vector3 awayRot;
    [SerializeField] private Vector3 monitor1Pos;
    [SerializeField] private Vector3 monitor1Rot;
    [SerializeField] private Vector3 monitor2Pos;
    [SerializeField] private Vector3 monitor2Rot;
    [SerializeField] private Vector3 doorPos;
    [SerializeField] private Vector3 doorRot;

    private bool canSwitch;
    public enum CameraState
    {
        Monitor1,
        Monitor2,
        Doors,
        Away
    }

    public CameraState CurrentState { get; private set; }
    private Coroutine transitionCoroutine;

    private void Awake()
    {

        canSwitch = true;
        if (targetCamera == null)
            targetCamera = Camera.main;


        CurrentState = CameraState.Away;
        targetCamera.transform.position = awayPos;
        targetCamera.transform.rotation = Quaternion.Euler(awayRot);
        targetCamera.fieldOfView = cameraZoomOut;
    }

    private void Update()
    {
        if (targetCamera.transform.position == awayPos 
            || targetCamera.transform.position == monitor1Pos
            || targetCamera.transform.position == monitor2Pos
            || targetCamera.transform.position == doorPos)
        {
            canSwitch = true;
        }
        else
        {
            canSwitch = false;
        }
        var keyboard = Keyboard.current;
        if (canSwitch == true)
        {
            if (keyboard.numpad1Key.wasPressedThisFrame)
            {
                SwitchState(CameraState.Monitor1);
                Debug.Log("1");
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                SwitchState(CameraState.Monitor2);
                Debug.Log("2");
            }
            else if (keyboard.numpad3Key.wasPressedThisFrame)
            {
                SwitchState(CameraState.Doors);
                Debug.Log("3");
            }
            else if (keyboard.numpad4Key.wasPressedThisFrame)
            {
                SwitchState(CameraState.Away);
                Debug.Log("4");
            }
        }
       
    }

    public void SwitchState(CameraState newState)
    {
        CurrentState = newState;

        Vector3 targetPos;
        Vector3 targetRotEuler;
        float targetFov;

        switch (newState)
        {
            case CameraState.Monitor1:
                targetPos = monitor1Pos;
                targetRotEuler = monitor1Rot;
                targetFov = cameraZoomIn;
                break;

            case CameraState.Monitor2:
                targetPos = monitor2Pos;
                targetRotEuler = monitor2Rot;
                targetFov = cameraZoomIn;
                break;

            case CameraState.Doors:
                targetPos = doorPos;
                targetRotEuler = doorRot;
                targetFov = cameraZoomIn;
                break;

            case CameraState.Away:
            default:
                targetPos = awayPos;
                targetRotEuler = awayRot;
                targetFov = cameraZoomOut;
                break;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(CameraTransition(targetPos, Quaternion.Euler(targetRotEuler), targetFov));
    }

    private IEnumerator CameraTransition(Vector3 targetPos, Quaternion targetRot, float targetFov)
    {
        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;
        float startFov = targetCamera.fieldOfView;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float curveT = transitionCurve.Evaluate(t);

            targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, curveT);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, curveT);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, curveT);

            yield return null;
        }

        targetCamera.transform.position = targetPos;
        targetCamera.transform.rotation = targetRot;
        targetCamera.fieldOfView = targetFov;

        transitionCoroutine = null;
    }

   
}