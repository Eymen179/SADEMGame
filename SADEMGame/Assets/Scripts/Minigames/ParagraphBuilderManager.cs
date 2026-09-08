using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // YENI: DOTween kutuphanesi

public class ParagraphBuilderManager : MonoBehaviour
{
    public static int sessionScore = 0;
    public static int sessionCorrectCount = 0;
    private int levelTargetCount = 5;

    [Header("Top UI")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtScore;

    // YENI: Can Sistemi (Dolu kalp gorsellerini buraya atayacagiz)
    public GameObject[] imgHearts;
    private int currentLives = 3;

    // YENI: Hizli Gecis (Understand Time) Butonu ve Fill Gorseli
    public Button btnUnderstandTime;
    public Image imgUnderstandTimeFill;

    [Header("Ortak Paneller")]
    public GameObject pnlPause;
    // YENI: Pause panelinin icindeki asil pencere (hareket edecek kisim)
    public RectTransform pnlPauseWindow;
    public float pauseAnimDuration = 0.3f;

    [Header("Sonsuz Mod Panelleri")]
    public GameObject pnlDeath_Infinity;
    public TextMeshProUGUI txtDeathScore_Infinity;
    public TextMeshProUGUI txtHighScore_Infinity;

    [Header("Level Modu Panelleri")]
    public GameObject pnlDeath_Level;
    public GameObject pnlWin_Level;
    public TextMeshProUGUI txtDeathGuessCount_Level;

    [Header("Oyun Ici Kontroller")]
    public Image imgParagraphInOrder; // BtnParagraphInOrder arka plani
    public TextMeshProUGUI txtParagraphInOrder; // BtnParagraphInOrder icindeki metin

    public Button[] btnSentences; // 4 adet cumle butonu
    public TextMeshProUGUI[] txtSentences;
    public Button btnConfirm;

    [Header("Renk Ayarlari")]
    public Color colorDefaultBtn = Color.white;
    public Color colorTextDefault = new Color(0.2f, 0.2f, 0.2f); // Siyah/Koyu Gri Yazi
    public Color colorTextSelected = Color.white; // Secilince Beyaz Yazi

    public Color colorCorrect = new Color(0.33f, 0.55f, 0.27f); // Yesil
    public Color colorWrong = new Color(0.8f, 0.2f, 0.2f); // Kirmizi

    [Header("Mavi Tonlari (Aciktan Koyuya)")]
    public Color[] selectionColors = new Color[4]
    {
        new Color(0.73f, 0.87f, 0.98f), // 1. Secim (Acik Mavi)
        new Color(0.39f, 0.71f, 0.96f), // 2. Secim
        new Color(0.13f, 0.59f, 0.95f), // 3. Secim
        new Color(0.10f, 0.46f, 0.82f)  // 4. Secim (Koyu Mavi)
    };

    // --- Sistem Degiskenleri ---
    private ParagraphBuilderDatabase currentDatabase;
    private ParagraphBuilderData currentTargetData;

    private string activeForeignLanguage = "tr";
    private string correctParagraph = "";

    private List<int> selectedIndices = new List<int>();

    private bool isGameActive = false;
    private bool isAnimating = false;
    private bool isUnderstandSkipped = false; // Oyuncunun hizli gecis butonuna basip basmadigini kontrol eder

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

        if (btnUnderstandTime != null) btnUnderstandTime.gameObject.SetActive(false);

        if (txtLanguageLevel != null) txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null) txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null) imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        currentLives = 3;
        UpdateHeartsUI();
        UpdateScoreUI();

        LoadJsonData();
        RefreshQuestion();

        isGameActive = true;
    }

    void LoadJsonData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(GameSessionData.TargetJsonFile);
        if (jsonFile != null)
        {
            currentDatabase = JsonUtility.FromJson<ParagraphBuilderDatabase>(jsonFile.text);
        }
    }

    void RefreshQuestion()
    {
        selectedIndices.Clear();

        // Ust kutuyu sifirla
        imgParagraphInOrder.color = colorDefaultBtn;
        txtParagraphInOrder.color = colorTextDefault;
        txtParagraphInOrder.text = "...";

        if (currentDatabase == null || currentDatabase.questions.Count == 0) return;

        // Rastgele paragraf sec
        int targetIndex = Random.Range(0, currentDatabase.questions.Count);
        currentTargetData = currentDatabase.questions[targetIndex];

        string[] correctSentences = GetParagraphSentences(currentTargetData);

        // Paragrafin dogru halini hafizaya al
        correctParagraph = string.Join(" ", correctSentences);

        // Cumleleri karistir (Shuffle)
        List<string> shuffledSentences = new List<string>(correctSentences);
        for (int i = 0; i < shuffledSentences.Count; i++)
        {
            string temp = shuffledSentences[i];
            int randomIndex = Random.Range(i, shuffledSentences.Count);
            shuffledSentences[i] = shuffledSentences[randomIndex];
            shuffledSentences[randomIndex] = temp;
        }

        // Karistirilmis cumleleri butonlara yazdir ve renkleri sifirla
        for (int i = 0; i < btnSentences.Length; i++)
        {
            if (i < shuffledSentences.Count)
            {
                txtSentences[i].text = shuffledSentences[i];
            }
            btnSentences[i].image.color = colorDefaultBtn;
            txtSentences[i].color = colorTextDefault;
        }
    }

    string[] GetParagraphSentences(ParagraphBuilderData data)
    {
        switch (activeForeignLanguage)
        {
            case "tr": return data.sentencesTr;
            case "en": return data.sentencesEn;
            case "de": return data.sentencesDe;
            default: return data.sentencesTr;
        }
    }

    // --- MIKRO ETKILESIMLER (UI GUNCELLEME) ---

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < imgHearts.Length; i++)
        {
            if (i < currentLives)
                imgHearts[i].SetActive(true); // Can varsa goster
            else
                imgHearts[i].SetActive(false); // Can yoksa gizle (arkasindaki bos kalp gozukecek)
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < btnSentences.Length; i++)
        {
            btnSentences[i].image.color = colorDefaultBtn;
            txtSentences[i].color = colorTextDefault;
        }

        List<string> currentParagraphList = new List<string>();

        for (int i = 0; i < selectedIndices.Count; i++)
        {
            int btnIndex = selectedIndices[i];

            btnSentences[btnIndex].image.color = selectionColors[i];
            txtSentences[btnIndex].color = colorTextSelected;

            currentParagraphList.Add(txtSentences[btnIndex].text);
        }

        if (currentParagraphList.Count > 0)
        {
            txtParagraphInOrder.text = string.Join(" ", currentParagraphList);
        }
        else
        {
            txtParagraphInOrder.text = "...";
        }
    }

    // --- BUTON METOTLARI ---

    // Hiyerarsideki BtnSentence butonlarina atanacak (0, 1, 2, 3)
    public void Button_SelectSentence(int buttonIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (!isGameActive || isAnimating) return;

        if (selectedIndices.Contains(buttonIndex))
        {
            selectedIndices.Remove(buttonIndex);
        }
        else
        {
            if (selectedIndices.Count < 4)
            {
                selectedIndices.Add(buttonIndex);
            }
        }

        UpdateSelectionVisuals();
    }

    public void Button_ConfirmMatch()
    {
        if (!isGameActive || isAnimating) return;

        if (selectedIndices.Count < 4) return;

        StartCoroutine(HandleMatchResult());
    }

    public void Button_SkipUnderstandTime()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (isAnimating)
        {
            isUnderstandSkipped = true;
        }
    }

    IEnumerator HandleMatchResult()
    {
        isAnimating = true;
        isUnderstandSkipped = false; // Her yeni kontrolde sifirla

        string playerParagraph = txtParagraphInOrder.text;
        bool isCorrect = (playerParagraph == correctParagraph);

        float waitDuration = isCorrect ? 10f : 45f;

        txtParagraphInOrder.color = colorTextSelected;

        if (isCorrect)
        {
            // DOGRU
            imgParagraphInOrder.color = colorCorrect;

            if (GameSessionData.IsInfinityMode) sessionScore += 10;
            else sessionCorrectCount++;

            UpdateScoreUI();
        }
        else
        {
            // YANLIS
            imgParagraphInOrder.color = colorWrong;
            txtParagraphInOrder.text = correctParagraph; // Dogrusunu goster

            currentLives--; // Cani azalt
            UpdateHeartsUI();
        }

        // Inceleme suresi UI'ini aktif et
        if (btnUnderstandTime != null) btnUnderstandTime.gameObject.SetActive(true);

        float timer = waitDuration;

        // Timer 0 olana kadar veya oyuncu Skip butonuna basana kadar calis
        while (timer > 0 && !isUnderstandSkipped)
        {
            timer -= Time.deltaTime;

            if (imgUnderstandTimeFill != null)
            {
                imgUnderstandTimeFill.fillAmount = timer / waitDuration;
            }

            yield return null; // Bir sonraki frame'i bekle
        }

        // Sure bitti veya skip edildi, UI'i kapat
        if (btnUnderstandTime != null) btnUnderstandTime.gameObject.SetActive(false);

        // Oyun sonu kontrolleri
        if (currentLives <= 0)
        {
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

    // --- UI VE OYUN SONU KONTROLLERÝ ---

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

            int highScore = PlayerPrefs.GetInt("Global_HighScore_ParagraphBuilder", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_ParagraphBuilder", highScore);
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

        // YENI: LEVEL BÝTÝRME KAYDI (SAVE SÝSTEMÝ)
        if (!GameSessionData.IsInfinityMode)
        {
            string saveKey = $"Completed_{GameSessionData.SelectedLevel}_{GameSessionData.CurrentUnitIndex}_{GameSessionData.CurrentLevelIndex}";
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        pnlWin_Level.SetActive(true);
    }

    public void Button_NextLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

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

    // YENI: Pause panel animasyonlari DOTween ile yapildi
    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (isAnimating) return; // Paragrafi incelerken pause acilmasini engelliyoruz
        isGameActive = false;

        // Paneli aktif et (Arka plan kararmasi gorunur olur)
        pnlPause.SetActive(true);
        Time.timeScale = 0; // Zamani durdur

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

    public void Button_RetryGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;
        SceneController.Instance.LoadScene("ParagraphBuilder");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;

        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);

    }
}

// --- JSON VERI YAPILARI ---

[System.Serializable]
public class ParagraphBuilderDatabase
{
    public string level;
    public List<ParagraphBuilderData> questions;
}

[System.Serializable]
public class ParagraphBuilderData
{
    // Paragrafi 4 cumle (string) halinde tutacak diziler
    public string[] sentencesTr;
    public string[] sentencesEn;
    public string[] sentencesDe;
}