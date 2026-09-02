using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class QuickMatchDatabase
{
    public string level;
    public List<QuickMatchWordData> words;
}

[System.Serializable]
public class QuickMatchWordData
{
    public string tr;
    public string en;
    public string de;
    public string es;
    public string fr;
}

public class QuickMatchManager : MonoBehaviour
{
    public static int sessionScore = 0; // Sonsuz mod icin puan
    public static int sessionCorrectCount = 0; // Level modu icin kelime bilme sayisi
    private int levelTargetCount = 5;

    [Header("Top UI")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtScore;
    public Image imgTime; // DTT Procedural UI Rounded Image, Unity'nin standart Image bileseninden turedigi icin dogrudan atanabilir.

    [Header("Ortak Paneller")]
    public GameObject pnlPause;

    [Header("Oyun Sonu Panelleri")]
    public GameObject pnlDeath_Infinity;
    public GameObject pnlDeath_Level;
    public GameObject pnlWin_Level;

    [Header("Sonsuz Mod Puan Textleri")]
    public TextMeshProUGUI txtDeathScore_Infinity;
    public TextMeshProUGUI txtHighScore_Infinity;

    [Header("Level Modu Skor Textleri")]
    public TextMeshProUGUI txtDeathGuessCount_Level;

    [Header("Oyun Içi Butonlar")]
    public Button btnSelectedWord; // Ustteki ana kelime butonu
    public TextMeshProUGUI txtSelectedWord;

    public Button[] btnSelectableWords; // Alttaki 5'li buton dizisi
    public TextMeshProUGUI[] txtSelectableWords;

    public Button btnConfirm;

    [Header("Renk Ayarlari")]
    public Color colorDefault = Color.white;
    public Color colorSelected = new Color(0.95f, 0.76f, 0.20f); // Sari
    public Color colorCorrect = new Color(0.33f, 0.55f, 0.27f); // Yesil
    public Color colorWrong = new Color(0.8f, 0.2f, 0.2f); // Kirmizi

    // --- Sistem Degiskenleri ---
    private QuickMatchDatabase currentDatabase;
    private QuickMatchWordData currentTargetData; // O anki dogru kelime cifti

    // Ileride farkli diller secildiginde degistirilecek anahtar degisken
    private string activeForeignLanguage = "en";

    private bool isGameActive = false;
    private bool isAnimating = false;
    private bool isMainWordSelected = false;
    private Button currentSelectedOption = null;

    // --- Zaman Degiskenleri ---
    private float maxTime = 20f;
    private float timeRemaining = 20f;

    // Dil Seviyesi Katsayilari (Multiplier)
    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    void Start()
    {
        // Panelleri gizle
        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlDeath_Level.SetActive(false);
        pnlWin_Level.SetActive(false);

        if (txtLanguageLevel != null)
            txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null)
            txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null)
            imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        timeRemaining = maxTime;
        UpdateScoreUI();
        LoadJsonData();
        RefreshWords();

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
            currentDatabase = JsonUtility.FromJson<QuickMatchDatabase>(jsonFile.text);
        }
    }

    void RefreshWords()
    {
        // Buton renklerini ve secimleri sifirla
        isMainWordSelected = false;
        currentSelectedOption = null;
        btnSelectedWord.image.color = colorDefault;

        foreach (Button btn in btnSelectableWords)
        {
            btn.image.color = colorDefault;
        }

        if (currentDatabase == null || currentDatabase.words.Count < 5) return;

        // 1. Hedef kelimeyi sec
        int targetIndex = Random.Range(0, currentDatabase.words.Count);
        currentTargetData = currentDatabase.words[targetIndex];

        // 2. Ileride sistemi cevirebilmek icin ozel metotla aktif dili aliyoruz (Su an 'en' doner)
        txtSelectedWord.text = GetForeignWord(currentTargetData).ToUpper(new System.Globalization.CultureInfo("en-US"));

        // 3. 4 adet yanlis, 1 adet dogru kelimeyi listeye ekle
        List<string> options = new List<string>();
        options.Add(currentTargetData.tr);

        while (options.Count < 5)
        {
            int randomWrongIndex = Random.Range(0, currentDatabase.words.Count);
            string wrongWord = currentDatabase.words[randomWrongIndex].tr;

            if (!options.Contains(wrongWord))
            {
                options.Add(wrongWord);
            }
        }

        // 4. Listeyi karistir (Shuffle)
        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        // 5. Butonlara yazdir
        for (int i = 0; i < btnSelectableWords.Length; i++)
        {
            txtSelectableWords[i].text = options[i].ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        }
    }

    // Gelecekte diger dilleri eklediginde tek degistirmen gereken yer burasi
    string GetForeignWord(QuickMatchWordData data)
    {
        switch (activeForeignLanguage)
        {
            case "en": return data.en;
            case "de": return data.de;
            case "es": return data.es;
            case "fr": return data.fr;
            default: return data.en;
        }
    }

    // --- OYUN ICI ETKILESIM METOTLARI ---

    // BtnSelectedWord'un OnClick event'ine baglanacak
    public void Button_SelectMainWord()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (!isGameActive || isAnimating) return;

        isMainWordSelected = true;
        btnSelectedWord.image.color = colorSelected;
    }

    // BtnSelectableWord butonlarinin OnClick eventine baglanacak. (Sahnede 0, 1, 2, 3, 4 seklinde parametre verilmeli)
    public void Button_SelectOptionWord(int buttonIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (!isGameActive || isAnimating) return;

        // Kural: Oyuncu ilk olarak Selected Word butonuna basmalidir
        if (!isMainWordSelected) return;

        // Eski secili olanin rengini sifirla
        if (currentSelectedOption != null)
        {
            currentSelectedOption.image.color = colorDefault;
        }

        currentSelectedOption = btnSelectableWords[buttonIndex];
        currentSelectedOption.image.color = colorSelected;
    }

    // BtnConfirm'un OnClick event'ine baglanacak
    public void Button_ConfirmMatch()
    {
        if (!isGameActive || isAnimating) return;
        if (!isMainWordSelected || currentSelectedOption == null) return;

        StartCoroutine(HandleMatchResult());
    }

    IEnumerator HandleMatchResult()
    {
        isAnimating = true;

        string selectedTurkishWord = currentSelectedOption.GetComponentInChildren<TextMeshProUGUI>().text.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        bool isCorrect = (selectedTurkishWord == currentTargetData.tr.ToLower());

        if (isCorrect)
        {
            // Dogruysa Yesil Yanar
            btnSelectedWord.image.color = colorCorrect;
            currentSelectedOption.image.color = colorCorrect;

            // Sure ekle
            timeRemaining += 5f;
            if (timeRemaining > maxTime) timeRemaining = maxTime;

            // Skor artisi
            if (GameSessionData.IsInfinityMode)
            {
                sessionScore += 10;
            }
            else
            {
                sessionCorrectCount++;
            }
            UpdateScoreUI();
        }
        else
        {
            // Yanlissa Kirmizi Yanar
            btnSelectedWord.image.color = colorWrong;
            currentSelectedOption.image.color = colorWrong;

            // Sure azalt
            timeRemaining -= 3f;
        }

        // Yarim saniyelik renk beklemesi
        yield return new WaitForSeconds(0.5f);

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
            RefreshWords();
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

            int highScore = PlayerPrefs.GetInt("Global_HighScore_QuickMatch", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_QuickMatch", highScore);
                PlayerPrefs.Save();
            }
            if (txtHighScore_Infinity != null) txtHighScore_Infinity.text = "Rekor: " + highScore.ToString();

            pnlDeath_Infinity.SetActive(true);
        }
        else
        {
            if (txtDeathGuessCount_Level != null) txtDeathGuessCount_Level.text = sessionCorrectCount.ToString() + "/" + levelTargetCount.ToString() + " Eþleþme";
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

    // --- ORTAK BUTON METOTLARI ---

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
        SceneController.Instance.LoadScene("QuickMatch");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;
        SceneController.Instance.LoadScene("GamesMenu");
    }
}