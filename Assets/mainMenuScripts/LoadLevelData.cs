using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class Level
{
    public int Id;
    public string Time;
    public string Name;
    public string Difficulty;
    public string Info;
}

[Serializable]
public class Twerker
{
    public Level[] LevelList;
}
public class LoadLevelData : MonoBehaviour
{
    [SerializeField] private TextAsset jsonFile;

    [SerializeField] private LevelGuiData prefab;

    [SerializeField] private Transform contentContainer;

    private Twerker PlayerAr = new Twerker();
    private void Start()
    {
        if (jsonFile == null)
        {
            Debug.Log("kouè");
            return;
        }

        PlayerAr = JsonUtility.FromJson<Twerker>(jsonFile.text);

        if (PlayerAr?.LevelList == null || PlayerAr.LevelList.Length == 0)
        {
            Debug.Log("kouè ELA");
            return;
        }

        foreach (Level level in PlayerAr.LevelList)
        {
            Debug.Log(level.Id);
            LevelGuiData newUI = Instantiate(prefab, contentContainer, false);
            newUI.SetData(level);
        }
    }
}
