using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WordData
{
    public List<string> tr;
    public string en;
}

[System.Serializable]
public class WordDatabase
{
    public string level;
    public List<WordData> words;
}

public class FastTypingManager : MonoBehaviour
{
    [Header("Ortak Paneller")]
    public GameObject pnlPause;
    public RectTransform pnlPauseWindow;
    public float pauseAnimDuration = 0.3f;

    [Header("Sonsuz Mod (Infinity) Panelleri")]
    public GameObject pnlDeath_Infinity;

    [Header("Level Modu Panelleri")]
    public GameObject pnlWin_Level;
    public GameObject pnlDeath_Level;

    [Header("Top UI & Can Sistemi")]
    public TextMeshProUGUI txtScore;
    public GameObject[] heartImages;

    [Header("Oyun Sonu Skor UI (Sonsuz Mod)")]
    public TextMeshProUGUI txtDeathScore;
    public TextMeshProUGUI txtHighScore;

    [Header("Oyun Ici Mantik")]
    public int currentLives = 3;
    public int correctWordsCount = 0;
    private float currentScore = 0;
    private bool isGameActive = false;

    // YENI: Level bitirme kelime hedefi (Diger oyunlarda 5 idi, burada 10 kelime yapmistik)
    private int levelTargetCount = 10;

    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public Image imgArrow;

    [Header("Oyun Ici Kontroller (Input)")]
    public TMP_InputField wordInputField;
    public GameObject confirmButton;

    [Header("Kelime Uretici (Spawner) Ayarlari")]
    public GameObject wordPrefab;
    public RectTransform spawnContainer;
    public float spawnInterval = 2.5f;
    public float fallSpeed = 150f;
    public int basePointPerWord = 10;

    [Header("Rastgelelik (RNG) Ayarlari")]
    [Range(0f, 100f)]
    public float heartSpawnChance = 10f;
    private Queue<int> recentSpawnedIndices = new Queue<int>();
    private int cooldownLength = 5;

    private List<FallingWord> activeWords = new List<FallingWord>();
    private float dynamicBottomLimitY;

    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    private WordDatabase currentWordDatabase;

    void Start()
    {
        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlWin_Level.SetActive(false);
        pnlDeath_Level.SetActive(false);

        txtLanguageLevel.text = GameSessionData.SelectedLevel;
        txtDeathScore.color = GameSessionData.SelectedLevelTextColor;
        imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        if (wordInputField != null) wordInputField.gameObject.SetActive(true);
        if (confirmButton != null) confirmButton.SetActive(true);

        StartCoroutine(InitializeGameRoutine());
    }

    IEnumerator InitializeGameRoutine()
    {
        yield return new WaitForEndOfFrame();

        CalculateDynamicLimit();
        LoadJsonData();

        if (GameSessionData.IsInfinityMode)
        {
            SetupInfinityMode();
        }
        else
        {
            SetupLevelMode();
        }
    }

    void CalculateDynamicLimit()
    {
        if (wordInputField != null && spawnContainer != null)
        {
            Vector3[] corners = new Vector3[4];
            wordInputField.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector3 localTopEdge = spawnContainer.InverseTransformPoint(corners[1]);
            dynamicBottomLimitY = localTopEdge.y;
        }
    }

    void LoadJsonData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(GameSessionData.TargetJsonFile);
        if (jsonFile != null)
        {
            currentWordDatabase = JsonUtility.FromJson<WordDatabase>(jsonFile.text);
        }
    }

    void SetupInfinityMode()
    {
        currentLives = 3;
        currentScore = 0;
        correctWordsCount = 0;
        isGameActive = true;
        recentSpawnedIndices.Clear();

        UpdateHealthUI();
        UpdateScoreUI();

        StartCoroutine(SpawnRoutine());
    }

    void SetupLevelMode()
    {
        currentLives = 3;
        correctWordsCount = 0;
        isGameActive = true;
        recentSpawnedIndices.Clear();

        UpdateHealthUI();

        // DEGISTIRILDI: txtScore kapatilmiyor, aksine bastan 0/10 yazdiriliyor
        if (txtScore != null)
            txtScore.gameObject.SetActive(true);

        UpdateScoreUI();

        StartCoroutine(SpawnRoutine());
    }

    // --- SPAWNER SISTEMI ---

    IEnumerator SpawnRoutine()
    {
        while (isGameActive)
        {
            SpawnSingleWord();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnSingleWord()
    {
        if (currentWordDatabase.words.Count == 0) return;

        WordData selectedWord = null;
        int selectedIndex = -1;

        float randomChance = Random.Range(0f, 100f);
        if (randomChance <= heartSpawnChance)
        {
            for (int i = 0; i < currentWordDatabase.words.Count; i++)
            {
                if (currentWordDatabase.words[i].en.ToLower() == "heart")
                {
                    selectedIndex = i;
                    selectedWord = currentWordDatabase.words[i];
                    break;
                }
            }
        }

        if (selectedWord == null)
        {
            int attempts = 0;
            do
            {
                selectedIndex = Random.Range(0, currentWordDatabase.words.Count);
                attempts++;
            }
            while (recentSpawnedIndices.Contains(selectedIndex) && attempts < 50);

            selectedWord = currentWordDatabase.words[selectedIndex];
        }

        if (selectedIndex != -1)
        {
            recentSpawnedIndices.Enqueue(selectedIndex);

            if (recentSpawnedIndices.Count > cooldownLength)
            {
                recentSpawnedIndices.Dequeue();
            }
        }

        GameObject newObj = Instantiate(wordPrefab, spawnContainer);
        float randomX = Random.Range(-200f, 200f);
        RectTransform rt = newObj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(randomX, 0);

        FallingWord fallingScript = newObj.GetComponent<FallingWord>();
        fallingScript.Initialize(selectedWord.en, selectedWord.tr, fallSpeed, dynamicBottomLimitY, this);

        activeWords.Add(fallingScript);
    }

    // --- OYUN ICI MEKANIKLER ---

    public void Button_ConfirmInput()
    {
        if (wordInputField != null && isGameActive)
        {
            string playerInput = wordInputField.text.Trim().ToLower();
            bool isCorrect = false;

            for (int i = 0; i < activeWords.Count; i++)
            {
                foreach (string meaning in activeWords[i].targetTurkishWords)
                {
                    if (meaning.ToLower() == playerInput)
                    {
                        isCorrect = true;
                        break;
                    }
                }

                if (isCorrect)
                {
                    if (activeWords[i].txtEnglishWord.text.ToLower() == "heart")
                    {
                        if (currentLives < heartImages.Length)
                        {
                            currentLives++;
                            UpdateHealthUI();
                        }
                    }

                    Destroy(activeWords[i].gameObject);
                    activeWords.RemoveAt(i);
                    OnWordTypedCorrectly(basePointPerWord);
                    break;
                }
            }

            wordInputField.text = "";
            wordInputField.ActivateInputField();
        }
    }

    public void OnWordTypedCorrectly(int basePoint)
    {
        correctWordsCount++;

        if (correctWordsCount % 15 == 0)
        {
            fallSpeed += 10f;
        }

        if (GameSessionData.IsInfinityMode)
        {
            currentScore += basePoint;
        }

        // YENI: Puan veya sayac arttiktan sonra iki modda da UI guncellenir
        UpdateScoreUI();

        if (!GameSessionData.IsInfinityMode)
        {
            if (correctWordsCount >= levelTargetCount)
            {
                LevelComplete();
            }
        }
    }

    public void OnWordMissed(FallingWord missedWord)
    {
        if (!isGameActive) return;

        activeWords.Remove(missedWord);
        OnMistakeMade();
    }

    public void OnMistakeMade()
    {
        currentLives--;
        UpdateHealthUI();

        if (currentLives <= 0)
        {
            if (GameSessionData.IsInfinityMode)
            {
                GameOverInfinity();
            }
            else
            {
                GameOverLevel();
            }
        }
    }

    // --- UI GUNCELLEMELERI ---

    void UpdateScoreUI()
    {
        // DEGISTIRILDI: Sadece sonsuz mod degil, Level modunda da "X/10" seklinde gosterilir
        if (txtScore != null)
        {
            if (GameSessionData.IsInfinityMode)
            {
                txtScore.text = Mathf.RoundToInt(currentScore).ToString();
            }
            else
            {
                txtScore.text = correctWordsCount.ToString() + "/" + levelTargetCount.ToString();
            }
        }
    }

    void UpdateHealthUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < currentLives)
                heartImages[i].SetActive(true);
            else
                heartImages[i].SetActive(false);
        }
    }

    void InputUIActivationSettings(bool isActive)
    {
        if (wordInputField != null) wordInputField.gameObject.SetActive(isActive);
        if (confirmButton != null) confirmButton.SetActive(isActive);
        if (imgArrow != null) imgArrow.gameObject.SetActive(isActive);
    }

    // --- PANEL TETIKLEYICILERI ---

    void GameOverInfinity()
    {
        isGameActive = false;
        Time.timeScale = 0;
        pnlDeath_Infinity.SetActive(true);
        InputUIActivationSettings(false);

        float multiplier = levelMultipliers[GameSessionData.SelectedLevel];
        int finalScore = Mathf.RoundToInt(currentScore * multiplier);

        if (txtDeathScore != null) txtDeathScore.text = finalScore.ToString();

        int highScore = PlayerPrefs.GetInt("HighScore_FastTyping_" + GameSessionData.SelectedLevel, 0);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore_FastTyping_" + GameSessionData.SelectedLevel, highScore);
        }
        if (txtHighScore != null) txtHighScore.text = highScore.ToString();
    }

    void GameOverLevel()
    {
        isGameActive = false;
        Time.timeScale = 0;
        pnlDeath_Level.SetActive(true);
        InputUIActivationSettings(false);
    }

    void LevelComplete()
    {
        isGameActive = false;
        Time.timeScale = 0;

        if (!GameSessionData.IsInfinityMode)
        {
            string saveKey = $"Completed_{GameSessionData.SelectedLevel}_{GameSessionData.CurrentUnitIndex}_{GameSessionData.CurrentLevelIndex}";
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        pnlWin_Level.SetActive(true);
        InputUIActivationSettings(false);
    }

    // --- BUTON METOTLARI (ON CLICK) ---

    public void Button_NextLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        int nextIndex = GameSessionData.CurrentLevelIndex + 1;

        if (nextIndex < GameSessionData.CurrentUnitLevels.Count)
        {
            GameSessionData.CurrentLevelIndex = nextIndex;

            LevelConfig nextLevelConfig = GameSessionData.CurrentUnitLevels[nextIndex];
            GameSessionData.TargetJsonFile = nextLevelConfig.jsonFileName;

            Time.timeScale = 1;
            SceneController.Instance.LoadScene(nextLevelConfig.sceneName);
        }
        else
        {
            PlayerPrefs.SetString("AutoOpenChapter", GameSessionData.SelectedLevel);

            Time.timeScale = 1;
            SceneController.Instance.LoadScene("LevelsMenu");
        }
    }

    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        isGameActive = false;

        // 1. Paneli aktif et (Arka plan kararmasi gorunur olur)
        pnlPause.SetActive(true);
        Time.timeScale = 0; // Zamani durdur

        InputUIActivationSettings(false);
        if (wordInputField != null) wordInputField.DeactivateInputField();

        // 2. Animasyon Baslangici: Paneli ekranin sol ust kosesine, kucultulmus bir sekilde koy
        pnlPauseWindow.localPosition = new Vector3(-800f, 1500f, 0f);
        pnlPauseWindow.localScale = Vector3.zero;

        // Olasi onceki tweenleri iptal et
        pnlPauseWindow.DOKill();

        // 3. Orta noktaya (0,0,0) getirirken scale'i 1 yap. SetUpdate(true) ile Time.timeScale=0'i yoksay
        pnlPauseWindow.DOLocalMove(Vector3.zero, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Button_ResumeGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        // Olasi onceki tweenleri iptal et
        pnlPauseWindow.DOKill();

        // 1. Paneli orta noktadan tekrar sol ust koseye ve 0 boyutuna dogru yolla
        pnlPauseWindow.DOLocalMove(new Vector3(-800f, 1500f, 0f), pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.zero, pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            // 2. Animasyon tamamen bittiginde paneli kapat ve oyunu baslat
            pnlPause.SetActive(false);
            isGameActive = true;
            Time.timeScale = 1;
            InputUIActivationSettings(true);
        });
    }

    public void Button_RetryGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        Time.timeScale = 1;
        SceneController.Instance.LoadScene("FastTyping");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        Time.timeScale = 1;

        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);
    }
}