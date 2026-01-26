using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneSignalBridge : MonoBehaviour
{
    private CharacterTransform _ct;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Rebind();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        _ct = null;
        Rebind();
    }

    void Rebind()
    {
        if (_ct != null) return;


        _ct = FindFirstObjectByType<CharacterTransform>();


    }

    public void Signal_ForceInfiniteAwakening()
    {
        Rebind();
        if (_ct != null) _ct.Signal_ForceInfiniteAwakening();
    }

    public void Signal_StopFreezeAndRelease()
    {
        Rebind();
        if (_ct != null) _ct.Signal_StopFreezeAndRelease();
    }
}
