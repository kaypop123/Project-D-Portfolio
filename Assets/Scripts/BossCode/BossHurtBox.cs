using UnityEngine;

public class BossHurtBox : MonoBehaviour
{
    public BossActFunc Core;
    public int part;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;                       // 허트박스는 트리거
        if (!Core) Core = GetComponentInParent<BossActFunc>();
    }

    void OnTriggerEnter2D(Collider2D other) => Core.TryHurt(other, part);
    void OnTriggerStay2D(Collider2D other) => Core.TryHurt(other, part);
}
