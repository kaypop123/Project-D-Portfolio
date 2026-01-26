using UnityEngine;
using System.Collections;

public class EnemyWormAcid : MonoBehaviour
{
    private Animator anime;

    [Header("Hit")]
    [SerializeField] LayerMask hitMask;    // Player, Ground, Wall 등    // Player, Ground, Wall 등

    [Header("Life")]
    [SerializeField] float lifeTime = 5f;

    [Header("Rotate")]
    [SerializeField] bool rotateToVelocity = true;
    [SerializeField] bool spriteFacesRight = false; // 스프라이트 기본이 오른쪽(→)을 보는지

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anime = GetComponent<Animator>();
    }

    void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Kill), lifeTime);
    }

    public void Launch(Vector2 initialVelocity)
    {
        rb.linearVelocity = initialVelocity;
        UpdateRotation();
    }

    void FixedUpdate()
    {
        if (rotateToVelocity) UpdateRotation();
    }

    void UpdateRotation()
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) return;

        // 스프라이트가 오른쪽을 보고 제작된 경우: transform.right = velocity 방향
        if (spriteFacesRight)
            transform.right = v;
        else
            transform.right = -v; // 스프라이트가 왼쪽을 보고 제작된 경우
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;

        Debug.Log($"Hit layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        if (rb) rb.linearVelocity = Vector2.zero;
        StartCoroutine(Hit());
    }

    IEnumerator Hit()
    {
        yield return null;
        rotateToVelocity = false;
        rb.bodyType = RigidbodyType2D.Static;
        anime.SetTrigger("Hit");

        yield return new WaitForSeconds(0.1f);
        Kill();

        yield return null;
    }

    void Kill()
    {
        Destroy(gameObject);
    }
}
