using UnityEngine;

public class NudgeDisableOpensPortal : MonoBehaviour
{
    [Header("꺼질 때 활성화할 포탈")]
    [SerializeField] private GameObject portal;

    [Header("중복 실행 방지")]
    private bool triggered = false;

    private void OnDisable()
    {
        if (triggered) return;
        triggered = true;

        if (portal != null)
        {
            portal.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[{name}] Portal reference is missing.");
        }
    }
}
