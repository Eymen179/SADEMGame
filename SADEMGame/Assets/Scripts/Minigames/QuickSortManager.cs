using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // YENI: DOTween eklendi

public class QuickSortManager : MonoBehaviour
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
    public Image imgSentenceInOrder; // BtnSentenceInOrder arka plani
    public TextMeshProUGUI txtSentenceInOrder; // BtnSentenceInOrder icindeki metin

    public Button[] btnSentenceParts; // 4 adet parca butonu
    public TextMeshProUGUI[] txtSentenceParts;
    public Button btnConfirm;

    [Header("Renk Ayarlari")]
    public Color colorDefaultBtn = Color.white; // Butonlarin basilmamis hali
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

    // --- Zaman Degiskenleri ---
    private float maxTime = 45f;
    private float timeRemaining = 45f;

    // --- Sistem Degiskenleri ---
    private QuickSortDatabase currentDatabase;
    private QuickSortData currentTargetData;

    private string activeForeignLanguage = "tr";
    private string correctSentence = ""; // Birlestirilmis dogru cumle

    // Oyuncunun sectigi butonlarin index'lerini sirasiyla tutar
    private List<int> selectedIndices = new List<int>();

    private bool isGameActive = false;
    private bool isAnimating = false;

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
            currentDatabase = JsonUtility.FromJson<QuickSortDatabase>(jsonFile.text);
        }
    }

    void RefreshQuestion()
    {
        selectedIndices.Clear();

        // Ust kutuyu sifirla
        imgSentenceInOrder.color = colorDefaultBtn;
        txtSentenceInOrder.color = colorTextDefault;
        txtSentenceInOrder.text = "..."; // Default bekleme yazisi

        if (currentDatabase == null || currentDatabase.questions.Count == 0) return;

        // Rastgele bir soru sec
        int targetIndex = Random.Range(0, currentDatabase.questions.Count);
        currentTargetData = currentDatabase.questions[targetIndex];

        string[] correctParts = GetSentenceParts(currentTargetData);

        // Parcalari birlestirip dogru cumleyi hafizaya al (Karsilastirma icin)
        correctSentence = string.Join(" ", correctParts);

        // Butonlara dagitmak icin parcalari karistir (Shuffle)
        List<string> shuffledParts = new List<string>(correctParts);
        for (int i = 0; i < shuffledParts.Count; i++)
        {
            string temp = shuffledParts[i];
            int randomIndex = Random.Range(i, shuffledParts.Count);
            shuffledParts[i] = shuffledParts[randomIndex];
            shuffledParts[randomIndex] = temp;
        }

        // Karistirilmis parcalari butonlara yazdir ve renkleri sifirla
        for (int i = 0; i < btnSentenceParts.Length; i++)
        {
            if (i < shuffledParts.Count)
            {
                txtSentenceParts[i].text = shuffledParts[i];
            }
            btnSentenceParts[i].image.color = colorDefaultBtn;
            txtSentenceParts[i].color = colorTextDefault;
        }
    }

    string[] GetSentenceParts(QuickSortData data)
    {
        switch (activeForeignLanguage)
        {
            case "tr": return data.partsTr;
            case "en": return data.partsEn;
            case "de": return data.partsDe;
            default: return data.partsTr;
        }
    }

    // --- BUTON METOTLARI ---

    // BtnSentencePart butonlarinin OnClick eventine baglanacak. (Inspector'dan 0, 1, 2, 3 girilmeli)
    public void Button_SelectPart(int buttonIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (!isGameActive || isAnimating) return;

        // Eger onceden secilmis bir butona tekrar basilirsa secimi kaldir
        if (selectedIndices.Contains(buttonIndex))
        {
            selectedIndices.Remove(buttonIndex);
        }
        else
        {
            // Secilmemisse ve 4'ten az secim yapilmissa listeye ekle
            if (selectedIndices.Count < 4)
            {
                selectedIndices.Add(buttonIndex);
            }
        }

        UpdateSelectionVisuals();
    }

    // Secim yapildikca UI'i guncelleyecek metot
    private void UpdateSelectionVisuals()
    {
        // Once butun butonlari default renge dondur
        for (int i = 0; i < btnSentenceParts.Length; i++)
        {
            btnSentenceParts[i].image.color = colorDefaultBtn;
            txtSentenceParts[i].color = colorTextDefault;
        }

        List<string> currentSentenceList = new List<string>();

        // Secili butonlari sirasiyla mavi tonlarina ve beyaz yaziya cevir
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            int btnIndex = selectedIndices[i];

            // i degerine gore mavinin tonunu belirle (0 en acik, 3 en koyu)
            btnSentenceParts[btnIndex].image.color = selectionColors[i];
            txtSentenceParts[btnIndex].color = colorTextSelected;

            // Ust kutuya yazilmak uzere kelimeyi listeye ekle
            currentSentenceList.Add(txtSentenceParts[btnIndex].text);
        }

        // Ust kutuyu guncelle
        if (currentSentenceList.Count > 0)
        {
            txtSentenceInOrder.text = string.Join(" ", currentSentenceList);
        }
        else
        {
            txtSentenceInOrder.text = "...";
        }
    }

    // BtnConfirm'un OnClick event'ine baglanacak
    public void Button_ConfirmMatch()
    {
        if (!isGameActive || isAnimating) return;

        // 4 parcanin tamami secilmeden onaylanamasin
        if (selectedIndices.Count < 4) return;

        StartCoroutine(HandleMatchResult());
    }

    IEnumerator HandleMatchResult()
    {
        isAnimating = true;

        string playerSentence = txtSentenceInOrder.text;
        bool isCorrect = (playerSentence == correctSentence);

        float waitDuration = 0.5f;

        // Gorsel geri bildirim icin ust kutunun yazi rengini beyaza cekelim
        txtSentenceInOrder.color = colorTextSelected;

        if (isCorrect)
        {
            // DOGRU
            imgSentenceInOrder.color = colorCorrect;

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
            // YANLIS
            imgSentenceInOrder.color = colorWrong;

            // Yanlis oldugunda dogru cumleyi beyaz renkte goster
            txtSentenceInOrder.text = correctSentence;

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

            int highScore = PlayerPrefs.GetInt("Global_HighScore_QuickSort", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_QuickSort", highScore);
                PlayerPrefs.Save();
            }
            if (txtHighScore_Infinity != null) txtHighScore_Infinity.text = "Rekor: " + highScore.ToString();

            // YENI: Olum panelini DOTween ile ekrana buyuterek cagiriyoruz
            pnlDeath_Infinity.SetActive(true);
            pnlDeath_Infinity.transform.localScale = Vector3.zero;
            pnlDeath_Infinity.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            if (txtDeathGuessCount_Level != null) txtDeathGuessCount_Level.text = sessionCorrectCount.ToString() + "/" + levelTargetCount.ToString() + " Doðru";
            // YENI: Olum panelini DOTween ile ekrana buyuterek cagiriyoruz
            pnlDeath_Level.SetActive(true);
            pnlDeath_Level.transform.localScale = Vector3.zero;
            pnlDeath_Level.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    void GameWon()
    {
        isGameActive = false;

        // YENI: LEVEL BITIRME KAYDI (SAVE SISTEMI)
        if (!GameSessionData.IsInfinityMode)
        {
            string saveKey = $"Completed_{GameSessionData.SelectedLevel}_{GameSessionData.CurrentUnitIndex}_{GameSessionData.CurrentLevelIndex}";
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        // YENI: Kazanma panelini DOTween ile ekrana buyuterek cagiriyoruz
        pnlWin_Level.SetActive(true);
        pnlWin_Level.transform.localScale = Vector3.zero;
        pnlWin_Level.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    // --- YENI: NEXT LEVEL BUTONU ICIN METOT ---
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

        if (isAnimating) return;
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
        SceneController.Instance.LoadScene("QuickSort");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;

        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);
    }
}

// --- JSON VERI YAPILARI EN ALTA TASINDI ---

[System.Serializable]
public class QuickSortDatabase
{
    public string level;
    public List<QuickSortData> questions;
}

[System.Serializable]
public class QuickSortData
{
    // Cumleyi 4 parca (string) halinde tutacak diziler
    public string[] partsTr;
    public string[] partsEn;
    public string[] partsDe;
}