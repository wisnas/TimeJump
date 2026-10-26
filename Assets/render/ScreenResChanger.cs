using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenResChanger : MonoBehaviour
{
    public Material targetMaterial;

    public string SceneName = "MainMenu";

    public float MenuSceneFloat = 2.0f;
    public float GameSceneFloat = 3.0f;

    public string parameterName = "_PixelSize";
    void Start()
    {
        if (targetMaterial != null)
        {
            if (SceneManager.GetActiveScene().name == SceneName)
            {
                targetMaterial.SetFloat(parameterName, MenuSceneFloat);
            }
            else
            {
                targetMaterial.SetFloat(parameterName, GameSceneFloat);
            }
        }
    }
}
