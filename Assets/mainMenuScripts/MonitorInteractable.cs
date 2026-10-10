using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class MonitorInteractable : MonoBehaviour
{
    [Header("Přiřazení")]
    public Camera uiCamera;             // Kamera, co kreslí do RT
    public RectTransform canvasRect;    // Canvas tohoto monitoru
    public LayerMask screenLayer;       // Layer monitoru

    [Header("Debug")]
    public bool debugMode = false;

    private Camera playerCamera;
    private TerminalButton currentButton;
    private Selectable currentSelectable;
    private Collider monitorCollider;

    void Awake()
    {
        monitorCollider = GetComponent<Collider>();
    }

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerCamera == null || uiCamera == null || canvasRect == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 30f, screenLayer))
        {
            if (hit.collider == monitorCollider)
            {
                Vector3 localPoint = transform.InverseTransformPoint(hit.point);

                float u = localPoint.x + 0.5f;
                float v = localPoint.y + 0.5f;

                if (Mathf.Abs(localPoint.x) >= 0.49f && Mathf.Abs(localPoint.z) < 0.49f)
                {
                    u = localPoint.z + 0.5f;
                }

                Vector2 uv = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));

                Vector2 virtualMousePos = new Vector2(
                    uv.x * uiCamera.pixelWidth,
                    uv.y * uiCamera.pixelHeight
                );

                if (EventSystem.current == null) return;

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = virtualMousePos
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                TerminalButton hitButton = null;
                Selectable hitSelectable = null;

                if (results.Count > 0)
                {
                    hitButton = results[0].gameObject.GetComponentInParent<TerminalButton>();
                    hitSelectable = results[0].gameObject.GetComponentInParent<Selectable>();
                }

                // 1. Změna HOVER stavu
                if (hitButton != currentButton)
                {
                    if (currentButton != null) currentButton.OnHoverExit();
                    currentButton = hitButton;
                    if (currentButton != null) currentButton.OnHoverEnter();
                }

                // Podpora i pro nativní Unity Button stavy
                if (hitSelectable != currentSelectable)
                {
                    if (currentSelectable != null)
                        ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerExitHandler);

                    currentSelectable = hitSelectable;

                    if (currentSelectable != null)
                        ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
                }

                // 2. KLIKNUTÍ (Active / Inactive toggle)
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (currentButton != null) currentButton.OnClick();
                    if (currentSelectable != null)
                    {
                        ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                    }
                }
                return;
            }
        }

        ClearHover();
    }

    private void ClearHover()
    {
        if (currentButton != null)
        {
            currentButton.OnHoverExit();
            currentButton = null;
        }

        if (currentSelectable != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
            currentSelectable = null;
        }
    }

    void OnDisable() => ClearHover();
}