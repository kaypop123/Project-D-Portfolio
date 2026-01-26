using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackBox : MonoBehaviour
{
    public EnemyCoreSystem Core; // 적 루트(Core) Drag&Drop

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // 감지 전용
        if (!Core) Core = GetComponentInParent<EnemyCoreSystem>();
    }
}
