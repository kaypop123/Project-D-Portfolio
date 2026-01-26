using UnityEngine;
using UnityEngine.SceneManagement;
public class RestartButton : MonoBehaviour
{
    public void RestartCurrentScene()
    {
        if (PlayerDontDestroy.instance != null)
        {
            Destroy(PlayerDontDestroy.instance.gameObject);
            PlayerDontDestroy.instance = null;  
        }

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}
