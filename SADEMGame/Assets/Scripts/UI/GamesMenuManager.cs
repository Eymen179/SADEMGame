using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GamesMenuManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject pnlLanguageLevel;

    // YENI: Aciklama metninin yazilacagi Text objesi
    public TextMeshProUGUI txtLanguageTitle;

    [Header("Scroll Views")]
    public GameObject ScrollView_WordGames;
    public GameObject ScrollView_SentenceGames;

    [Header("Dil Seviyesi Butonlari")]
    public Button[] btnLevels;
    public TextMeshProUGUI[] txtLevels;

    private string selectedMiniGameScene;
    private string selectedMiniGamePrefix;

    public Color color_WordButtonUnselected;
    public Color color_WordButtonSelected;
    public Color color_SentenceButtonUnselected;
    public Color color_SentenceButtonSelected;

    public Image img_WordButton;
    public Image img_SentenceButton;

    [Header("Dil Seviyesi Renkleri")]
    // --- Renk Tanimlamalari ---
    public Color colorA1 = new Color(0.33f, 0.75f, 0.33f); // Yeþil
    public Color colorA2 = new Color(0.13f, 0.59f, 0.95f); // Mavi
    public Color colorB1 = new Color(0.95f, 0.76f, 0.20f); // Sarý
    public Color colorB2 = new Color(0.96f, 0.52f, 0.15f); // Turuncu
    public Color colorC1 = new Color(0.85f, 0.25f, 0.25f); // Kýrmýzý

    private Color colorMissing = Color.black;

    private Color colorTextBlack = new Color(0.1f, 0.1f, 0.1f);
    private Color colorTextWhite = Color.white;

    private string[] levelNames = { "A1", "A2", "B1", "B2", "C1" };

    void Start()
    {
        pnlLanguageLevel.SetActive(false);

        ScrollView_WordGames.SetActive(true);
        ScrollView_SentenceGames.SetActive(false);

        img_WordButton.color = color_WordButtonSelected;
        img_SentenceButton.color = color_SentenceButtonUnselected;
    }

    public void Button_OpenLanguageSelection(string gameData)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        string[] data = gameData.Split(',');
        if (data.Length == 2)
        {
            selectedMiniGameScene = data[0];
            selectedMiniGamePrefix = data[1];

            // YENI: Oyun aciklamasini guncelle
            UpdateLanguageTitleDescription(selectedMiniGameScene);

            CheckAvailableLevels();

            pnlLanguageLevel.SetActive(true);
        }
        else
        {
            Debug.LogError("Oyun verisi hatalý girildi! Format 'SahneAdý,JsonÖneki' olmalý.");
        }
    }

    // YENI: Secilen sahneye gore aciklama metnini belirleyen metot
    private void UpdateLanguageTitleDescription(string sceneName)
    {
        if (txtLanguageTitle == null) return;

        string description = "";

        // Sahne isimlerini kendi projendeki birebir isimlerle kontrol et
        switch (sceneName)
        {
            // --- KELIME OYUNLARI ---
            case "FastTyping":
                description = "Type the words as fast as you can before the time runs out.";
                break;
            case "GuessWord":
                description = "Guess the hidden word within 5 tries using color clues.";
                break;
            case "CatchWord":
                description = "Catch the correct falling words that match the given translation.";
                break;
            case "QuickMatch":
                description = "Match the correct word pairs before the time runs out.";
                break;
            case "QuickSpeak":
                description = "Pronounce the given words correctly using your microphone.";
                break;

            // --- CUMLE OYUNLARI ---
            case "ParagraphBuilder":
                description = "Select the sentences in the correct logical order to build a paragraph.";
                break;
            case "QuickFindTrue":
                description = "Identify the grammatically correct sentence among the incorrect options.";
                break;
            case "QuickFillBlank":
                description = "Choose the correct word to fill the blank in the sentence.";
                break;
            case "QuickSort":
                description = "Select the sentence parts in the correct order to form a meaningful sentence.";
                break;
            case "QuickTest":
                description = "Read the question and select the correct answer from the multiple choices.";
                break;

            default:
                description = "Play the selected mini-game to improve your language skills.";
                break;
        }

        // Aciklamanin altina sabit metni iki satir bosluk birakarak ekliyoruz
        txtLanguageTitle.text = description + " Select Your Language Level And Play";
    }

    private void CheckAvailableLevels()
    {
        for (int i = 0; i < btnLevels.Length; i++)
        {
            string targetFileName = selectedMiniGamePrefix + "_" + levelNames[i];
            TextAsset jsonFile = Resources.Load<TextAsset>(targetFileName);

            if (jsonFile != null)
            {
                btnLevels[i].interactable = true;
                ApplyLevelColor(i);
            }
            else
            {
                btnLevels[i].interactable = false;
                btnLevels[i].image.color = colorMissing;
                txtLevels[i].color = colorTextWhite;
            }
        }
    }

    private void ApplyLevelColor(int index)
    {
        switch (index)
        {
            case 0:
                btnLevels[index].image.color = colorA1;
                txtLevels[index].color = colorTextBlack;
                break;
            case 1:
                btnLevels[index].image.color = colorA2;
                txtLevels[index].color = colorTextWhite;
                break;
            case 2:
                btnLevels[index].image.color = colorB1;
                txtLevels[index].color = colorTextBlack;
                break;
            case 3:
                btnLevels[index].image.color = colorB2;
                txtLevels[index].color = colorTextBlack;
                break;
            case 4:
                btnLevels[index].image.color = colorC1;
                txtLevels[index].color = colorTextWhite;
                break;
        }
    }

    public void Button_SelectLevelAndPlay(string level)
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        GameSessionData.IsInfinityMode = true;
        GameSessionData.SelectedLevel = level;

        switch (level)
        {
            case "A1": GameSessionData.SelectedLevelColor = colorA1; GameSessionData.SelectedLevelTextColor = colorTextBlack; break;
            case "A2": GameSessionData.SelectedLevelColor = colorA2; GameSessionData.SelectedLevelTextColor = colorTextWhite; break;
            case "B1": GameSessionData.SelectedLevelColor = colorB1; GameSessionData.SelectedLevelTextColor = colorTextBlack; break;
            case "B2": GameSessionData.SelectedLevelColor = colorB2; GameSessionData.SelectedLevelTextColor = colorTextBlack; break;
            case "C1": GameSessionData.SelectedLevelColor = colorC1; GameSessionData.SelectedLevelTextColor = colorTextWhite; break;
            default: GameSessionData.SelectedLevelColor = Color.white; GameSessionData.SelectedLevelTextColor = Color.black; break;
        }

        GameSessionData.TargetJsonFile = selectedMiniGamePrefix + "_" + level;

        SceneController.Instance.LoadScene(selectedMiniGameScene);
    }

    public void Button_CloseLanguageSelection()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        pnlLanguageLevel.SetActive(false);
    }

    public void Button_BackToMainMenu()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        SceneController.Instance.LoadScene("MainMenu");
    }

    public void Button_WordGames()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        img_WordButton.color = color_WordButtonSelected;
        img_SentenceButton.color = color_SentenceButtonUnselected;

        ScrollView_WordGames.SetActive(true);
        ScrollView_SentenceGames.SetActive(false);
    }

    public void Button_SentenceGames()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");
        
        img_WordButton.color = color_WordButtonUnselected;
        img_SentenceButton.color = color_SentenceButtonSelected;

        ScrollView_WordGames.SetActive(false);
        ScrollView_SentenceGames.SetActive(true);
    }

    public void Button_A________________________Inspector()
    {
        Debug.Log("Test");
    }
    public void Button_Z________________________Inspector()
    {
        Debug.Log("Test");
    }
}