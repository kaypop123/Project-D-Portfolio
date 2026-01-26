using UnityEngine;

public class PaperSoundEffect : MonoBehaviour
{
    public AudioClip PaperPanelSfx;

    private void Start()
    {

        PlayPaperPanel();
    }
    public void PlayPaperPanel()
    {
        SoundManager.Instance.PlaySFX(PaperPanelSfx);
    }

}
