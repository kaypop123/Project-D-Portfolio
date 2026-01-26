using UnityEngine;
using UnityEngine.UI;

public class UIImageAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 0.1f;

    private Image img;
    private int currentFrame;
    private float timer;

    void Start()
    {
        img = GetComponent<Image>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            currentFrame = (currentFrame + 1) % frames.Length;
            img.sprite = frames[currentFrame];
        }
    }
}
