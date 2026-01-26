// SaveDebugBinder.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveDebugBinder : MonoBehaviour
{
    [SerializeField] int slot = 0;
    [SerializeField] bool loadSceneIfDifferent = false;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveManager.Instance.Save(slot);
            Debug.Log($"[Test] Saved slot {slot}");
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SaveManager.Instance.Load(slot, loadSceneIfDifferent);
            Debug.Log($"[Test] Loaded slot {slot}");
        }
    }
}
