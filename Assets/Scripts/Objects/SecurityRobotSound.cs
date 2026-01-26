using UnityEngine;

public class SecurityRobotSound : MonoBehaviour
{
    public AudioClip WalkSfx;
    public AudioClip DetectSfx;
    public void PlayWalk()
    {
        SoundManager.Instance.PlaySFX(WalkSfx);
    }
    public void PlayDetect()
    {
        SoundManager.Instance.PlaySFX(DetectSfx);
    }
}
