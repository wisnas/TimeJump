using TMPro;
using UnityEngine;

public class LevelDetailPanel : MonoBehaviour
{
    public int Id;
    public string Time;
    public string Name;
    public string Difficulty;
    public string Info;

    [Header("Texty na prefabu karty")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI infoText;

    private void OnDisable()
    {
        
    }

    public void ShowData(Level level)
    {
        gameObject.SetActive(true);

        Id = level.Id;
        Time = level.Time;
        Name = level.Name;
        Difficulty = level.Difficulty;
        Info = level.Info;

        nameText.text = $"{Id}.{Name}";
        timeText.text = $"TIME: {Time}";
        // pridat difftext
        infoText.text = Info; 
    }
}
