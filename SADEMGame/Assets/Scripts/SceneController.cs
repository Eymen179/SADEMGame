using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [HideInInspector] public string sceneNameBeforeNewSceneLoad = "MainMenu";
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void LoadScene(string sceneName)
    {

        if (string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        else
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene.Contains("Menu"))
            {
                sceneNameBeforeNewSceneLoad = currentScene;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}