using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UnitConfig
{
    public string unitName;
    public LevelConfig[] levels = new LevelConfig[5]; // Her unitede 5 level
}

[System.Serializable]
public class LanguageChapterConfig
{
    public string languageName; // A1, A2...
    public Color chapterColor;
    public Color textColor;

    [Header("UI Referanslari")]
    public Button btnLanguageChapter;
    public TextMeshProUGUI txtLanguageLevel;
    public TextMeshProUGUI txtUnitCompleted;
    public Slider completedSlider;
    public GameObject objLockedImg;
    public GameObject objLockedTxt;
    public GameObject pnlChapter; // PnlChapter_A1 objesi

    [Header("Unite Icerikleri")]
    public UnitConfig[] units = new UnitConfig[6]; // Her dilde 6 unite
}

public class LevelsMenuManager : MonoBehaviour
{
    [Header("Dil Seviyeleri (Chapters)")]
    public LanguageChapterConfig[] languageChapters;
    public Color colorLockedGray = new Color(0.4f, 0.4f, 0.4f);

    [Header("Paneller")]
    public GameObject scrollViewLanguageLevels;
    public GameObject pnlUnitLevels; // Ana level paneli
    public Image imgPnlLevelsBackground; // PnlLevels arkaplani (Renk degisecek)
    public TextMeshProUGUI txtUnitLevelsTitle; // Ornegin: "Gunluk Hayat"

    [Header("Level Bilgi Paneli (YENI)")]
    public GameObject pnlLevelInfo;
    public TextMeshProUGUI txtLevelInfoTitle; // Ornegin: "Level 1"
    public TextMeshProUGUI txtMinigameTitle;  // Ornegin: "Hizli Eslestirme"
    public Image imgLevelInfoLanguage;        // Dil etiketi arkaplani
    public TextMeshProUGUI txtLevelInfoLanguage; // Dil etiketi yazisi (A1)

    [Header("Level Butonlari (PnlUnitLevels icindeki 5 buton)")]
    public Button[] btnLevels;
    public TextMeshProUGUI[] txtLevels;
    public GameObject[] imgLevelLocked;
    public GameObject[] imgLevelCompleted;

    private int activeLanguageIndex = -1;
    private int activeUnitIndex = -1;

    // YENI: PnlLevelInfo panelinde Play butonuna basilana kadar secili leveli hafizada tutar
    private int pendingLevelIndex = -1;

    // Butonlarin Inspector'da ayarlanan orijinal renklerini hafizada tutmak icin
    private Color[] originalLevelButtonColors = new Color[5];

    void Start()
    {
        // Oyun baslarken butonlarin orijinal renklerini kaydet
        for (int i = 0; i < btnLevels.Length; i++)
        {
            if (btnLevels[i] != null)
            {
                originalLevelButtonColors[i] = btnLevels[i].image.color;
            }
        }

        pnlUnitLevels.SetActive(false);

        if (pnlLevelInfo != null)
            pnlLevelInfo.SetActive(false); // Baslangicta info panelini kapali tut

        CloseAllChapterPanels();

        // Eger bir oyun bitirilip "Next Level" ile veya geri donusle menuye gelindiyse,
        // kalinan Chapter panelini otomatik acmak icin kontrol yapiyoruz.
        string autoOpenChap = PlayerPrefs.GetString("AutoOpenChapter", "");
        if (!string.IsNullOrEmpty(autoOpenChap))
        {
            PlayerPrefs.SetString("AutoOpenChapter", ""); // Sifirla
            OpenChapterPanelByName(autoOpenChap);
        }
        else
        {
            scrollViewLanguageLevels.SetActive(true);
        }

        RefreshChapterLocksAndProgress();
    }

    private void CloseAllChapterPanels()
    {
        foreach (var chapter in languageChapters)
        {
            if (chapter.pnlChapter != null)
                chapter.pnlChapter.SetActive(false);
        }
    }

    private void RefreshChapterLocksAndProgress()
    {
        bool isPreviousLanguageCompleted = true; // A1 her zaman acik

        for (int i = 0; i < languageChapters.Length; i++)
        {
            LanguageChapterConfig chap = languageChapters[i];

            // O dildeki toplam tamamlanmis level sayisini hesapla
            int totalCompletedInChapter = 0;
            int totalLevelsInChapter = chap.units.Length * 5; // 6 * 5 = 30

            for (int u = 0; u < chap.units.Length; u++)
            {
                for (int l = 0; l < chap.units[u].levels.Length; l++)
                {
                    string key = $"Completed_{chap.languageName}_{u}_{l}";
                    if (PlayerPrefs.GetInt(key, 0) == 1)
                    {
                        totalCompletedInChapter++;
                    }
                }
            }

            // UI Guncelleme
            if (chap.txtUnitCompleted != null) chap.txtUnitCompleted.text = $"{totalCompletedInChapter}/{totalLevelsInChapter} Completed";
            if (chap.completedSlider != null) chap.completedSlider.value = (float)totalCompletedInChapter / totalLevelsInChapter;

            if (isPreviousLanguageCompleted)
            {
                // KILIDI ACIK
                chap.btnLanguageChapter.interactable = true;
                chap.btnLanguageChapter.image.color = chap.chapterColor;
                if (chap.txtLanguageLevel != null) chap.txtLanguageLevel.color = chap.textColor;
                if (chap.txtUnitCompleted != null) chap.txtUnitCompleted.color = chap.textColor;

                if (chap.txtLanguageLevel != null) chap.txtLanguageLevel.gameObject.SetActive(true);
                if (chap.txtUnitCompleted != null) chap.txtUnitCompleted.gameObject.SetActive(true);
                if (chap.completedSlider != null) chap.completedSlider.gameObject.SetActive(true);

                if (chap.objLockedImg != null) chap.objLockedImg.SetActive(false);
                if (chap.objLockedTxt != null) chap.objLockedTxt.SetActive(false);
            }
            else
            {
                // KILITLI
                chap.btnLanguageChapter.interactable = false;
                chap.btnLanguageChapter.image.color = colorLockedGray;

                if (chap.txtLanguageLevel != null) chap.txtLanguageLevel.gameObject.SetActive(false);
                if (chap.txtUnitCompleted != null) chap.txtUnitCompleted.gameObject.SetActive(false);
                if (chap.completedSlider != null) chap.completedSlider.gameObject.SetActive(false);

                if (chap.objLockedImg != null) chap.objLockedImg.SetActive(true);
                if (chap.objLockedTxt != null) chap.objLockedTxt.SetActive(true);
            }

            isPreviousLanguageCompleted = (totalCompletedInChapter == totalLevelsInChapter);
        }
    }

    public void Button_OpenChapter(int langIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        scrollViewLanguageLevels.SetActive(false);
        CloseAllChapterPanels();
        languageChapters[langIndex].pnlChapter.SetActive(true);

        UpdateUnitButtonsUI(langIndex);
    }

    private void OpenChapterPanelByName(string langName)
    {
        for (int i = 0; i < languageChapters.Length; i++)
        {
            if (languageChapters[i].languageName == langName)
            {
                Button_OpenChapter(i);
                return;
            }
        }
        scrollViewLanguageLevels.SetActive(true);
    }

    private void UpdateUnitButtonsUI(int langIndex)
    {
        LanguageChapterConfig chap = languageChapters[langIndex];

        Transform unitsParent = chap.pnlChapter.transform.Find("PnlChapterUnits");
        if (unitsParent == null) return;

        for (int u = 0; u < chap.units.Length; u++)
        {
            Button btnUnit = unitsParent.GetChild(u).GetComponent<Button>();
            if (btnUnit != null)
            {
                TextMeshProUGUI txtTitle = btnUnit.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI txtComp = btnUnit.transform.Find("TxtCompleted").GetComponent<TextMeshProUGUI>();

                txtTitle.text = chap.units[u].unitName;

                int unitCompletedCount = 0;
                for (int l = 0; l < 5; l++)
                {
                    if (PlayerPrefs.GetInt($"Completed_{chap.languageName}_{u}_{l}", 0) == 1)
                        unitCompletedCount++;
                }
                txtComp.text = $"{unitCompletedCount}/5";

                int capturedLangIndex = langIndex;
                int capturedUnitIndex = u;
                btnUnit.onClick.RemoveAllListeners();
                btnUnit.onClick.AddListener(() => OpenUnitLevelsPanel(capturedLangIndex, capturedUnitIndex));
            }
        }
    }

    private void OpenUnitLevelsPanel(int langIndex, int unitIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        activeLanguageIndex = langIndex;
        activeUnitIndex = unitIndex;

        LanguageChapterConfig chap = languageChapters[langIndex];
        UnitConfig unit = chap.units[unitIndex];

        chap.pnlChapter.SetActive(false);
        pnlUnitLevels.SetActive(true);

        imgPnlLevelsBackground.color = chap.chapterColor;
        txtUnitLevelsTitle.text = unit.unitName;
        txtUnitLevelsTitle.color = chap.textColor;

        for (int l = 0; l < 5; l++)
        {
            bool isUnlocked = false;
            bool isCompleted = (PlayerPrefs.GetInt($"Completed_{chap.languageName}_{unitIndex}_{l}", 0) == 1);

            if (l == 0) isUnlocked = true;
            else isUnlocked = (PlayerPrefs.GetInt($"Completed_{chap.languageName}_{unitIndex}_{l - 1}", 0) == 1);

            txtLevels[l].text = (l + 1).ToString();

            if (isUnlocked)
            {
                btnLevels[l].interactable = true;
                btnLevels[l].image.color = originalLevelButtonColors[l];
                imgLevelLocked[l].SetActive(false);
                txtLevels[l].gameObject.SetActive(true);
            }
            else
            {
                btnLevels[l].interactable = false;
                btnLevels[l].image.color = colorLockedGray;
                imgLevelLocked[l].SetActive(true);
                txtLevels[l].gameObject.SetActive(false);
            }

            imgLevelCompleted[l].SetActive(isCompleted);
        }
    }

    // YENI: PnlUnitLevels icindeki 1,2,3,4,5 butonlarina atanan bu metot artik direkt oyunu acmak yerine Info panelini acar.
    public void Button_PlayLevel(int levelIndex)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        pendingLevelIndex = levelIndex;

        LanguageChapterConfig chap = languageChapters[activeLanguageIndex];
        UnitConfig unit = chap.units[activeUnitIndex];
        LevelConfig levelToPlay = unit.levels[levelIndex];

        // UI bilesenlerini guncelle
        if (txtLevelInfoTitle != null) txtLevelInfoTitle.text = "Level " + (levelIndex + 1).ToString();
        if (txtMinigameTitle != null) txtMinigameTitle.text = GetMiniGameNameFromScene(levelToPlay.sceneName);

        if (txtLevelInfoLanguage != null)
        {
            txtLevelInfoLanguage.text = chap.languageName;
            txtLevelInfoLanguage.color = chap.textColor;
        }

        if (imgLevelInfoLanguage != null)
        {
            imgLevelInfoLanguage.color = chap.chapterColor;
        }

        // Bilgi panelini ekranda goster
        if (pnlLevelInfo != null) pnlLevelInfo.SetActive(true);
    }

    // YENI: PnlLevelInfo panelindeki "Play" butonuna atanacak metot
    public void Button_StartPendingLevel()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (pendingLevelIndex == -1) return;

        LanguageChapterConfig chap = languageChapters[activeLanguageIndex];
        UnitConfig unit = chap.units[activeUnitIndex];
        LevelConfig levelToPlay = unit.levels[pendingLevelIndex];

        GameSessionData.IsInfinityMode = false;
        GameSessionData.SelectedLevel = chap.languageName;
        GameSessionData.SelectedLevelColor = chap.chapterColor;
        GameSessionData.SelectedLevelTextColor = chap.textColor;

        GameSessionData.CurrentUnitIndex = activeUnitIndex;
        GameSessionData.CurrentLevelIndex = pendingLevelIndex;
        GameSessionData.TargetJsonFile = levelToPlay.jsonFileName;

        GameSessionData.CurrentUnitLevels.Clear();
        GameSessionData.CurrentUnitLevels.AddRange(unit.levels);

        PlayerPrefs.SetString("LastPlayedChapter", chap.languageName);
        PlayerPrefs.Save();

        // Info panelini temizle
        if (pnlLevelInfo != null) pnlLevelInfo.SetActive(false);
        pendingLevelIndex = -1;

        SceneController.Instance.LoadScene(levelToPlay.sceneName);
    }

    // YENI: PnlLevelInfo panelindeki "Back" butonuna atanacak metot
    public void Button_CloseLevelInfo()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (pnlLevelInfo != null) pnlLevelInfo.SetActive(false);
        pendingLevelIndex = -1;
    }

    // YARDIMCI METOT: Scene adindan Mini Oyun adini Turkceye cevirir
    private string GetMiniGameNameFromScene(string sceneName)
    {
        switch (sceneName)
        {
            case "QuickSpeak": return "Quick Speak";
            case "CatchWord": return "Catch Word";
            case "FastTyping": return "Fast Typing";
            case "QuickTest": return "Quick Test";
            case "QuickSort": return "Quick Sort";
            case "GuessWord": return "Guess Word";
            case "QuickMatch": return "Quick Match";
            case "QuickFindTrue": return "Quick Find True";
            case "ParagraphBuilder": return "Paragraph Builder";
            default: return sceneName; // Eger kayitli degilse direk sahne adini yazar
        }
    }

    public void Button_BackToChapters()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (pnlLevelInfo != null) pnlLevelInfo.SetActive(false);
        pnlUnitLevels.SetActive(false);
        languageChapters[activeLanguageIndex].pnlChapter.SetActive(true);
    }

    public void Button_CloseChapter()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        if (pnlLevelInfo != null) pnlLevelInfo.SetActive(false);
        CloseAllChapterPanels();
        scrollViewLanguageLevels.SetActive(true);
    }

    public void Button_BackToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        SceneController.Instance.LoadScene("MainMenu");
    }
}