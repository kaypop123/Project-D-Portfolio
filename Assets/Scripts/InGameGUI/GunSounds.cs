using UnityEngine;

public class GunSounds : MonoBehaviour
{
    [Header("Find Settings")]
    [Tooltip("씬에서 찾을 AudioSource의 GameObject 이름 (예: RadioSfxSource)")]
    public string radioSfxSourceObjectName = "RadioSfxSource";

    [Header("Loop Clip")]
    public AudioClip gunLoopClip;

    private AudioSource radioSfxSource;

    void OnEnable()
    {
        ResolveRadioSfxSource();
        if (radioSfxSource == null || gunLoopClip == null) return;

        radioSfxSource.clip = gunLoopClip;
        radioSfxSource.loop = true;
        radioSfxSource.Play();
    }

    void OnDisable()
    {
        if (radioSfxSource == null) return;

        radioSfxSource.Stop();
        radioSfxSource.loop = false;
        radioSfxSource.clip = null;
    }

    private void ResolveRadioSfxSource()
    {
        if (radioSfxSource != null) return;


        radioSfxSource = GetComponent<AudioSource>();
        if (radioSfxSource != null) return;

        radioSfxSource = GetComponentInChildren<AudioSource>(true);
        if (radioSfxSource != null) return;


        if (!string.IsNullOrEmpty(radioSfxSourceObjectName))
        {
            GameObject go = GameObject.Find(radioSfxSourceObjectName);
            if (go != null)
            {
                radioSfxSource = go.GetComponent<AudioSource>();
                if (radioSfxSource != null) return;
            }
        }

    }
}