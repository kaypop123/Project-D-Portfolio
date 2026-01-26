using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Radio Sources")]
    public AudioSource radioLoopSource; 
    public AudioSource radioSfxSource;  

    private const float DEFAULT_VOLUME = 0.1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM", DEFAULT_VOLUME);
        float sfx = PlayerPrefs.GetFloat("SFX", DEFAULT_VOLUME);

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);


        if (radioLoopSource != null) radioLoopSource.volume = sfx;
        if (radioSfxSource != null) radioSfxSource.volume = sfx;
    }



    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayRadioSFX(AudioClip clip)
    {
        if (clip == null) return;
        if (radioSfxSource == null) return;
        radioSfxSource.PlayOneShot(clip);
    }


    public void StartRadioLoop(AudioClip loopClip)
    {
        if (loopClip == null) return;
        if (radioLoopSource == null) return;

        radioLoopSource.clip = loopClip;
        radioLoopSource.loop = true;

        if (!radioLoopSource.isPlaying)
            radioLoopSource.Play();
    }


    public void StopRadioLoop()
    {
        if (radioLoopSource == null) return;
        if (radioLoopSource.isPlaying)
            radioLoopSource.Stop();
    }


    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp01(value);
        bgmSource.volume = value;
        PlayerPrefs.SetFloat("BGM", value);
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);
        sfxSource.volume = value;


        if (radioLoopSource != null) radioLoopSource.volume = value;
        if (radioSfxSource != null) radioSfxSource.volume = value;

        PlayerPrefs.SetFloat("SFX", value);
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }


}
