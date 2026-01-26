using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioClip mainMenuBgm;

    void Start()
    {
        // 메인메뉴 씬일 때만 재생
        if (SceneManager.GetActiveScene().name == "MainScene")
        {
            SoundManager.Instance.PlayBGM(mainMenuBgm);
        }
    }

    void OnDestroy()
    {
        // 씬이 바뀔 때 BGM 정지
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }
}
