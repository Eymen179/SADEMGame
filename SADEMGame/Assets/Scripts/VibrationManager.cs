using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance;

    public bool isVibrationOn = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            int vibCounter = PlayerPrefs.GetInt("VibrationButtonCounter", 0);
            isVibrationOn = (vibCounter % 2 == 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Vibrate()
    {
        if (!isVibrationOn) return;

        // Editor'de iken
#if UNITY_EDITOR
        Debug.Log("[VibrationManager] Titreþim tetiklendi! (Gerçek cihazda titrer)");
#endif

        // Mobilde iken
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void ToggleVibration(bool state)
    {
        isVibrationOn = state;

        if (isVibrationOn)
        {
#if UNITY_EDITOR
            Debug.Log("[VibrationManager] Ayarlardan Titreþim AÇILDI!");
#endif

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}