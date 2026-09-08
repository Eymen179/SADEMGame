using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class CatchWordDatabase
{
    public string level;
    public List<CatchWordData> words;
}

[System.Serializable]
public class CatchWordData
{
    public string tr;
    public string en;
    public string de;
    public string es;
    public string fr;
}

public class CatchWordManager : MonoBehaviour
{
    public static CatchWordManager Instance;

    public static int sessionScore = 0;
    public static int sessionCorrectCount = 0;

    public static int totalCorrectCatches = 0;

    private int levelTargetCount = 5;

    [Header("Top UI & Can Sistemi")]
    public TextMeshProUGUI txtLanguageLevel;
    public Image imgLanguageLevel;
    public TextMeshProUGUI txtScore;
    public GameObject[] heartImages;

    [Header("Ortak Paneller")]
    public GameObject pnlPause;
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

    [Header("Sepet (Basket) Ayarlari")]
    public Transform basketTransform;
    public float basketMoveSpeed = 5f;
    public float minX = -1.2f;
    public float maxX = 1.2f;

    [Header("Kapak (Door) Ayarlari")]
    public Transform basketDoorRight;
    public Transform basketDoorLeft;
    private Vector3 doorRightStartPos;
    private Vector3 doorLeftStartPos;

    [Header("Kapak Butonu (Door Button) Ayarlari")]
    public Button btnDoor;
    public Image imgDoorIcon;
    public Sprite iconDoorOpen;
    public Sprite iconDoorClosed;
    public Color colorDoorOpen = Color.green;
    public Color colorDoorClosed = Color.red;

    [Header("Kelime Uretici (Spawner) Ayarlari")]
    public GameObject wordBoxPrefab;
    public Transform spawnPointParent;
    public TextMeshProUGUI txtSelectedWord;
    public float spawnInterval = 1.5f;

    private float currentWaitTime = 3f;

    // YENI: O anki round icinde kac kutu uretildigini tutan sayac
    private int currentRoundSpawnCount = 0;

    // --- Sistem Degiskenleri ---
    private CatchWordDatabase currentDatabase;
    private CatchWordData currentTargetData;
    private string targetWordEnglish;

    private bool isGameActive = false;
    private int currentLives = 3;

    private bool isMovingLeft = false;
    private bool isMovingRight = false;

    private Dictionary<string, float> levelMultipliers = new Dictionary<string, float>()
    {
        {"A1", 1.0f}, {"A2", 1.2f}, {"B1", 1.5f}, {"B2", 1.75f}, {"C1", 2.0f}
    };

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        pnlPause.SetActive(false);
        pnlDeath_Infinity.SetActive(false);
        pnlDeath_Level.SetActive(false);
        pnlWin_Level.SetActive(false);

        if (txtLanguageLevel != null) txtLanguageLevel.text = GameSessionData.SelectedLevel;
        if (txtLanguageLevel != null) txtLanguageLevel.color = GameSessionData.SelectedLevelTextColor;
        if (imgLanguageLevel != null) imgLanguageLevel.color = GameSessionData.SelectedLevelColor;

        doorRightStartPos = basketDoorRight.localPosition;
        doorLeftStartPos = basketDoorLeft.localPosition;
        basketDoorRight.gameObject.SetActive(false);
        basketDoorLeft.gameObject.SetActive(false);

        if (imgDoorIcon != null)
        {
            imgDoorIcon.sprite = iconDoorOpen;
            btnDoor.GetComponent<Image>().color = colorDoorOpen;
        }

        currentLives = heartImages.Length;
        UpdateScoreUI();
        UpdateHealthUI();

        currentWaitTime = 3f;

        LoadJsonData();
        StartCoroutine(StartNewRound());

        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive) return;

        if (isMovingLeft)
        {
            basketTransform.Translate(Vector3.left * basketMoveSpeed * Time.deltaTime);
        }
        else if (isMovingRight)
        {
            basketTransform.Translate(Vector3.right * basketMoveSpeed * Time.deltaTime);
        }

        float clampedX = Mathf.Clamp(basketTransform.position.x, minX, maxX);
        basketTransform.position = new Vector3(clampedX, basketTransform.position.y, basketTransform.position.z);
    }

    void LoadJsonData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(GameSessionData.TargetJsonFile);
        if (jsonFile != null)
        {
            currentDatabase = JsonUtility.FromJson<CatchWordDatabase>(jsonFile.text);
        }
    }

    IEnumerator StartNewRound()
    {
        PickRandomTargetWord();

        // YENI: Her yeni kelimede spawn sayacini sifirliyoruz
        currentRoundSpawnCount = 0;

        yield return new WaitForSeconds(currentWaitTime);
        StartCoroutine(SpawnWordsRoutine());
    }

    string FormatWordTitleCase(string word, string cultureInfo)
    {
        if (string.IsNullOrEmpty(word)) return word;
        var culture = new System.Globalization.CultureInfo(cultureInfo);
        return char.ToUpper(word[0], culture) + word.Substring(1).ToLower(culture);
    }

    void PickRandomTargetWord()
    {
        if (currentDatabase != null && currentDatabase.words.Count > 0)
        {
            int randomIndex = Random.Range(0, currentDatabase.words.Count);
            currentTargetData = currentDatabase.words[randomIndex];

            txtSelectedWord.text = FormatWordTitleCase(currentTargetData.tr, "tr-TR");
            targetWordEnglish = currentTargetData.en;
        }
    }

    IEnumerator SpawnWordsRoutine()
    {
        while (isGameActive)
        {
            SpawnWordBox();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnWordBox()
    {
        GameObject newBox = Instantiate(wordBoxPrefab, new Vector3(Random.Range(minX, maxX), 6f, 0f), Quaternion.identity);
        CatchWordItem itemScript = newBox.GetComponent<CatchWordItem>();

        // YENI: Uretilen kutu sayisini artir
        currentRoundSpawnCount++;

        bool isCorrectBox = false;

        // Eger uretilen kutu ilk 3'un icindeyse kesinlikle YANLIS, 4. ve sonrasiysa %50 ihtimalle DOGRU olacak
        if (currentRoundSpawnCount > 3)
        {
            isCorrectBox = (Random.value > 0.5f);
        }

        string boxWordEn = "";

        if (isCorrectBox)
        {
            boxWordEn = targetWordEnglish;
        }
        else
        {
            int randWrong = Random.Range(0, currentDatabase.words.Count);
            boxWordEn = currentDatabase.words[randWrong].en;

            // YENI GUVENLIK KILIDI: Rastgele secilen yanlis kelime, tesadufen dogru kelimeyse 
            // baska bir tane bulana kadar tekrar sec.
            while (boxWordEn == targetWordEnglish)
            {
                randWrong = Random.Range(0, currentDatabase.words.Count);
                boxWordEn = currentDatabase.words[randWrong].en;
            }
        }

        itemScript.Initialize(boxWordEn, this);
    }

    public void OnWordCaught(CatchWordItem caughtItem)
    {
        if (!isGameActive) return;

        Destroy(caughtItem.gameObject);

        if (caughtItem.assignedEnglishWord == targetWordEnglish)
        {
            CatchWordItem[] remainingWords = FindObjectsOfType<CatchWordItem>();
            foreach (CatchWordItem word in remainingWords)
            {
                Destroy(word.gameObject);
            }

            if (targetWordEnglish.ToLower() == "heart")
            {
                if (currentLives < heartImages.Length)
                {
                    currentLives++;
                    UpdateHealthUI();
                }
            }

            totalCorrectCatches++;
            if (totalCorrectCatches >= 10)
            {
                currentWaitTime = 1.5f;
            }

            if (GameSessionData.IsInfinityMode)
            {
                sessionScore += 10;
            }
            else
            {
                sessionCorrectCount++;
            }

            UpdateScoreUI();
            StopAllCoroutines();

            if (!GameSessionData.IsInfinityMode && sessionCorrectCount >= levelTargetCount)
            {
                GameWon();
            }
            else
            {
                StartCoroutine(StartNewRound());
            }
        }
        else
        {
            currentLives--;
            UpdateHealthUI();

            if (currentLives <= 0)
            {
                GameOver();
            }
        }
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

    void UpdateHealthUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].SetActive(i < currentLives);
        }
    }

    // Ses Gereken kisimlar
    public void PointerDown_MoveLeft() { isMovingLeft = true; }
    public void PointerUp_MoveLeft() { isMovingLeft = false; }

    public void PointerDown_MoveRight() { isMovingRight = true; }
    public void PointerUp_MoveRight() { isMovingRight = false; }

    public void PointerDown_Doors()
    {
        basketDoorRight.gameObject.SetActive(true);
        basketDoorLeft.gameObject.SetActive(true);

        basketDoorRight.DOLocalMove(doorRightStartPos + new Vector3(-0.50f, 0.50f, 0), 0.2f);
        basketDoorLeft.DOLocalMove(doorLeftStartPos + new Vector3(0.50f, 0.50f, 0), 0.2f);

        if (btnDoor != null)
        {
            imgDoorIcon.sprite = iconDoorClosed;
            btnDoor.GetComponent<Image>().color = colorDoorClosed;
        }
    }

    public void PointerUp_Doors()
    {
        basketDoorRight.DOLocalMove(doorRightStartPos, 0.2f).OnComplete(() => basketDoorRight.gameObject.SetActive(false));
        basketDoorLeft.DOLocalMove(doorLeftStartPos, 0.2f).OnComplete(() => basketDoorLeft.gameObject.SetActive(false));

        if (btnDoor != null)
        {
            imgDoorIcon.sprite = iconDoorOpen;
            btnDoor.GetComponent<Image>().color = colorDoorOpen;
        }
    }

    void GameOver()
    {
        isGameActive = false;
        StopAllCoroutines();

        if (GameSessionData.IsInfinityMode)
        {
            float multiplier = 1.0f;
            if (levelMultipliers.ContainsKey(GameSessionData.SelectedLevel))
            {
                multiplier = levelMultipliers[GameSessionData.SelectedLevel];
            }

            int finalScore = Mathf.RoundToInt(sessionScore * multiplier);
            if (txtDeathScore_Infinity != null) txtDeathScore_Infinity.text = finalScore.ToString();

            int highScore = PlayerPrefs.GetInt("Global_HighScore_CatchWord", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("Global_HighScore_CatchWord", highScore);
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
        StopAllCoroutines();

        // SAVE SISTEMI
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

        // Olasi durdurulmus zamani normale cevir
        Time.timeScale = 1;

        int nextIndex = GameSessionData.CurrentLevelIndex + 1;

        // Eger 5. unite levelinde degil isek bir sonraki levele gec
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

    public void Button_PauseGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        isGameActive = false;

        pnlPause.SetActive(true);
        Time.timeScale = 0; // Zamaný durdur

        // Panel baslangic konumu ve boyutu
        pnlPauseWindow.localPosition = new Vector3(-800f, 1500f, 0f);
        pnlPauseWindow.localScale = Vector3.zero;

        pnlPauseWindow.DOKill();

        // Orta noktaya hareket + boyut islemleri. SetUpdate(true) ile zaman donuk olsa bile animasyonun devam etmesi saglanir.
        pnlPauseWindow.DOLocalMove(Vector3.zero, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.one, pauseAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Button_ResumeGame()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        pnlPauseWindow.DOKill();

        // Sol ust koseye hareket + boyut islemleri. SetUpdate(true) ile zaman donuk olsa bile animasyonun devam etmesi saglanir.
        pnlPauseWindow.DOLocalMove(new Vector3(-800f, 1500f, 0f), pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true);
        pnlPauseWindow.DOScale(Vector3.zero, pauseAnimDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            // Panel kapanma ve zamani geri getirme islemleri
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
        totalCorrectCatches = 0;
        Time.timeScale = 1;
        SceneController.Instance.LoadScene("CatchWord");
    }

    public void Button_ReturnToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        sessionScore = 0;
        sessionCorrectCount = 0;
        totalCorrectCatches = 0;
        Time.timeScale = 1;
        SceneController.Instance.LoadScene(SceneController.Instance.sceneNameBeforeNewSceneLoad);
    }
}