using UnityEngine;

public class DeathPanelSoundEffect : MonoBehaviour
{
    public AudioClip DeathPanelSfx;

    private void Start()
    {

        PlayDeathPanel();
    }
    public void PlayDeathPanel()
    {
        SoundManager.Instance.PlaySFX(DeathPanelSfx);
    }

}
