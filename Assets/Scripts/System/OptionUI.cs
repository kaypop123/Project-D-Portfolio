using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    private bool initialized = false;
    private const float DEFAULT_VOLUME = 0.5f;

    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM", DEFAULT_VOLUME);
        float sfx = PlayerPrefs.GetFloat("SFX", DEFAULT_VOLUME);

        // 초기화 중 이벤트 방지
        bgmSlider.SetValueWithoutNotify(bgm);
        sfxSlider.SetValueWithoutNotify(sfx);

        SoundManager.Instance.SetBGMVolume(bgm);
        SoundManager.Instance.SetSFXVolume(sfx);

        initialized = true;
    }

    public void OnBGMChanged(float value)
    {
        if (!initialized) return;
        SoundManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        if (!initialized) return;
        SoundManager.Instance.SetSFXVolume(value);
    }
}
