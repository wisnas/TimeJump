using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Image))]
public class TerminalButton : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;
    public Sprite activeSprite;

    [Header("Text Colors")]
    public Color textHoverColor = Color.black;
    public Color textActiveColor = Color.white;

    [Header("Active State Settings")]
    public bool isActive = false;
    public float activeWidthOffset = 80f;

    [Header("Menu Connection")]
    public GameObject targetPanel;

    public TerminalButton parentScript;

    [Header("Event")]
    public UnityEvent onClick;

    private Image targetImage;
    private TextMeshProUGUI[] buttonTexts;
    private Color[] normalTextColors;
    private RectTransform rectTransform;

    private float defaultWidth;
    private Vector2 defaultAnchoredPos;

    private void OnDisable()
    {
        if (isActive)
        {
            SetActiveState(false);

            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }
        }
    }

    void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        defaultWidth = rectTransform.sizeDelta.x;
        defaultAnchoredPos = rectTransform.anchoredPosition;

        buttonTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        normalTextColors = new Color[buttonTexts.Length];

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            normalTextColors[i] = buttonTexts[i].color;
        }

        if (normalSprite == null && targetImage.sprite != null)
        {
            normalSprite = targetImage.sprite;
        }

        ApplyStateVisuals();
    }

    public void SetActiveState(bool active)
    {
        isActive = active;
        ApplyStateVisuals();
    }

    public void ToggleActiveState()
    {
        SetActiveState(!isActive);
    }

    private void ApplyStateVisuals()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(isActive);
        }

        if (isActive)
        {
            rectTransform.sizeDelta = new Vector2(defaultWidth + activeWidthOffset, rectTransform.sizeDelta.y);
            rectTransform.anchoredPosition = new Vector2(defaultAnchoredPos.x + (activeWidthOffset * 0.5f), defaultAnchoredPos.y);

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                if (buttonTexts[i] != null) buttonTexts[i].color = textActiveColor;
            }

            if (targetImage != null && activeSprite != null) targetImage.sprite = activeSprite;
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(defaultWidth, rectTransform.sizeDelta.y);
            rectTransform.anchoredPosition = defaultAnchoredPos;

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                if (buttonTexts[i] != null) buttonTexts[i].color = normalTextColors[i];
            }

            if (targetImage != null && normalSprite != null) targetImage.sprite = normalSprite;
        }
    }

    public void OnHoverEnter()
    {
        if (isActive) return;

        if (highlightedSprite != null && targetImage != null)
            targetImage.sprite = highlightedSprite;

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            if (buttonTexts[i] != null) buttonTexts[i].color = textHoverColor;
        }
    }

    public void OnHoverExit()
    {
        if (isActive) return;

        if (normalSprite != null && targetImage != null)
            targetImage.sprite = normalSprite;

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            if (buttonTexts[i] != null) buttonTexts[i].color = normalTextColors[i];
        }
    }

    public void OnClick()
    {
        ToggleActiveState();
        onClick?.Invoke();
    }
}