using UnityEngine;

public class objSfx : MonoBehaviour
{
    public AudioClip SirenSfx;
    public AudioClip DoorOpenSfx;
    public AudioClip ResearcherWalkSfx;
    public AudioClip FailSfx;
    public AudioClip bloodSfx;
    public AudioClip lightSfx;
    public AudioClip BubbleSfx;
    public AudioClip GlassCrashSfx;
    public AudioClip GlassGoodCrashSfx;
    public AudioClip AttackSfx;
    public AudioClip SucceseSfx;
    public AudioClip HeartSfx;
    public AudioClip SecuritySucessSfx;
    // Start is called once before the first execution of Update after the MonoBehaviour is created




    public void PlaySiren()
    {
        SoundManager.Instance.PlaySFX(SirenSfx);
    }
    public void DoorOpen()
    {
        SoundManager.Instance.PlaySFX(DoorOpenSfx);
    }
    public void ResearcherWalk()
    {
        SoundManager.Instance.PlaySFX(ResearcherWalkSfx);
    }
    public void fail ()
    {
        SoundManager.Instance.PlaySFX(FailSfx);
    }
    public void blood()
    {
        SoundManager.Instance.PlaySFX(bloodSfx);
    }
    public void Light()
    {
        SoundManager.Instance.PlaySFX(lightSfx);
    }
    public void Bubble ()
    {
        SoundManager.Instance.PlaySFX(BubbleSfx);
    }
    public void GlassCrash()
    {
        SoundManager.Instance.PlaySFX(GlassCrashSfx);
    }
    public void GlassGoodCrash()
    {
        SoundManager.Instance.PlaySFX(GlassGoodCrashSfx);
    }
    public void Attack()
    {
        SoundManager.Instance.PlaySFX(AttackSfx);
    }
    public void Succese()
    {
        SoundManager.Instance.PlaySFX(SucceseSfx);
    }

    
            public void Heart()
    {
        SoundManager.Instance.PlaySFX(HeartSfx);
    }
    public void SecuritySucess()
    {
        SoundManager.Instance.PlaySFX(SecuritySucessSfx);
    }
}
