using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening; // DOTween kütüphanesini dahil ediyoruz

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [HideInInspector] public string sceneNameBeforeNewSceneLoad = "MainMenu";

    [Header("Transition Settings")]
    public float animDuration = 0.4f;
    public Ease scaleDownEase = Ease.InBack;
    public Ease scaleUpEase = Ease.OutBack;

    private bool isTransitioning = false; // Cift tiklamalari onlemek icin bir flag

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;

        string targetScene = string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.Contains("Menu"))
        {
            sceneNameBeforeNewSceneLoad = currentScene;
        }

        GameObject mainPanelObj = GameObject.FindGameObjectWithTag("UIPanel");

        if (mainPanelObj != null)
        {
            isTransitioning = true;
            RectTransform panelRect = mainPanelObj.GetComponent<RectTransform>();

            panelRect.DOKill(); // Önceki animasyonlarý durdur

            // Animasyon
            panelRect.DOScale(0.05f, animDuration).SetEase(scaleDownEase).SetUpdate(true).OnComplete(() =>
            {
                // Animasyon bitince yeni sahneyi yukle
                panelRect.DOKill(); // Önceki animasyonlarý durdur
                SceneManager.LoadScene(targetScene);
            });
        }
        else
        {
            // Tag'li panel yoksa direkt sahneyi yukle
            SceneManager.LoadScene(targetScene);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isTransitioning = false;

        GameObject mainPanelObj = GameObject.FindGameObjectWithTag("UIPanel");

        if (mainPanelObj != null)
        {
            RectTransform panelRect = mainPanelObj.GetComponent<RectTransform>();

            panelRect.localScale = Vector3.one * 0.05f;

            panelRect.DOScale(1f, animDuration).SetEase(scaleUpEase).SetUpdate(true);
        }
    }
}