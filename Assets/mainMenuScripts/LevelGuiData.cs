using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LevelGuiData : MonoBehaviour
{
    [Header("Texty na prefabu karty")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    private Level currentLevel;
    private LevelDetailPanel panel;

    public int Id;
    public string Time;
    public string Name;
    public string Difficulty;
    public string Info;
    private void Awake()
    {
        panel = FindFirstObjectByType<LevelDetailPanel>(FindObjectsInactive.Include);
    }
    public void SetData(Level level)
    {
        currentLevel = level;

        Id = level.Id;
        Time = level.Time;
        Name = level.Name;
        Difficulty = level.Difficulty;
        Info = level.Info;

        if (nameText != null) nameText.text = $"{Id}.{Name}";
        if (timeText != null) timeText.text = $"TIME: {Time}";
    }
    public void OnLevelClicked()
    {
        if (panel != null && currentLevel != null)
        {
            panel.transform.SetAsLastSibling();

            panel.ShowData(currentLevel);
        }
    }


}
