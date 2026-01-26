using UnityEngine;

public class SuccesfulSound : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AudioClip SuccesfullSFX;

    private void Start()
    {

        Succesfull();
    }
    public void Succesfull()
    {
        SoundManager.Instance.PlaySFX(SuccesfullSFX);
    }
}
