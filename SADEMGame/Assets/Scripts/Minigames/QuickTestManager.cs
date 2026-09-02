using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickTestManager : MonoBehaviour
{
    public static int sessionScore = 0;
    public static int sessionCorrectCount = 0;
    private int levelTargetCount = 5;

    [Header("Top UI")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtScore;
    public Image imgTime; // DTT Procedural UI (Filled Mod)

    [Header("Ortak Paneller")]
    public GameObject pnlPause;

    [Header("Sonsuz Mod Panelleri")]
    public GameObject pnlDeath_Infinity;
    public TextMeshProUGUI txtDeathScore_Infinity;
    public TextMeshProUGUI txtHighScore_Infinity;

    [Header("Level Modu Panelleri")]
    public GameObject pnlDeath_Level;
    public GameObject pnlWin_Level;
    public TextMeshProUGUI txtDeathGuessCount_Level;

    [Header("Oyun Ici Kontroller")]
    public TextMeshProUGUI txtQuestion; // BtnQuestion icindeki Text
    public Button[] btnAnswers; // 4 adet cevap butonu
    public TextMeshProUGUI[] txtAnswers;
    public Button btnConfirm;

    [Header("Renk Ayarlari")]
    public Color colorDefault = Color.white;
    public Color colorSelected = new Color(0.95f, 0.76f, 0.20f); // Sari
    public Color colorCorrect = new Color(0.33f, 0.55f, 0.27f); // Yesil
    public Color colorWrong = new Color(0.8f, 0.2f, 0.2f); // Kirmizi
    public Color colorCorrectReveal = new Color(0.6f, 0.6f, 0.6f); // Gri

    // --- Zaman Degiskenleri ---
    private float maxTime = 50f;
    private float timeRemaining = 50f;

    // --- Sistem Degiskenleri ---
    private QuickTestDatabase currentDatabase;
    private QuickTestData currentTargetData;

    private string activeForeignLanguage = "tr"; // Su an Turkce testi yapiliyor

    private bool isGameActive = false;
    private bool isAnimating = false;
    private Button currentSelectedOption = null;

    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    void Start()
    {
        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlDeath_Level.SetActive(false);
        pnlWin_Level.SetActive(false);

        if (txtLanguageLevel != null) txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null) txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null) imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        timeRemaining = maxTime;
        UpdateScoreUI();
        LoadJsonData();
        RefreshQuestion();

        isGameActive = true;
    }

    void Update()
    {
        if (isGameActive && !isAnimating)
        {
            timeRemaining -= Time.deltaTime;

            if (imgTime != null)
            {
                imgTime.fillAmount = timeRemaining / maxTime;
            }

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                imgTime.fillAmount = 0;
                GameOver();
            }
        }
    }

    void LoadJsonData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(GameSessionData.TargetJsonFile);
        if (jsonFile != null)
        {
            currentDatabase = JsonUtility.FromJson<QuickTestDatabase>(jsonFile.text);
        }
    }

    void RefreshQuestion()
    {
        currentSelectedOption = null;

        foreach (Button btn in btnAnswers)
        {
            btn.image.color = colorDefault;
        }

        if (currentDatabase == null || currentDatabase.questions.Count == 0) return;

        int targetIndex = Random.Range(0, currentDatabase.questions.Count);
        currentTargetData = currentDatabase.questions[targetIndex];

        txtQuestion.text = GetQuestion(currentTargetData);

        List<string> options = new List<string>();
        options.Add(GetCorrectAnswer(currentTargetData));

        string[] wrongAnswers = GetWrongAnswers(currentTargetData);
        for (int i = 0; i < wrongAnswers.Length; i++)
        {
            options.Add(wrongAnswers[i]);
        }

        // Seçenekleri karistir (Shuffle)
        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        for (int i = 0; i < btnAnswers.Length; i++)
        {
            if (i < options.Count)
            {
                txtAnswers[i].text = options[i];
            }
        }
    }

    string GetQuestion(QuickTestData data)
    {
        switch (activeForeignLanguage)
        {
            case "tr": return data.questionTr;
            case "en": return data.questionEn;
            case "de": return data.questionDe;
            default: return data.questionTr;
        }
    }

    string GetCorrectAnswer(QuickTestData data)
    {
        switch (activeForeignLanguage)
        {
            case "tr": return data.correctAnswerTr;
            case "en": return data.correctAnswerEn;
            case "de": return data.correctAnswerDe;
            default: return data.correctAnswerTr;
        }
    }

    string[] GetWrongAnswers(QuickTestData data)
    {
        switch (activeForeignLanguage)
        {
            case "tr": return data.wrongAnswersTr;
            case "en": return data.wrongAnswersEn;
            case "de": return data.wrongAnswersDe;
            default: return data.wrongAnswersTr;
        }
    }

    public void Button_SelectOption(int buttonIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (!isGameActive || isAnimating) return;

        if (currentSelectedOption != null)
        {
            currentSelectedOption.image.color = colorDefault;
        }

        currentSelectedOption = btnAnswers[buttonIndex];
        currentSelectedOption.image.color = colorSelected;
    }

    public void Button_ConfirmMatch()
    {
        if (!isGameActive || isAnimating) return;

        if (currentSelectedOption == null) return;

        StartCoroutine(HandleMatchResult());
    }

    IEnumerator HandleMatchResult()
    {
        isAnimating = true;

        string selectedAnswer = currentSelectedOption.GetComponentInChildren<TextMeshProUGUI>().text;
        string correctAnswer = GetCorrectAnswer(currentTargetData);

        bool isCorrect = (selectedAnswer == correctAnswer);

        float waitDuration = 0.5f;

        if (isCorrect)
        {
            currentSelectedOption.image.color = colorCorrect;

            timeRemaining = Mathf.Min(timeRemaining + 5f, maxTime);

            if (GameSessionData.IsInfinityMode)
            {
                sessionScore += 10;
            }
            else
            {
                sessionCorrectCount++;
            }

            UpdateScoreUI();
            waitDuration = 0.5f;
        }
        else
        {
            currentSelectedOption.image.color = colorWrong;

            for (int i = 0; i < txtAnswers.Length; i++)
            {
                if (txtAnswers[i].text == correctAnswer)
                {
                    btnAnswers[i].image.color = colorCorrectReveal;
                    break;
                }
            }

            timeRemaining -= 1f;
            waitDuration = 2f;
        }

        yield return new WaitForSeconds(waitDuration);

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            imgTime.fillAmount = 0;
            GameOver();
        }
        else if (!GameSessionData.IsInfinityMode && sessionCorrectCount >= levelTargetCount)
        {
            GameWon();
        }
        else
        {
            RefreshQuestion();
        }

        isAnimating = false;
    }

    void UpdateScoreUI()
    {
        if (txtScore != null)
        {
            if (GameSessionData.IsInfinityMode)
            {
                txtScore.text = sessionScore.ToString();
            }
            else
            {
                txtScore.text = sessionCorrectCount.ToString() + "/" + levelTargetCount.ToString();
            }
        }
    }

    void GameOver()
    {
        isGameActive = false;

        if (GameSessionData.IsInfinityMode)
        {
            float multiplier = 1.0f;
            if (levelMultipliers.ContainsKey(GameSessionData.SelectedLevel))
            {
                multiplier = levelMultipliers[GameSessionData.SelectedLevel];
            }

            int finalScore = Mathf.RoundToInt(sessionScore * multiplier);
            if (txtDeathScore_Infinity != null) txtDeathScore_Infinity.text = finalScore.ToString();

            int highScore = PlayerPrefs.GetInt("Global_HighScore_QuickTest", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_QuickTest", highScore);
                PlayerPrefs.Save();
            }
            if (txtHighScore_Infinity != null) txtHighScore_Infinity.text = "Rekor: " + highScore.ToString();

            pnlDeath_Infinity.SetActive(true);
        }
        else
        {
            if (txtDeathGuessCount_Level != null) txtDeathGuessCount_Level.text = sessionCorrectCount.ToString() + "/" + levelTargetCount.ToString() + " Doðru";
            pnlDeath_Level.SetActive(true);
        }
    }

    void GameWon()
    {
        isGameActive = false;

        // YENÝ: LEVEL BÝTÝRME KAYDI (SAVE SÝSTEMÝ)
        if (!GameSessionData.IsInfinityMode)
        {
            string saveKey = $"Completed_{GameSessionData.SelectedLevel}_{GameSessionData.CurrentUnitIndex}_{GameSessionData.CurrentLevelIndex}";
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        pnlWin_Level.SetActive(true);
    }

    // --- YENÝ: NEXT LEVEL BUTONU ÝÇÝN METOT ---
    public void Button_NextLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        // Olasý durdurulmuþ zamaný normale çevir
        Time.timeScale = 1;

        int nextIndex = GameSessionData.CurrentLevelIndex + 1;

        // Eðer 5. levelde (index 4) degilsek bir sonraki levele gec
        if (nextIndex < GameSessionData.CurrentUnitLevels.Count)
        {
            GameSessionData.CurrentLevelIndex = nextIndex;

            LevelConfig nextLevelConfig = GameSessionData.CurrentUnitLevels[nextIndex];
            GameSessionData.TargetJsonFile = nextLevelConfig.jsonFileName;

            SceneController.Instance.LoadScene(nextLevelConfig.sceneName);
        }
        else
        {
            // 5. Level bittiyse (Ünite bittiyse) menuye geri don ve ilgili dilin Chapter panelini otomatik ac
            PlayerPrefs.SetString("AutoOpenChapter", GameSessionData.SelectedLevel);
            SceneController.Instance.LoadScene("LevelsMenu");
        }
    }

    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (isAnimating) return;
        isGameActive = false;
        pnlPause.SetActive(true);
    }

    public void Button_ResumeGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        isGameActive = true;
        pnlPause.SetActive(false);
    }

    public void Button_RetryGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;
        SceneController.Instance.LoadScene("QuickTest");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;
        SceneController.Instance.LoadScene("GamesMenu");
    }
}

// --- JSON VERI YAPILARI ---

[System.Serializable]
public class QuickTestDatabase
{
    public string level;
    public List<QuickTestData> questions;
}

[System.Serializable]
public class QuickTestData
{
    // Turkce altyapisi
    public string questionTr;
    public string correctAnswerTr;
    public string[] wrongAnswersTr; // 3 adet yanlis cevap listesi

    // Ingilizce altyapisi
    public string questionEn;
    public string correctAnswerEn;
    public string[] wrongAnswersEn;

    // Almanca altyapisi
    public string questionDe;
    public string correctAnswerDe;
    public string[] wrongAnswersDe;
}