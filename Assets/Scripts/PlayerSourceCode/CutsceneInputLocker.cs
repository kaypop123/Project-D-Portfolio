using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;

public class CutsceneInputLocker : MonoBehaviour
{
    PlayerInput _playerInput;

    void OnEnable()
    {
        TryBindPlayer();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        _playerInput = null;
        TryBindPlayer();
    }

    void TryBindPlayer()
    {
        if (_playerInput != null) return;


        _playerInput = FindFirstObjectByType<PlayerInput>();


    }


    public void LockInput()
    {
        TryBindPlayer();
        if (_playerInput != null) _playerInput.DeactivateInput();
    }

    public void UnlockInput()
    {
        TryBindPlayer();
        if (_playerInput != null) _playerInput.ActivateInput();
    }
}
