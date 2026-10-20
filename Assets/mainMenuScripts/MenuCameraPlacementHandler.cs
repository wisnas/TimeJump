using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("Placement Targets")]
    [SerializeField] private Vector3 awayPos;
    [SerializeField] private Vector3 awayRot;
    [SerializeField] private Vector3 monitor1Pos;
    [SerializeField] private Vector3 monitor1Rot;
    [SerializeField] private Vector3 monitor2Pos;
    [SerializeField] private Vector3 monitor2Rot;
    [SerializeField] private Vector3 doorPos;
    [SerializeField] private Vector3 doorRot;

    [Header("Monitor Click")]
    [SerializeField] private Collider monitor1Collider;
    [SerializeField] private Collider monitor2Collider;

    [Header("Bars")]
    [SerializeField] private Button LeftBar;
    [SerializeField] private Button RightBar;
    [SerializeField] private Button BottomBar;

    [SerializeField] private float fadeDuration = 0.25f;
    private readonly Dictionary<Button, Coroutine> activeFades = new Dictionary<Button, Coroutine>();

    public enum CameraState
    {
        Monitor1,
        Monitor2,
        Doors,
        Away
    }

    public CameraState CurrentState { get; private set; }
    private Coroutine transitionCoroutine;

    public bool CanSwitch => transitionCoroutine == null;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        CurrentState = CameraState.Away;
        targetCamera.transform.position = awayPos;
        targetCamera.transform.rotation = Quaternion.Euler(awayRot);
        targetCamera.fieldOfView = cameraZoomOut;

        ApplyBarVisibility(CurrentState);
    }
    private void Update()
    {
        if (CurrentState != CameraState.Away || !CanSwitch) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        bool isHovering = false;
        bool isHovering2 = false;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider == monitor1Collider)
            {
                isHovering = true;
            }
            if (hit.collider == monitor2Collider)
            {
                isHovering2 = true;
            }
        }
        

        if (isHovering && mouse.leftButton.wasPressedThisFrame)
        {
            SwitchState(CameraState.Monitor1);
        }

        if (isHovering2 && mouse.leftButton.wasPressedThisFrame)
        {
            SwitchState(CameraState.Monitor2);
        }
    }

    public void SwitchState(CameraState newState)
    {
        if (!CanSwitch) return;

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

        ApplyBarVisibility(newState);

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(CameraTransition(targetPos, Quaternion.Euler(targetRotEuler), targetFov));
    }

    private void ApplyBarVisibility(CameraState state)
    {
        if (LeftBar != null) LeftBar.onClick.RemoveAllListeners();
        if (RightBar != null) RightBar.onClick.RemoveAllListeners();
        if (BottomBar != null) BottomBar.onClick.RemoveAllListeners();

        switch (state)
        {
            case CameraState.Monitor1:
                Show(LeftBar);
                LeftBar.onClick.AddListener(() => SwitchState(CameraState.Monitor2));

                Show(RightBar);
                RightBar.onClick.AddListener(() => SwitchState(CameraState.Doors));

                Show(BottomBar);
                BottomBar.onClick.AddListener(() => SwitchState(CameraState.Away));
                break;

            case CameraState.Monitor2:
                Hide(LeftBar);

                Show(RightBar);
                RightBar.onClick.AddListener(() => SwitchState(CameraState.Monitor1));

                Show(BottomBar);
                BottomBar.onClick.AddListener(() => SwitchState(CameraState.Away));
                break;

            case CameraState.Doors:
                Show(LeftBar);
                LeftBar.onClick.AddListener(() => SwitchState(CameraState.Monitor1));

                Hide(RightBar);

                Show(BottomBar);
                BottomBar.onClick.AddListener(() => SwitchState(CameraState.Away));
                break;

            case CameraState.Away:
            default:
                Hide(LeftBar);
                Hide(RightBar);
                Hide(BottomBar);
                break;
        }
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

    public void Hide(Button bar)
    {
        if (bar == null) return;
        bar.interactable = false;
        FadeTo(bar, 0f);
    }

    public void Show(Button bar)
    {
        if (bar == null) return;
        bar.interactable = true;
        FadeTo(bar, 166f / 255f);
    }

    private void FadeTo(Button bar, float targetAlpha)
    {
        if (activeFades.TryGetValue(bar, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        activeFades[bar] = StartCoroutine(FadeRoutine(bar, targetAlpha, fadeDuration));
    }

    private IEnumerator FadeRoutine(Button bar, float targetAlpha, float duration)
    {
        Image img = bar.image;
        if (img == null) yield break;

        Color startColor = img.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            img.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }

        img.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        activeFades[bar] = null;
    }
}