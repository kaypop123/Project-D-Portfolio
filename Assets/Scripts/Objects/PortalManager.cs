using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;

    private Vector2 nextSpawnPosition;
    private bool shouldSpawn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetNextSpawn(Vector2 position)
    {
        nextSpawnPosition = position;
        shouldSpawn = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldSpawn) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = nextSpawnPosition;
        }

        shouldSpawn = false;
    }
}
