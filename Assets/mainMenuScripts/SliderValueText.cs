using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class SliderValueText : MonoBehaviour
{
    [Header("Přiřazení textu")]
    public TextMeshProUGUI valueText;

    [Header("Formát zobrazení")]
    [Tooltip("Pokud je zaškrtnuto, ukazuje vždy 0 až 100 bez ohledu na Min/Max nastavení slideru.")]
    public bool showAsPercentage = true;
    public string suffix = "%"; 

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        slider.onValueChanged.AddListener(UpdateText);
    }

    void Start()
    {
        UpdateText(slider.value);
    }

    private void UpdateText(float val)
    {
        if (valueText == null) return;

        if (showAsPercentage)
        {
            int percent = Mathf.RoundToInt(slider.normalizedValue * 100f);
            valueText.text = percent.ToString() + suffix;
        }
        else
        {
            valueText.text = Mathf.RoundToInt(val).ToString() + suffix;
        }
    }

    void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdateText);
        }
    }
}