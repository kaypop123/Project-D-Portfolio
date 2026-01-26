using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneController : MonoBehaviour
{

    public string SceneName;

    public void LoadScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }
}