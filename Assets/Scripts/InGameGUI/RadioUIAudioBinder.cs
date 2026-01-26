using UnityEngine;

public class RadioUIAudioBinder : MonoBehaviour
{
    [Header("Radio Clips")]
    public AudioClip radioNoiseLoop;

    private void OnEnable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StartRadioLoop(radioNoiseLoop);
    }

    private void OnDisable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopRadioLoop();
    }
}
