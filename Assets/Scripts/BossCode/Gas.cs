using UnityEngine;

public class Gas : MonoBehaviour
{
    private Animator anime;

    [Header("Hit")]
    [SerializeField] LayerMask hitMask;    // Player, Ground, Wall µî    // Player, Ground, Wall µî

    [Header("Life")]
    [SerializeField] float lifeTime = 10f;

    void Awake()
    {
        anime = GetComponent<Animator>();
    }

    void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Kill), lifeTime);
    }

    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;

        Debug.Log($"Hit layer: {LayerMask.LayerToName(other.gameObject.layer)}");
    }

    void Kill()
    {
        Destroy(gameObject);
    }
    
}
