using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings Images")]
    public Sprite spriteSoundOn;
    public Sprite spriteSoundOff;

    public Sprite spriteVibrationOn;
    public Sprite spriteVibrationOff;

    public Image buttonSoundSettings;
    public Image buttonVibrationSettings;

    private int soundButtonCounter = 0;
    private int vibrationButtonCounter = 0;

    public GameObject pnlOptions;

    void Start()
    {
        if (pnlOptions != null) pnlOptions.SetActive(false);

        // YENÝ: Oyuna baþlarken hafýzadaki deðerleri okuyoruz
        soundButtonCounter = PlayerPrefs.GetInt("SoundButtonCounter", 0);
        vibrationButtonCounter = PlayerPrefs.GetInt("VibrationButtonCounter", 0);

        // YENÝ: Okunan deðerlere göre buton görsellerini en baþtan doðru þekilde ayarlýyoruz
        UpdateInitialVisuals();
    }

    // YENÝ METOT: Butonlarýn baþlangýç görsellerini günceller (Sistemi tekrar tetiklemez, sadece görseli ayarlar)
    private void UpdateInitialVisuals()
    {
        // SES GÖRSELÝ
        if (soundButtonCounter % 2 == 0)
        {
            buttonSoundSettings.sprite = spriteSoundOn;
        }
        else
        {
            buttonSoundSettings.sprite = spriteSoundOff;
        }

        // TÝTREÞÝM GÖRSELÝ (Boyutlarýyla Birlikte)
        if (vibrationButtonCounter % 2 == 0)
        {
            buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 512f);
            buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 512f);
            buttonVibrationSettings.sprite = spriteVibrationOn;
        }
        else
        {
            buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 290f);
            buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 470f);
            buttonVibrationSettings.sprite = spriteVibrationOff;
        }
    }

    public void Button_Start()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");
        // Oyuncunun en son oynadýðý dil seviyesini hafýzadan çekiyoruz
        string lastChapter = PlayerPrefs.GetString("LastPlayedChapter", "");

        if (string.IsNullOrEmpty(lastChapter))
        {
            // Eðer kayýt yoksa (ilk defa giriyorsa) LevelsMenu'yü dümdüz aç
            SceneController.Instance.LoadScene("LevelsMenu");
        }
        else
        {
            // Kayýt varsa, LevelsMenuManager'ýn o paneli otomatik açmasý için gerekli komutu ver
            PlayerPrefs.SetString("AutoOpenChapter", lastChapter);
            SceneController.Instance.LoadScene("LevelsMenu");
        }
    }

    public void Button_Cities()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        SceneController.Instance.LoadScene("LevelsMenu");
    }

    public void Button_Games()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        SceneController.Instance.LoadScene("GamesMenu");
    }

    public void Button_Options()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        pnlOptions.SetActive(true);
    }

    public void Button_CloseOptions()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        pnlOptions.SetActive(false);
    }

    public void Button_SoundSettings()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        soundButtonCounter++;
        PlayerPrefs.SetInt("SoundButtonCounter", soundButtonCounter);

        ButtonCounter(soundButtonCounter, spriteSoundOn, spriteSoundOff, true);
    }

    public void Button_VibrationSettings()
    {
        AudioManager.Instance.PlayAudioClip("Sound_ButtonClick");

        vibrationButtonCounter++;
        PlayerPrefs.SetInt("VibrationButtonCounter", vibrationButtonCounter);

        ButtonCounter(vibrationButtonCounter, spriteVibrationOn, spriteVibrationOff, false);
    }

    private void ButtonCounter(int counter, Sprite spriteOn, Sprite spriteOff, bool isSound)
    {
        if (counter % 2 == 0) //Açma durumu
        {
            if (isSound)
            {
                buttonSoundSettings.sprite = spriteOn;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ToggleSound(true);
                }
            }
            else
            {
                buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 512f);
                buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 512f);

                buttonVibrationSettings.sprite = spriteOn;

                if (VibrationManager.Instance != null)
                {
                    VibrationManager.Instance.ToggleVibration(true);
                }
            }
        }
        else //Kapama durumu
        {
            if (isSound)
            {
                buttonSoundSettings.sprite = spriteOff;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ToggleSound(false);
                }
            }
            else
            {
                buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 290f);
                buttonVibrationSettings.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 470f);

                buttonVibrationSettings.sprite = spriteOff;

                if (VibrationManager.Instance != null)
                {
                    VibrationManager.Instance.ToggleVibration(false);
                }
            }
        }
    }
}