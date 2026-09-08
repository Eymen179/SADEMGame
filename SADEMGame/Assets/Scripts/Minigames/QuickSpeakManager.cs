using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
// YENI: Oculus kutuphanesi yerine Saf Wit.ai kutuphanesi eklendi
using Meta.WitAi.Dictation;
// YENI: Android izinleri icin gerekli kutuphane eklendi
using UnityEngine.Android;

[System.Serializable]
public class QuickSpeakDatabase
{
    public string level;
    public List<QuickSpeakData> words;
}

[System.Serializable]
public class QuickSpeakData
{
    public string tr;
    public string en;
    public string de;
    public string es;
    public string fr;
}

public class QuickSpeakManager : MonoBehaviour
{
    public static int sessionScore = 0;
    public static int sessionCorrectCount = 0;
    private int levelTargetCount = 5;

    [Header("Top UI")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtScore;
    public Image imgTime;

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

    [Header("Oyun Ici UI Kontrolleri")]
    public TextMeshProUGUI txtSelectedWord;
    public TMP_InputField inputFieldSpeakToText;

    [Header("Mikrofon Butonu Gorselleri")]
    public Image imgMicButton;
    public Color colorMicIdle = new Color(0.9f, 0.1f, 0.2f);
    public Color colorMicListening = Color.green;

    [Header("Voice SDK (Ses Motoru)")]
    // YENI: AppDictationExperience yerine WitDictation kullaniyoruz
    public WitDictation witDictation;

    // --- Zaman Degiskenleri ---
    private float maxTime = 30f;
    private float timeRemaining = 30f;

    // --- Sistem Degiskenleri ---
    private QuickSpeakDatabase currentDatabase;
    private QuickSpeakData currentTargetData;
    private string targetWordTurkish;

    private bool isGameActive = false;
    private bool isListening = false;

    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    private void Awake()
    {
        // MANIFEST HACK: Unity'nin mikrofon kullandigimizi algilayip APK'ya 
        // RECORD_AUDIO iznini zorla yazdirmasi icin eklenmis sahte bir kod.
        if (Microphone.devices.Length > 0) { string dummy = Microphone.devices[0]; }
    }

    private void OnEnable()
    {
        if (witDictation != null)
        {
            witDictation.DictationEvents.OnPartialTranscription.AddListener(OnSpeechRecognized);
            witDictation.DictationEvents.OnFullTranscription.AddListener(OnSpeechRecognized);
        }
    }

    private void OnDisable()
    {
        if (witDictation != null)
        {
            witDictation.DictationEvents.OnPartialTranscription.RemoveListener(OnSpeechRecognized);
            witDictation.DictationEvents.OnFullTranscription.RemoveListener(OnSpeechRecognized);
        }
    }
    private void OnDestroy()
    {
        if (witDictation != null && witDictation.Active)
        {
            witDictation.DeactivateAndAbortRequest();
        }
    }

    void Start()
    {
        // ANDROID MIKROFON IZNI KONTROLU
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlDeath_Level.SetActive(false);
        pnlWin_Level.SetActive(false);

        if (txtLanguageLevel != null) txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null) txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null) imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        if (imgMicButton != null) imgMicButton.color = colorMicIdle;
        inputFieldSpeakToText.text = "";

        timeRemaining = maxTime;
        UpdateScoreUI();

        LoadJsonData();
        PickRandomWord();

        isGameActive = true;
    }

    void Update()
    {
        if (isGameActive)
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
            currentDatabase = JsonUtility.FromJson<QuickSpeakDatabase>(jsonFile.text);
        }
    }

    void PickRandomWord()
    {
        if (currentDatabase != null && currentDatabase.words.Count > 0)
        {
            int randomIndex = Random.Range(0, currentDatabase.words.Count);
            currentTargetData = currentDatabase.words[randomIndex];

            txtSelectedWord.text = currentTargetData.en.ToUpper(new System.Globalization.CultureInfo("en-US"));
            targetWordTurkish = currentTargetData.tr.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        }
    }

    // --- MIKROFON (EVENT TRIGGER) KONTROLLERI ---

    public void PointerDown_StartListening()
    {
        if (!isGameActive) return;

        isListening = true;
        if (imgMicButton != null) imgMicButton.color = colorMicListening;
        inputFieldSpeakToText.text = "";

        if (witDictation != null)
        {
            witDictation.Activate();
        }
    }

    public void PointerUp_StopListening()
    {
        if (!isGameActive) return;

        isListening = false;
        if (imgMicButton != null) imgMicButton.color = colorMicIdle;

        if (witDictation != null)
        {
            witDictation.Deactivate();
        }
    }

    public void OnSpeechRecognized(string transcribedText)
    {
        string cleanText = transcribedText.Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Trim();
        inputFieldSpeakToText.text = cleanText.ToLower(new System.Globalization.CultureInfo("tr-TR"));
    }

    private void CleanUpWitAi()
    {
        // 1. Dinlemeyi zorla kes
        if (witDictation != null && witDictation.Active)
        {
            witDictation.DeactivateAndAbortRequest();
        }

        // 2. Sahne hala aktifken (cokme riski yokken) o inatci objeyi bul ve yok et
        GameObject audioBufferObj = GameObject.Find("AudioBuffer");
        if (audioBufferObj != null)
        {
            Destroy(audioBufferObj);
        }
    }

    // --- BUTON METOTLARI ---

    public void Button_DeleteText()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        inputFieldSpeakToText.text = "";
    }

    public void Button_Confirm()
    {
        if (!isGameActive) return;

        string playerInput = inputFieldSpeakToText.text.Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Trim().
            ToLower(new System.Globalization.CultureInfo("tr-TR"));

        if (string.IsNullOrEmpty(playerInput)) return;

        if (playerInput == targetWordTurkish)
        {
            timeRemaining += 5f;
            if (timeRemaining > maxTime) timeRemaining = maxTime;

            if (GameSessionData.IsInfinityMode)
            {
                sessionScore += 10;
            }
            else
            {
                sessionCorrectCount++;
            }

            txtSelectedWord.DOColor(Color.green, 0.2f).OnComplete(() => txtSelectedWord.DOColor(Color.black, 0.2f));

            UpdateScoreUI();

            if (!GameSessionData.IsInfinityMode && sessionCorrectCount >= levelTargetCount)
            {
                GameWon();
            }
            else
            {
                inputFieldSpeakToText.text = "";
                PickRandomWord();
            }
        }
        else
        {
            timeRemaining -= 3f;
            txtSelectedWord.DOColor(Color.red, 0.2f).OnComplete(() => txtSelectedWord.DOColor(Color.black, 0.2f));
        }
    }

    // --- UI GUNCELLEME VE OYUN SONU ---

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

        if (witDictation != null) witDictation.Deactivate();

        if (GameSessionData.IsInfinityMode)
        {
            float multiplier = 1.0f;
            if (levelMultipliers.ContainsKey(GameSessionData.SelectedLevel))
            {
                multiplier = levelMultipliers[GameSessionData.SelectedLevel];
            }

            int finalScore = Mathf.RoundToInt(sessionScore * multiplier);
            if (txtDeathScore_Infinity != null) txtDeathScore_Infinity.text = finalScore.ToString();

            int highScore = PlayerPrefs.GetInt("Global_HighScore_FastSpeaking", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_FastSpeaking", highScore);
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
            if (txtDeathGuessCount_Level != null) txtDeathGuessCount_Level.text = sessionCorrectCount.ToString() + "/" + levelTargetCount.ToString() + " Dogru";
            // YENI: Olum panelini DOTween ile ekrana buyuterek cagiriyoruz
            pnlDeath_Level.SetActive(true);
            pnlDeath_Level.transform.localScale = Vector3.zero;
            pnlDeath_Level.transform.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    void GameWon()
    {
        isGameActive = false;
        if (witDictation != null) witDictation.Deactivate();

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

    public void Button_NextLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        CleanUpWitAi();

        sessionScore = 0;
        sessionCorrectCount = 0;
        Time.timeScale = 1;

        int nextIndex = GameSessionData.CurrentLevelIndex + 1;

        if (nextIndex < GameSessionData.CurrentUnitLevels.Count)
        {
            GameSessionData.CurrentLevelIndex = nextIndex;

            LevelConfig nextLevelConfig = GameSessionData.CurrentUnitLevels[nextIndex];
            GameSessionData.TargetJsonFile = nextLevelConfig.jsonFileName;

            SceneController.Instance.LoadScene(nextLevelConfig.sceneName);
        }
        else
        {
            PlayerPrefs.SetString("AutoOpenChapter", GameSessionData.SelectedLevel);
            SceneController.Instance.LoadScene("LevelsMenu");
        }
    }

    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        isGameActive = false;
        if (witDictation != null) witDictation.Deactivate();

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

        CleanUpWitAi();

        sessionScore = 0;
        sessionCorrectCount = 0;
        SceneController.Instance.LoadScene("QuickSpeak");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        CleanUpWitAi();

        sessionScore = 0;
        sessionCorrectCount = 0;

        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);

    }
}