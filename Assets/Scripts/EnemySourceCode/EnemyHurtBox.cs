using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHurtBox : MonoBehaviour
{
    public EnemyCoreSystem Core;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;                       // 허트박스는 트리거
        if (!Core) Core = GetComponentInParent<EnemyCoreSystem>();
    }

    void OnTriggerEnter2D(Collider2D other) => Core.TryHurt(other);
    void OnTriggerStay2D(Collider2D other) => Core.TryHurt(other);
}
