using UnityEngine;

public class EffectSoundFX : MonoBehaviour
{
    public AudioClip HitSfx;
    public AudioClip ChangeSfx1;
    public AudioClip ChangeSfx2;
    public void PlayHit()
    {
        SoundManager.Instance.PlaySFX(HitSfx);
    }
    public void PlayChange1()
    {
        SoundManager.Instance.PlaySFX(ChangeSfx1);
    }
    public void PlayChange2()
    {
        SoundManager.Instance.PlaySFX(ChangeSfx2);
    }
}
