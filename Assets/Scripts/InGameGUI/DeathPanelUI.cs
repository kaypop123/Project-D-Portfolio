using UnityEngine;
using UnityEngine.InputSystem;

public class DeathPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject bloodImage;
    [SerializeField] private GameObject blackImage;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private StatusSystem status;
    void Awake()
    {
        blackImage.SetActive(false);
        bloodImage.SetActive(false);
    }
    void OnEnable()
    {
        if (status != null)
            status.OnDie += Show;
    }

    void OnDisable()
    {
        if (status != null)
            status.OnDie -= Show;
    }
    public void Show()
    {
        bloodImage.SetActive(true);
        blackImage.SetActive(true);


        if (playerInput != null)
            playerInput.enabled = false;

        Time.timeScale = 1f;
    }

    public void Hide()
    {
        Time.timeScale = 1f;

        if (playerInput != null)
            playerInput.enabled = true;

        bloodImage.SetActive(false);
    }
}