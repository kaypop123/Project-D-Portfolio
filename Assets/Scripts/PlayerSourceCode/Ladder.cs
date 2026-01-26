using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    [Header("Optional End Points")]
    public Transform topPoint;      // 없으면 collider bounds 사용
    public Transform bottomPoint;

    [Header("Snap")]
    public float snapOffsetX = 0f;  // 사다리 중앙에서 살짝 옆으로 붙이고 싶으면

    private Collider2D col;
    public Bounds Bounds => col.bounds;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true; // 반드시 Trigger!
    }

    public float TopY => topPoint ? topPoint.position.y : Bounds.max.y;
    public float BottomY => bottomPoint ? bottomPoint.position.y : Bounds.min.y;
    public float CenterX => Bounds.center.x + snapOffsetX;
}
