using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class WordleDatabase
{
    public string level;
    public List<GuessWordData> words;
}

[System.Serializable]
public class GuessWordData
{
    public string tr;
    public string en;
    public string de;
    public string es;
    public string fr;
}

[System.Serializable]
public class WordRow
{
    public Image[] boxImages;
    public TextMeshProUGUI[] boxTexts;
}

[System.Serializable]
public class LetterPanelConfig
{
    [Tooltip("Hiyerarsideki PnlLetters objesi")]
    public GameObject panelObject;
    [Tooltip("Bu panele ait satirlar (Kac tahmin hakki varsa)")]
    public WordRow[] guessRows;
}

public class GuessWordManager : MonoBehaviour
{
    public static int sessionGuessedCount = 0;
    public static int sessionBaseScore = 0;

    private int levelTargetCount = 5;

    [Header("Top UI")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtGuessCount;

    [Header("Ortak Paneller")]
    public GameObject pnlPause;
    // YENI: Pause panelinin icindeki asil pencere (hareket edecek kisim)
    public RectTransform pnlPauseWindow;
    public float pauseAnimDuration = 0.3f;

    [Header("Sonsuz Mod Panelleri")]
    public GameObject pnlDeath_Infinity;
    public GameObject pnlNextWord_Infinity;

    [Header("Level Modu Panelleri")]
    public GameObject pnlDeath_Level;
    public GameObject pnlNextWord_Level;
    public GameObject pnlWin_Level;

    [Header("Panel Icerik Textleri (Death & Next)")]
    public TextMeshProUGUI txtDeathWord_Infinity;
    public TextMeshProUGUI txtDeathGuessCount_Infinity;

    public TextMeshProUGUI txtDeathScore_Infinity;
    public TextMeshProUGUI txtHighScore_Infinity;

    public TextMeshProUGUI txtDeathWord_Level;
    public TextMeshProUGUI txtDeathGuessCount_Level;

    public TextMeshProUGUI txtNextWord_Infinity;
    public TextMeshProUGUI txtNextWord_Level;

    [Header("Oyun Ici Kontroller")]
    public TMP_InputField wordInputField;
    public GameObject confirmButton;
    public TextMeshProUGUI txtDescription;

    [Header("Ipucu (Hint) Sistemi")]
    public GameObject btnHint;
    public TextMeshProUGUI txtPlaceholder;
    private string targetWordEnglish;
    private bool hintUsedForCurrentWord = false;

    [Header("Wordle Dinamik Grid Panelleri")]
    public LetterPanelConfig panelA1_A2_B1;
    public LetterPanelConfig panelB2;
    public LetterPanelConfig panelC1;

    [Header("Renk Ayarlari")]
    public Color colorCorrect = new Color(0.33f, 0.55f, 0.27f);
    public Color colorWrongPosition = new Color(0.71f, 0.61f, 0.23f);
    public Color colorIncorrect = new Color(0.23f, 0.23f, 0.24f);
    public Color colorDefault = Color.white;
    public Color colorDeactivated = Color.black;

    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    private WordleDatabase currentWordDatabase;
    private string targetWord;
    private int targetWordLength;

    private int activeStartIndex = 0;

    private WordRow[] activeGuessRows;
    private int currentRowIndex = 0;
    private bool isGameActive = false;
    private bool isAnimating = false;

    void Start()
    {
        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlNextWord_Infinity.SetActive(false);
        pnlDeath_Level.SetActive(false);
        pnlNextWord_Level.SetActive(false);
        pnlWin_Level.SetActive(false);

        hintUsedForCurrentWord = false;
        if (btnHint != null) btnHint.SetActive(false);
        if (txtPlaceholder != null) txtPlaceholder.text = "Enter text...";

        if (txtLanguageLevel != null)
            txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null)
            txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null)
            imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        UpdateTopUIGuessCount();

        if (wordInputField != null) wordInputField.gameObject.SetActive(true);
        if (confirmButton != null) confirmButton.SetActive(true);

        LoadJsonData();
        PickRandomWord();
        SetupActivePanel();
        ResetGridUI();

        isGameActive = true;
    }

    void SetupActivePanel()
    {
        if (panelA1_A2_B1.panelObject != null) panelA1_A2_B1.panelObject.SetActive(false);
        if (panelB2.panelObject != null) panelB2.panelObject.SetActive(false);
        if (panelC1.panelObject != null) panelC1.panelObject.SetActive(false);

        string level = GameSessionData.SelectedLevel;

        if (level == "A1" || level == "A2" || level == "B1")
        {
            panelA1_A2_B1.panelObject.SetActive(true);
            activeGuessRows = panelA1_A2_B1.guessRows;
        }
        else if (level == "B2")
        {
            panelB2.panelObject.SetActive(true);
            activeGuessRows = panelB2.guessRows;
        }
        else if (level == "C1")
        {
            panelC1.panelObject.SetActive(true);
            activeGuessRows = panelC1.guessRows;
        }
        else
        {
            panelA1_A2_B1.panelObject.SetActive(true);
            activeGuessRows = panelA1_A2_B1.guessRows;
        }
    }

    void UpdateTopUIGuessCount()
    {
        if (txtGuessCount != null)
        {
            if (GameSessionData.IsInfinityMode)
            {
                txtGuessCount.text = sessionGuessedCount.ToString();
            }
            else
            {
                txtGuessCount.text = sessionGuessedCount.ToString() + "/" + levelTargetCount.ToString();
            }
        }
    }

    void LoadJsonData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(GameSessionData.TargetJsonFile);
        if (jsonFile != null)
        {
            currentWordDatabase = JsonUtility.FromJson<WordleDatabase>(jsonFile.text);
        }
    }

    void PickRandomWord()
    {
        if (currentWordDatabase != null && currentWordDatabase.words.Count > 0)
        {
            int randomIndex = Random.Range(0, currentWordDatabase.words.Count);

            GuessWordData selectedWordData = currentWordDatabase.words[randomIndex];

            targetWord = selectedWordData.tr.ToLower();
            targetWordEnglish = selectedWordData.en;
            targetWordLength = targetWord.Length;

            Debug.Log("Hedef: " + targetWord + " | Ingilizce: " + targetWordEnglish);
        }
    }

    void ResetGridUI()
    {
        if (activeGuessRows == null || activeGuessRows.Length == 0) return;

        int totalBoxesInRow = activeGuessRows[0].boxImages.Length;
        activeStartIndex = (totalBoxesInRow - targetWordLength) / 2;

        foreach (WordRow row in activeGuessRows)
        {
            for (int i = 0; i < totalBoxesInRow; i++)
            {
                if (i >= activeStartIndex && i < activeStartIndex + targetWordLength)
                {
                    row.boxImages[i].color = colorDefault;
                    row.boxTexts[i].text = "";
                }
                else
                {
                    row.boxImages[i].color = colorDeactivated;
                    row.boxTexts[i].text = "";
                }
            }
        }
    }

    public void Button_ConfirmInput()
    {
        if (!isGameActive || isAnimating) return;
        if (wordInputField == null) return;

        string playerGuess = wordInputField.text.Trim().ToLower();

        if (playerGuess.Length != targetWordLength)
        {
            ShowDescriptionMessage("Kelime " + targetWordLength + " harfli olmalidir!");
            wordInputField.ActivateInputField();
            return;
        }

        wordInputField.text = "";
        wordInputField.ActivateInputField();

        StartCoroutine(VerifyWordAndAnimate(playerGuess));
    }

    IEnumerator VerifyWordAndAnimate(string guess)
    {
        isAnimating = true;
        WordRow currentRow = activeGuessRows[currentRowIndex];

        for (int i = 0; i < targetWordLength; i++)
        {
            currentRow.boxTexts[activeStartIndex + i].text = guess[i].ToString().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        }

        Color[] resultColors = new Color[targetWordLength];
        bool[] targetLetterUsed = new bool[targetWordLength];
        bool[] guessLetterChecked = new bool[targetWordLength];

        for (int i = 0; i < targetWordLength; i++)
        {
            if (guess[i] == targetWord[i])
            {
                resultColors[i] = colorCorrect;
                targetLetterUsed[i] = true;
                guessLetterChecked[i] = true;
            }
        }

        for (int i = 0; i < targetWordLength; i++)
        {
            if (!guessLetterChecked[i])
            {
                bool found = false;
                for (int j = 0; j < targetWordLength; j++)
                {
                    if (!targetLetterUsed[j] && targetWord[j] == guess[i])
                    {
                        resultColors[i] = colorWrongPosition;
                        targetLetterUsed[j] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    resultColors[i] = colorIncorrect;
                }
            }
        }

        for (int i = 0; i < targetWordLength; i++)
        {
            int boxIndex = activeStartIndex + i;
            Transform boxTransform = currentRow.boxImages[boxIndex].rectTransform;
            Color targetColor = resultColors[i];

            boxTransform.DOScaleX(0, 0.15f).SetEase(Ease.InSine);
            yield return new WaitForSeconds(0.15f);

            currentRow.boxImages[boxIndex].color = targetColor;

            boxTransform.DOScaleX(1, 0.15f).SetEase(Ease.OutSine);
            yield return new WaitForSeconds(0.15f);
        }

        if (guess == targetWord)
        {
            WordGuessedSuccessfully();
        }
        else
        {
            currentRowIndex++;

            if (currentRowIndex >= activeGuessRows.Length)
            {
                GameOverLost();
            }
            else if (currentRowIndex == 3)
            {
                if (btnHint != null) btnHint.SetActive(true);
            }
        }

        isAnimating = false;
    }

    void ShowDescriptionMessage(string message)
    {
        if (txtDescription != null)
        {
            txtDescription.text = message;
            txtDescription.DOFade(1, 0).OnComplete(() => txtDescription.DOFade(0, 2f).SetDelay(1f));
        }
    }

    void HideInputUI()
    {
        if (wordInputField != null) wordInputField.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.SetActive(false);
        if (btnHint != null) btnHint.SetActive(false);
    }

    void WordGuessedSuccessfully()
    {
        isGameActive = false;
        HideInputUI();

        sessionGuessedCount++;

        int earnedPoints = hintUsedForCurrentWord ? 15 : 20;
        sessionBaseScore += earnedPoints;

        string formattedTargetWord = targetWord.ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (GameSessionData.IsInfinityMode)
        {
            if (txtNextWord_Infinity != null) txtNextWord_Infinity.text = formattedTargetWord;
            pnlNextWord_Infinity.SetActive(true);
        }
        else
        {
            if (txtNextWord_Level != null) txtNextWord_Level.text = formattedTargetWord;
            pnlNextWord_Level.SetActive(true);
        }
    }

    void GameOverLost()
    {
        isGameActive = false;
        HideInputUI();

        string formattedTargetWord = targetWord.ToUpper(new System.Globalization.CultureInfo("tr-TR"));

        if (GameSessionData.IsInfinityMode)
        {
            if (txtDeathWord_Infinity != null) txtDeathWord_Infinity.text = formattedTargetWord;
            if (txtDeathGuessCount_Infinity != null) txtDeathGuessCount_Infinity.text = sessionGuessedCount.ToString() + " Word Guessed";

            float multiplier = 1.0f;
            if (levelMultipliers.ContainsKey(GameSessionData.SelectedLevel))
            {
                multiplier = levelMultipliers[GameSessionData.SelectedLevel];
            }

            int finalScore = Mathf.RoundToInt(sessionBaseScore * multiplier);

            if (txtDeathScore_Infinity != null) txtDeathScore_Infinity.text = finalScore.ToString();

            // YENI: PlayerPrefs ile High Score Kaydi (Tek ve Ortak Bir Key Kullaniliyor)
            int highScore = PlayerPrefs.GetInt("Global_HighScore_GuessWord", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_GuessWord", highScore);
                PlayerPrefs.Save(); // Garantiye almak adina degisiklikleri aninda kaydediyoruz
            }
            if (txtHighScore_Infinity != null) txtHighScore_Infinity.text = "Rekor: " + highScore.ToString();

            // YENI: Olum panelini DOTween ile ekrana buyuterek cagiriyoruz
            pnlDeath_Infinity.SetActive(true);
            pnlDeath_Infinity.transform.localScale = Vector3.zero;
            pnlDeath_Infinity.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            if (txtDeathWord_Level != null) txtDeathWord_Level.text = formattedTargetWord;
            if (txtDeathGuessCount_Level != null) txtDeathGuessCount_Level.text = sessionGuessedCount.ToString() + "/" + levelTargetCount.ToString() + " Word Guessed";

            // YENI: Olum panelini DOTween ile ekrana buyuterek cagiriyoruz
            pnlDeath_Level.SetActive(true);
            pnlDeath_Level.transform.localScale = Vector3.zero;
            pnlDeath_Level.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    // --- BUTON METOTLARI ---

    public void Button_UseHint()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (txtPlaceholder != null && !string.IsNullOrEmpty(targetWordEnglish))
        {
            txtPlaceholder.text = targetWordEnglish.ToUpper(new System.Globalization.CultureInfo("en-US"));

            hintUsedForCurrentWord = true;
        }

        if (btnHint != null)
        {
            btnHint.SetActive(false);
        }
    }

    // YENI: Pause panel animasyonlari DOTween ile yapildi
    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (isAnimating) return;
        isGameActive = false;

        // Paneli aktif et (Arka plan kararmasi gorunur olur)
        pnlPause.SetActive(true);
        // Wordle oyununda sure bazli islem olmasa da tutarlilik icin timeScale = 0 yapilir
        Time.timeScale = 0;

        if (wordInputField != null) wordInputField.DeactivateInputField();

        // Animasyon Baslangici: Paneli ekranin sol ust kosesine, kucultulmus bir sekilde koy
        pnlPauseWindow.localPosition = new Vector3(-800f, 1500f, 0f);
        pnlPauseWindow.localScale = Vector3.zero;

        // Olasi onceki tweenleri iptal et
        pnlPauseWindow.DOKill();

        // Orta noktaya (0,0,0) getirirken scale'i 1 yap. SetUpdate(true) ile Time.timeScale=0'i yoksay
        pnlPauseWindow.DOLocalMove(Vector3.zero, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Button_ResumeGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        // Olasi onceki tweenleri iptal et
        pnlPauseWindow.DOKill();

        // Paneli orta noktadan tekrar sol ust koseye ve 0 boyutuna dogru yolla
        pnlPauseWindow.DOLocalMove(new Vector3(-800f, 1500f, 0f), pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.zero, pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            // Animasyon tamamen bittiginde paneli kapat ve oyunu baslat
            pnlPause.SetActive(false);
            isGameActive = true;
            Time.timeScale = 1;
        });
    }

    public void Button_NextWord()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (GameSessionData.IsInfinityMode)
        {
            SceneController.Instance.LoadScene("GuessWord");
        }
        else
        {
            if (sessionGuessedCount >= levelTargetCount)
            {
                pnlNextWord_Level.SetActive(false);

                // YENI: LEVEL BÝTÝRME KAYDI (SAVE SISTEMI)
                string saveKey = $"Completed_{GameSessionData.SelectedLevel}_{GameSessionData.CurrentUnitIndex}_{GameSessionData.CurrentLevelIndex}";
                PlayerPrefs.SetInt(saveKey, 1);
                PlayerPrefs.Save();

                // YENI: Kazanma panelini DOTween ile ekrana buyuterek cagiriyoruz
                pnlWin_Level.SetActive(true);
                pnlWin_Level.transform.localScale = Vector3.zero;
                pnlWin_Level.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else
            {
                SceneController.Instance.LoadScene("GuessWord");
            }
        }
    }

    public void Button_NextLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        // Yeni levele gecerken sayaclari sifirla
        sessionGuessedCount = 0;
        sessionBaseScore = 0;
        Time.timeScale = 1;

        int nextIndex = GameSessionData.CurrentLevelIndex + 1;

        // Eger 5. levelde (index 4) degilsek bir sonraki levele gec
        if (nextIndex < GameSessionData.CurrentUnitLevels.Count)
        {
            GameSessionData.CurrentLevelIndex = nextIndex;

            LevelConfig nextLevelConfig = GameSessionData.CurrentUnitLevels[nextIndex];
            GameSessionData.TargetJsonFile = nextLevelConfig.jsonFileName;

            SceneController.Instance.LoadScene(nextLevelConfig.sceneName);
        }
        else
        {
            // 5. Level bittiyse (Unite bittiyse) menuye geri don ve ilgili dilin Chapter panelini otomatik ac
            PlayerPrefs.SetString("AutoOpenChapter", GameSessionData.SelectedLevel);
            SceneController.Instance.LoadScene("LevelsMenu");
        }
    }

    public void Button_RetryGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionGuessedCount = 0;
        sessionBaseScore = 0;
        Time.timeScale = 1;
        SceneController.Instance.LoadScene("GuessWord");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionGuessedCount = 0;
        sessionBaseScore = 0;
        Time.timeScale = 1;
        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);
    }
}