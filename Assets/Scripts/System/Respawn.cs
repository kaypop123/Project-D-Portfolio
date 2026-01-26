using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Respawn : MonoBehaviour
{
    [Header("Respawn")]
    public GameObject player;
    public Transform spawn;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("Fade Settings")]
    [SerializeField] private float panelFadeInTime = 0.2f;
    [SerializeField] private float textFadeInTime = 0.1f;
    [SerializeField] private float holdTime = 0.2f;
    [SerializeField] private float fadeOutTime = 0.2f;

    private PlayerInput playerInput;

    private void Awake()
    {
        if (alertText != null)
        {
            Color c = alertText.color;
            c.a = 0f;
            alertText.color = c;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
        }

        playerInput = player.GetComponent<PlayerInput>();
    }

    public void PlayerRespawn()
    {

        if (playerInput != null)
            playerInput.enabled = false;
        Time.timeScale = 0f;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        yield return FadePanel(0f, 1f, panelFadeInTime);

        yield return new WaitForSecondsRealtime(holdTime);

        player.transform.position = spawn.position;

        yield return FadeText(0f, 1f, textFadeInTime);

        yield return new WaitForSecondsRealtime(holdTime);

        yield return FadeText(1f, 0f, fadeOutTime);

        yield return FadePanel(1f, 0f, fadeOutTime);



        if (playerInput != null)
            playerInput.enabled = true;
    }

    private IEnumerator FadeText(float start, float end, float duration)
    {
        float t = 0f;
        Color c = alertText.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            if (alertText != null)
            {
                c.a = Mathf.Lerp(start, end, lerp);
                alertText.color = c;
            }
            Time.timeScale = 1f;
            yield return null;
        }

        c.a = end;
        alertText.color = c;
    }

    private IEnumerator FadePanel(float start, float end, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            if (panelGroup != null)
                panelGroup.alpha = Mathf.Lerp(start, end, lerp);

            yield return null;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = end;
           
        }
    }
}
