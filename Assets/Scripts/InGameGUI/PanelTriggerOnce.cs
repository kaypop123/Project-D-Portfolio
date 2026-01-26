using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PanelTriggerOnce : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private float displayTime = 1.2f;

    private bool hasShown = false;

    private PlayerInput playerInput;


    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasShown) return;

        if (collision.CompareTag("Player"))
        {
            playerInput = collision.GetComponent<PlayerInput>();
            hasShown = true;
            StartCoroutine(ShowPanelAndPause());
        }
 
    }

    private IEnumerator ShowPanelAndPause()
    {

        if (playerInput != null)
            playerInput.enabled = false;
        // 게임 정지
        Time.timeScale = 0f;

        panel.SetActive(true);

        // timeScale 무시하고 대기
        yield return new WaitForSecondsRealtime(displayTime);

        panel.SetActive(false);

        // 게임 재개
        Time.timeScale = 1f;
        if (playerInput != null)
            playerInput.enabled = true;
    }
}
