using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class MonitorInteractable : MonoBehaviour
{
    [Header("Přiřazení")]
    public Camera uiCamera;
    public RectTransform canvasRect;
    public LayerMask screenLayer;

    [Header("Omezení interakce")]
    public MenuCameraPlacementHandler cameraHandler;
    public MenuCameraPlacementHandler.CameraState allowedState;

    [Header("Debug")]
    public bool debugMode = false;

    private Camera playerCamera;
    private TerminalButton currentButton;
    private Selectable currentSelectable;
    private Collider monitorCollider;

    private string debugPhysics = "Fyzika";
    private string debugUV = "UV:";
    private string debugUI = "UI:";

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
        if (cameraHandler != null && cameraHandler.CurrentState != allowedState)
        {
            ClearHover();
            return;
        }

        if (playerCamera == null || uiCamera == null || canvasRect == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 30f, screenLayer))
        {
            debugPhysics = $"objekt: {hit.collider.name}";

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
                debugUV = $"UV: {uv.x:F3}, {uv.y:F3}";

                Vector2 virtualMousePos = new Vector2(
                    uv.x * uiCamera.pixelWidth,
                    uv.y * uiCamera.pixelHeight
                );

                if (EventSystem.current == null) return;

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = virtualMousePos
                };

                TerminalButton hitButton = null;
                Selectable hitSelectable = null;

                TerminalButton[] allButtons = canvasRect.GetComponentsInChildren<TerminalButton>();

                for (int i = allButtons.Length - 1; i >= 0; i--)
                {
                    TerminalButton btn = allButtons[i];

                    if (!btn.gameObject.activeInHierarchy || !btn.enabled) continue;

                    RectTransform btnRect = btn.GetComponent<RectTransform>();

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(btnRect, virtualMousePos, uiCamera, out Vector2 btnLocalPoint))
                    {
                        if (btnRect.rect.Contains(btnLocalPoint))
                        {
                            hitButton = btn;
                            hitSelectable = btn.GetComponent<Selectable>();
                            debugUI = $"Hit: {btn.gameObject.name}";

                            break;
                        }
                    }
                }
                if (hitButton == null) debugUI = "UI: none";

                if (hitButton != currentButton)
                {
                    if (currentButton != null) currentButton.OnHoverExit();
                    currentButton = hitButton;
                    if (currentButton != null) currentButton.OnHoverEnter();
                }

                if (hitSelectable != currentSelectable)
                {
                    if (currentSelectable != null)
                        ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerExitHandler);

                    currentSelectable = hitSelectable;

                    if (currentSelectable != null)
                        ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
                }
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
        else
        {
            debugPhysics = "Paprsek myši netrefil žádný monitor.";
            debugUV = "UV: ---";
            debugUI = "UI: ---";
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

    private void OnGUI()
    {
        if (!debugMode || (cameraHandler != null && cameraHandler.CurrentState != allowedState)) return;

        float boxWidth = 450f;
        float boxHeight = 110f;
        Rect bgRect = new Rect(Screen.width - boxWidth - 20, 20, boxWidth, boxHeight);

        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.green;

        GUI.Label(new Rect(bgRect.x + 10, bgRect.y + 10, boxWidth, 25), $"--- DEBUG: {gameObject.name} ---", style);

        style.normal.textColor = Color.white;
        GUI.Label(new Rect(bgRect.x + 10, bgRect.y + 35, boxWidth, 25), debugPhysics, style);
        GUI.Label(new Rect(bgRect.x + 10, bgRect.y + 55, boxWidth, 25), debugUV, style);
        GUI.Label(new Rect(bgRect.x + 10, bgRect.y + 75, boxWidth, 25), debugUI, style);
    }
}