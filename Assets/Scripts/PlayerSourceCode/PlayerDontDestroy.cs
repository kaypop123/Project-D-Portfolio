using UnityEngine;

public class PlayerDontDestroy : MonoBehaviour
{
    public static PlayerDontDestroy instance;

    void Awake()
    {

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {

            Destroy(gameObject);
        }
    }
}