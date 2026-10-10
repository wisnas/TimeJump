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
    public Color textHoverColor = Color.white;
    public Color textActiveColor = Color.green;

    [Header("Active State Settings")]
    public bool isActive = false;
    public float activeWidthOffset = 80f; // Rozšíření o 80 px

    [Header("Event")]
    public UnityEvent onClick;

    private Image targetImage;
    private TextMeshProUGUI buttonText;
    private Color normalTextColor;
    private RectTransform rectTransform;

    // Uložené výchozí hodnoty
    private float defaultWidth;
    private Vector2 defaultAnchoredPos;

    void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        defaultWidth = rectTransform.sizeDelta.x;
        defaultAnchoredPos = rectTransform.anchoredPosition;

        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (buttonText != null)
        {
            normalTextColor = buttonText.color;
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
        if (isActive)
        {
            // 1. Zvětšíme šířku o 80 px
            rectTransform.sizeDelta = new Vector2(defaultWidth + activeWidthOffset, rectTransform.sizeDelta.y);

            // 2. Posuneme celé tlačítko doprava o polovinu offsetu (80 / 2 = 40 px)
            // Tím levá strana a text zůstanou na milimetr přesně na původním místě!
            rectTransform.anchoredPosition = new Vector2(defaultAnchoredPos.x + (activeWidthOffset * 0.5f), defaultAnchoredPos.y);

            if (buttonText != null) buttonText.color = textActiveColor;
            if (targetImage != null && activeSprite != null) targetImage.sprite = activeSprite;
        }
        else
        {
            // Návrat na původní rozměry a pozici
            rectTransform.sizeDelta = new Vector2(defaultWidth, rectTransform.sizeDelta.y);
            rectTransform.anchoredPosition = defaultAnchoredPos;

            if (buttonText != null) buttonText.color = normalTextColor;
            if (targetImage != null && normalSprite != null) targetImage.sprite = normalSprite;
        }
    }

    public void OnHoverEnter()
    {
        if (isActive) return;

        if (highlightedSprite != null && targetImage != null)
            targetImage.sprite = highlightedSprite;

        if (buttonText != null)
            buttonText.color = textHoverColor;
    }

    public void OnHoverExit()
    {
        if (isActive) return;

        if (normalSprite != null && targetImage != null)
            targetImage.sprite = normalSprite;

        if (buttonText != null)
            buttonText.color = normalTextColor;
    }

    public void OnClick()
    {
        ToggleActiveState();
        onClick?.Invoke();
    }
}