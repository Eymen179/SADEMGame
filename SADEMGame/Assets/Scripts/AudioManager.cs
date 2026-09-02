using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource audioSource;
    [SerializeField] private List<AudioClip> audioSounds;


    public bool isSoundOn = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            isSoundOn = PlayerPrefs.GetInt("SoundSetting", 1) == 1;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void PlayAudioClip(string audioName)
    {

        if (!isSoundOn) return;

        foreach (AudioClip clip in audioSounds)
        {
            if (clip.name == audioName)
            {
                audioSource.PlayOneShot(clip);
                break; 
            }
        }
    }
    public void ToggleSound(bool soundState)
    {
        isSoundOn = soundState;

        PlayerPrefs.SetInt("SoundSetting", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isSoundOn && audioSource != null)
        {
            audioSource.Stop();
        }
    }
}