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

    private GameObject draggingObject;
    private Slider draggingSlider;
    private Scrollbar draggingScrollbar; 

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

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (draggingObject != null && EventSystem.current != null)
            {
                PointerEventData releaseData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(draggingObject, releaseData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(draggingObject, releaseData, ExecuteEvents.endDragHandler);
            }
            draggingObject = null;
            draggingSlider = null;
            draggingScrollbar = null;
        }

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

                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    ScrollRect[] scrollRects = canvasRect.GetComponentsInChildren<ScrollRect>();
                    foreach (ScrollRect sr in scrollRects)
                    {
                        if (!sr.gameObject.activeInHierarchy || !sr.enabled) continue;

                        RectTransform checkRect = sr.viewport != null ? sr.viewport : sr.GetComponent<RectTransform>();
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(checkRect, virtualMousePos, uiCamera, out Vector2 localPt))
                        {
                            if (checkRect.rect.Contains(localPt))
                            {
                                pointerData.scrollDelta = new Vector2(0f, Mathf.Sign(scrollY) * 4f); 
                                ExecuteEvents.ExecuteHierarchy(sr.gameObject, pointerData, ExecuteEvents.scrollHandler);
                                break;
                            }
                        }
                    }
                }

                TerminalButton hitButton = null;
                Selectable hitSelectable = null;

                Selectable[] allSelectables = canvasRect.GetComponentsInChildren<Selectable>();
                for (int i = allSelectables.Length - 1; i >= 0; i--)
                {
                    Selectable sel = allSelectables[i];
                    if (!sel.gameObject.activeInHierarchy || !sel.interactable) continue;

                    if (IsPointOverSelectable(sel, virtualMousePos))
                    {
                        hitSelectable = sel;
                        hitButton = sel.GetComponent<TerminalButton>();
                        debugUI = $"Hit: {sel.gameObject.name}";
                        break;
                    }
                }

                if (hitSelectable == null && hitButton == null)
                {
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
                }

                if (hitButton == null && hitSelectable == null) debugUI = "UI: none";

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
                        draggingObject = currentSelectable.gameObject;

                        if (currentSelectable is Slider slider)
                        {
                            draggingSlider = slider;
                            UpdateSliderValue(draggingSlider, virtualMousePos);
                        }
                        else if (currentSelectable is Scrollbar scrollbar)
                        {
                            draggingScrollbar = scrollbar;
                            UpdateScrollbarValue(draggingScrollbar, virtualMousePos);
                        }
                        else
                        {
                            ExecuteEvents.Execute(draggingObject, pointerData, ExecuteEvents.pointerDownHandler);
                            ExecuteEvents.Execute(draggingObject, pointerData, ExecuteEvents.initializePotentialDrag);
                            ExecuteEvents.Execute(draggingObject, pointerData, ExecuteEvents.beginDragHandler);
                            ExecuteEvents.Execute(draggingObject, pointerData, ExecuteEvents.pointerClickHandler);
                        }
                    }
                }

                if (Mouse.current.leftButton.isPressed)
                {
                    if (draggingSlider != null)
                    {
                        UpdateSliderValue(draggingSlider, virtualMousePos);
                    }
                    else if (draggingScrollbar != null)
                    {
                        UpdateScrollbarValue(draggingScrollbar, virtualMousePos);
                    }
                    else if (draggingObject != null)
                    {
                        ExecuteEvents.Execute(draggingObject, pointerData, ExecuteEvents.dragHandler);
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

    private bool IsPointOverSelectable(Selectable sel, Vector2 screenPos)
    {
        RectTransform rect = sel.GetComponent<RectTransform>();
        if (rect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, uiCamera, out Vector2 uiLocalPoint))
        {
            if (rect.rect.Contains(uiLocalPoint)) return true;
        }

        if (sel is Slider slider)
        {
            if (slider.handleRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(slider.handleRect, screenPos, uiCamera, out Vector2 handleLocal))
            {
                if (slider.handleRect.rect.Contains(handleLocal)) return true;
            }
            if (slider.fillRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(slider.fillRect, screenPos, uiCamera, out Vector2 fillLocal))
            {
                if (slider.fillRect.rect.Contains(fillLocal)) return true;
            }
        }
        else if (sel is Scrollbar scrollbar)
        {
            if (scrollbar.handleRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollbar.handleRect, screenPos, uiCamera, out Vector2 handleLocal))
            {
                if (scrollbar.handleRect.rect.Contains(handleLocal)) return true;
            }
        }

        return false;
    }

    private void UpdateSliderValue(Slider slider, Vector2 screenPos)
    {
        RectTransform clickRect = null;
        if (slider.handleRect != null && slider.handleRect.parent != null)
            clickRect = slider.handleRect.parent as RectTransform;
        else if (slider.fillRect != null && slider.fillRect.parent != null)
            clickRect = slider.fillRect.parent as RectTransform;
        else
            clickRect = slider.GetComponent<RectTransform>();

        if (clickRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, screenPos, uiCamera, out Vector2 localCursor))
        {
            Rect rect = clickRect.rect;
            float normalizedValue;

            if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
            {
                normalizedValue = Mathf.InverseLerp(rect.xMin, rect.xMax, localCursor.x);
                if (slider.direction == Slider.Direction.RightToLeft)
                    normalizedValue = 1f - normalizedValue;
            }
            else
            {
                normalizedValue = Mathf.InverseLerp(rect.yMin, rect.yMax, localCursor.y);
                if (slider.direction == Slider.Direction.TopToBottom)
                    normalizedValue = 1f - normalizedValue;
            }

            slider.normalizedValue = Mathf.Clamp01(normalizedValue);
        }
    }

    private void UpdateScrollbarValue(Scrollbar scrollbar, Vector2 screenPos)
    {
        RectTransform clickRect = null;
        if (scrollbar.handleRect != null && scrollbar.handleRect.parent != null)
            clickRect = scrollbar.handleRect.parent as RectTransform;
        else
            clickRect = scrollbar.GetComponent<RectTransform>();

        if (clickRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, screenPos, uiCamera, out Vector2 localCursor))
        {
            Rect rect = clickRect.rect;
            float normalizedValue;

            if (scrollbar.direction == Scrollbar.Direction.LeftToRight || scrollbar.direction == Scrollbar.Direction.RightToLeft)
            {
                normalizedValue = Mathf.InverseLerp(rect.xMin, rect.xMax, localCursor.x);
                if (scrollbar.direction == Scrollbar.Direction.RightToLeft)
                    normalizedValue = 1f - normalizedValue;
            }
            else
            {
                normalizedValue = Mathf.InverseLerp(rect.yMin, rect.yMax, localCursor.y);
                if (scrollbar.direction == Scrollbar.Direction.TopToBottom)
                    normalizedValue = 1f - normalizedValue;
            }

            scrollbar.value = Mathf.Clamp01(normalizedValue);
        }
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
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(currentSelectable.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
            }
            currentSelectable = null;
        }
    }

    void OnDisable()
    {
        ClearHover();
        draggingObject = null;
        draggingSlider = null;
        draggingScrollbar = null;
    }

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