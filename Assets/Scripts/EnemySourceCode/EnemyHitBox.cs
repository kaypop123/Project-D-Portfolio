using System.Collections.Generic;
using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    [Header("FX")]
    public HitImpactManager fx;                    // HitImpactManager (pos 필수)

    [Header("Filter")]
    public string attackTag = "PlayerAttack";      // 공격 히트박스 태그
    public LayerMask attackLayers = ~0;            // 또는 레이어로 필터(둘 다 통과해야 함)

    [Header("Spam Guard")]
    public float perAttackerCooldown = 0.08f;      // 같은 공격자로부터 스팸 방지

    // 공격자별 쿨다운
    private readonly Dictionary<Transform, float> _lastHitTime = new();

    void Reset()
    {
        // 이 객체의 Collider2D를 트리거로
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (!fx || !fx.pos) return;

        // 태그 + 레이어 필터
        if (!string.IsNullOrEmpty(attackTag) && !other.CompareTag(attackTag)) return;
        if ((attackLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // 스팸 방지 (공격자 기준)
        Transform attacker = other.attachedRigidbody ? other.attachedRigidbody.transform
                                                     : other.transform.root;
        float now = Time.time;
        if (_lastHitTime.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
            return;
        _lastHitTime[attacker] = now;

        // 히트 위치(대략) 설정: 상대 콜라이더에서 적 중심에 가장 가까운 점
        Vector2 hitPoint = other.ClosestPoint(transform.position);
        fx.pos.position = hitPoint;

        // 공격자 기준으로 좌/우 플립해서 임팩트 + 데칼 출력
        fx.PlayFromAttacker(attacker);
    }
}