using UnityEngine;

public class DoorOpenTriiger : MonoBehaviour
{
    private Animation anime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anime = GetComponent<Animation>();

    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!anime.isPlaying)
            {
                anime.Play();
            }
        }
    }
}
