using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string nextSceneName;
    public Vector2 spawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PortalManager.Instance.SetNextSpawn(spawnPosition);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
