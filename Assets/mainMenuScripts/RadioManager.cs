using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    private TerminalButton[] buttonList;

    private void Awake()
    {
        buttonList = GetComponentsInChildren<TerminalButton>(true);

        foreach (TerminalButton btn in buttonList)
        {
            btn.onClick.AddListener(() => OnAnyButtonClicked(btn));
        }
    }

    private void OnAnyButtonClicked(TerminalButton clickedButton)
    {
        foreach (TerminalButton btn in buttonList)
        {
            if (btn != clickedButton && btn.isActive)
            {
                btn.SetActiveState(false);
            }
        }
    }

}
