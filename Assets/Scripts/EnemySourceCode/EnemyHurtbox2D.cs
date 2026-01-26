using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHurtbox2D : MonoBehaviour
{
    [Header("FX")]
    public HitImpactManager fx;              // ← 적 몸에 붙인 HitImpactManager 드래그

    [Header("Filter")]
    public string attackTag = "PlayerAttack";
    public LayerMask attackLayers = ~0;

    [Header("Spam Guard")]
    public float perAttackerCooldown = 0.08f;

    [Header("Hit Point Options")]
    public bool usePosAsHitPoint = false;
    public bool lockYToPos = true;
    public float yOffset = 0f;
    public bool clampToSelfBounds = true;

    [Header("Camera Shake (optional)")]
    public CameraShaker cameraShaker;        // 카메라 연출 쓰면 넣고, 아니면 비워도 됨

    // ===== Stun on Hit =====
    [Header("Stun On Hit")]
    public bool applyStun = true;
    public float stunDuration = 0.3f;
    public bool zeroHorizontalVelocityOnStart = true;
    public bool freezeXDuringStun = false;

    // ===== Knockback =====
    [Header("Knockback")]
    public bool applyKnockback = true;
    public float knockbackForce = 5f;

    // -------------------------------------------------
    private readonly Dictionary<Transform, float> _lastHitTime = new();
    private Collider2D _selfCol;

    // enemy refs
    Rigidbody2D _rb;
    Animator _anim;                  // 적 애니메이터 트리거만 씀("Hurt" 권장)
    Coroutine _stunCo;
    RigidbodyConstraints2D _rbConstraintsBackup;

    [Header("Health System")]
    public EnemyHealthSystem healthSystem;

    void Awake()
    {
        _selfCol = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponentInChildren<Animator>(); // 자식에 있어도 찾기 쉬움
                                                    // 인스펙터에서 직접 할당 안했을 경우, 싱글톤 인스턴스를 찾아 할당
        if (cameraShaker == null && Camera.main != null)
        {
            cameraShaker = Camera.main.GetComponent<CameraShaker>();
        }
    }

    void OnDisable()
    {
        StopStunImmediate();
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // 허트박스는 Trigger 권장
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (healthSystem != null && healthSystem.IsDead) return;
        if (!string.IsNullOrEmpty(attackTag) && !other.CompareTag(attackTag)) return;
        if ((attackLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // 공격자가 데미지 정보를 제공해야 함 (플레이어 공격 쪽에 DamageAmount 붙이기)
        DamageAmount damageDealer = null;
        if (!other.TryGetComponent(out damageDealer))
            damageDealer = other.GetComponentInParent<DamageAmount>();
        if (damageDealer == null) return;

        // 스팸 가드
        Transform attacker = other.attachedRigidbody ? other.attachedRigidbody.transform
                                                     : other.transform.root;
        float now = Time.time;
        if (_lastHitTime.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
            return;
        _lastHitTime[attacker] = now;

        // === A) 데미지 먼저
        bool diedThisHit = false;
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damageDealer.damageAmount);
            diedThisHit = healthSystem.IsDead;
        }
        HitStop.I?.Do(0.06f);
        // === B) FX/카메라 (사망해도 연출은 재생)
        Vector2 basePos = fx && fx.pos ? (Vector2)fx.pos.position : (Vector2)transform.position;
        Vector2 hitPoint = usePosAsHitPoint ? basePos : other.ClosestPoint(basePos);
        if (lockYToPos) hitPoint.y = basePos.y + yOffset;
        if (clampToSelfBounds && _selfCol != null)
        {
            var b = _selfCol.bounds;
            hitPoint.x = Mathf.Clamp(hitPoint.x, b.min.x, b.max.x);
            hitPoint.y = Mathf.Clamp(hitPoint.y, b.min.y, b.max.y);
        }
        Vector2 attackOrigin = other.bounds.center;

        fx?.PlayFromOriginAt(attackOrigin, hitPoint);
        cameraShaker?.StartShake();

        // 방향(넉백/허트 애니 참고)
        var hitDir = ((Vector2)transform.position - attackOrigin).normalized;

        // 죽었으면 생존 로직 스킵
        if (diedThisHit) return;

        // === C) 생존 시: 허트 애니, 넉백, 스턴
        if (_anim) _anim.SetTrigger("Hurt");

        if (applyKnockback && _rb)
        {
            // 공격이 날아온 '가로 방향'만 기준으로 넉백 방향 결정
            // (히트박스/공격체의 높이(Y)와 무관하게 항상 수평 넉백)
            float dir = Mathf.Sign(transform.position.x - attackOrigin.x);
            if (dir == 0f) dir = Mathf.Sign(transform.position.x - attacker.position.x);
            if (dir == 0f) dir = 1f; // 동일 x일 때 오른쪽으로 디폴트

            Vector2 knockDir = new Vector2(dir, 0f); //  수평 100%

            // X 관성 제거(연속 히트 때 누적 방지), Y는 유지
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (applyStun) StartStun(stunDuration);
    }

    // ===== Stun =====
    public void StartStun(float duration)
    {
        if (_stunCo != null) StopCoroutine(_stunCo);
        _stunCo = StartCoroutine(StunCo(duration));
    }

    IEnumerator StunCo(float duration)
    {
        if (zeroHorizontalVelocityOnStart && _rb)
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        yield return new WaitForFixedUpdate();

        if (freezeXDuringStun && _rb)
        {
            _rbConstraintsBackup = _rb.constraints;
            _rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        }

        yield return new WaitForSeconds(duration);

        if (freezeXDuringStun && _rb) _rb.constraints = _rbConstraintsBackup;
        _stunCo = null;
    }

    void StopStunImmediate()
    {
        if (_stunCo != null) StopCoroutine(_stunCo);
        _stunCo = null;

        if (freezeXDuringStun && _rb) _rb.constraints = _rbConstraintsBackup;
    }
}
