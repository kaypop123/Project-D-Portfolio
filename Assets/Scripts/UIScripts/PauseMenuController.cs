using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // 인스펙터에서 패널 연결
    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true); 
        Time.timeScale = 0f;        
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;         
        isPaused = false;
    }
}