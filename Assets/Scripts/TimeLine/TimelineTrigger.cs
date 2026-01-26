using UnityEngine;
using UnityEngine.Playables; 

public class TimelineTrigger : MonoBehaviour
{
    public PlayableDirector director;    
    public string targetTag = "Player"; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            Debug.Log("Timeline Triggered!");
            if (director != null)
            {
                director.Play();
            }
        }
    }
}
