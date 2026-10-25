using UnityEngine;

[System.Serializable]
public class GameSettings
{
    public float musicVolume = 1f;
    public float masterVolume = 1f;
    public float sfxVolume = 1f;

    public int displayModeIndex = 0;
    public float brightness = 1f;
    public int resolutionIndex = 0;
    public int vSyncIndex = 0;
    public int qualityIndex = 2;

    public float mouseSensitivity = 50f;
    public int invertedYIndex = 0;
}
