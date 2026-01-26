using UnityEngine;

/// <summary>
/// 손(Transform hand)에서 주변 대상(Rigidbody2D)을 '힘'으로만 끌어당기는 흡입 효과.
/// - "absorbTag" 태그(기본 "Soul")만 대상
/// - 원뿔 각도/반경, 시야선(벽 가림) 체크, 거리 Falloff, 속도 상한 지원
/// - Non-Alloc 방식(OverlapCircle with ContactFilter2D)
/// </summary>
public class HandSuction : MonoBehaviour
{
    [Header("Refs")]
    public Transform hand;                           // 힘을 걸 기준점(손)
    [Range(-1f, 1f)] public float facingSign = 1f;   // +1=오른쪽, -1=왼쪽

    [Header("Target Filter")]
    public LayerMask targetMask = ~0;                // 대상 레이어
    public string absorbTag = "Soul";                // 이 태그만 흡입
    public bool requireLineOfSight = true;           // 시야선(벽 가림) 체크
    public LayerMask obstacleMask;                   // 벽/지면 등 가림 레이어

    [Header("Suction Shape")]
    public float radius = 6f;                        // 유효 반경
    [Range(0f, 180f)] public float coneAngle = 90f;  // 원뿔 각도(전방만)
    public bool useCone = true;                      // false면 360도(반경만)

    [Header("Force")]
    public float suctionStrength = 25f;              // 기본 힘 계수
    public float maxPullSpeed = 12f;                 // 당길 때 속도 상한
    public float minDistance = 0.35f;                // 너무 가까울 때 폭주 방지
    public float tangentialAssist = 0.0f;            // 소용돌이 보정(접선)
    public AnimationCurve falloff =                  // 거리 비율(0=손,1=경계)→힘 가중치
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Lifecycle")]
    public bool suctionActive = false;               // 현재 흡입 중?
    public bool autoFaceFromLocalScale = true;       // 로컬스케일.x로 좌우 자동

    // ===== Non-Alloc 버퍼/필터 =====
    const int MaxHits = 64;
    readonly Collider2D[] _hits = new Collider2D[MaxHits];
    ContactFilter2D _filter;

    void Reset()
    {
        hand = transform;
        // 기본 가림 레이어(프로젝트에 맞게 교체 가능)
        obstacleMask = LayerMask.GetMask("Default", "Ground", "Walls");
    }

    void Awake()
    {
        // ContactFilter2D 구성
        _filter = new ContactFilter2D();
        _filter.useTriggers = true;          // 트리거도 대상에 포함하려면 true
        _filter.SetLayerMask(targetMask);    // 대상 레이어 적용
        _filter.useLayerMask = true;
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.01f, radius);
        minDistance = Mathf.Max(0.01f, minDistance);
        suctionStrength = Mathf.Max(0f, suctionStrength);
        maxPullSpeed = Mathf.Max(0f, maxPullSpeed);
    }

    void FixedUpdate()
    {
        if (!suctionActive || hand == null) return;

        // 좌우 자동 판정(스프라이트 플립 시 편함)
        if (autoFaceFromLocalScale)
            facingSign = Mathf.Sign(transform.lossyScale.x);

        Vector2 handPos = hand.position;
        Vector2 fwd = (facingSign >= 0f) ? Vector2.right : Vector2.left;

        // hand 주변 콜라이더 Non-Alloc 탐지
        int count = Physics2D.OverlapCircle(handPos, radius, _filter, _hits);
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col) continue;

            // 태그 필터 ("Soul"만)
            if (!col.CompareTag(absorbTag)) continue;

            var rb = col.attachedRigidbody;
            if (!rb) continue;

            // 각도/거리 계산
            Vector2 toTgt = (Vector2)col.bounds.center - handPos;
            float dist = toTgt.magnitude;
            if (dist <= 0.0001f) continue;

            // 원뿔 각 제한(전방만 흡입)
            if (useCone && coneAngle < 179.9f)
            {
                float angle = Vector2.Angle(fwd, toTgt);
                if (angle > coneAngle * 0.5f) continue;
            }

            // 시야선 체크(벽 가림 시 제외)
            if (requireLineOfSight)
            {
                var hit = Physics2D.Raycast(handPos, toTgt.normalized, dist, obstacleMask);
                if (hit.collider != null && hit.collider.transform != col.transform)
                    continue;
            }

            // 거리 비율 Falloff
            float t = Mathf.Clamp01(dist / radius);
            float fall = Mathf.Clamp01(falloff.Evaluate(t));

            // 힘 방향: 손을 향해
            Vector2 dirToHand = (handPos - (Vector2)col.bounds.center);
            float d = Mathf.Max(minDistance, dirToHand.magnitude); // 과가속 방지
            Vector2 dir = dirToHand / d;

            // 속도 상한
            Vector2 vel = rb.linearVelocity;
            if (vel.magnitude < maxPullSpeed)
            {
                Vector2 force = dir * (suctionStrength * fall * rb.mass);

                // 접선 성분(소용돌이 느낌)
                if (tangentialAssist > 0f)
                {
                    Vector2 tangent = new Vector2(-dir.y, dir.x);
                    force += tangent * (tangentialAssist * fall * rb.mass);
                }

                rb.AddForce(force, ForceMode2D.Force);
            }
        }
    }

    // ===== 외부 제어용 간단 API =====
    public void BeginSuction() => suctionActive = true;
    public void EndSuction() => suctionActive = false;
    public void SetFacing(float sign)
    {
        autoFaceFromLocalScale = false;       
        facingSign = Mathf.Sign(sign);
    }

    // ===== 에디터 시각화 =====
    void OnDrawGizmosSelected()
    {
        if (!hand) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(hand.position, radius);

        if (useCone && coneAngle < 179.9f)
        {
            Vector2 fwd = (facingSign >= 0f) ? Vector2.right : Vector2.left;
            float half = coneAngle * 0.5f * Mathf.Deg2Rad;

            Vector2 left = Rot(fwd, half);
            Vector2 right = Rot(fwd, -half);

            Gizmos.DrawLine(hand.position, (Vector2)hand.position + left.normalized * radius);
            Gizmos.DrawLine(hand.position, (Vector2)hand.position + right.normalized * radius);
        }
    }

    static Vector2 Rot(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }
}
