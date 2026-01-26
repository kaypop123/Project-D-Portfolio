using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerHurtbox2D : MonoBehaviour
{
    [Header("FX")]
    public HitImpactManager fx;

    [Header("Filter")]
    public string attackTag = "EnemyAttack";
    public LayerMask attackLayers = ~0;

    [Header("Spam Guard")]
    public float perAttackerCooldown = 0.08f;

    [Header("Hit Point Options")]
    public bool usePosAsHitPoint = false;
    public bool lockYToPos = true;
    public float yOffset = 0f;
    public bool clampToSelfBounds = true;

    [Header("Camera Shake")]
    public CameraShaker cameraShaker;

    // ===== Invincibility(i-frames) & blink =====
    [Header("Invincibility (i-frames)")]
    public float invincibleTime = 2f;
    public float blinkInterval = 0.1f;
    [Range(0f, 1f)] public float blinkAlpha = 0.35f;
    public SpriteRenderer[] blinkRenderers;

    // ===== Stun on Hit (movement lock) =====
    [Header("Stun On Hit")]
    [Tooltip("맞았을 때 일정 시간 조작/이동을 막습니다.")]
    public bool applyStun = true;
    [Tooltip("스턴 지속 시간(초)")]
    public float stunDuration = 0.5f;
    [Tooltip("스턴 시작 시 수평 속도를 0으로 만듭니다.")]
    public bool zeroHorizontalVelocityOnStart = true;
    [Tooltip("스턴 시간 동안 X 이동을 물리적으로 고정(필요할 때만 사용)")]
    public bool freezeXDuringStun = false;

    // ===== Knockback =====
    [Header("Knockback")]
    public bool applyKnockback = true;
    public float knockbackForce = 6f;

    // -------------------------------------------------

    private readonly Dictionary<Transform, float> _lastHitTime = new();
    private Collider2D _selfCol;

    // invincible state
    bool _invincible = false;
    Coroutine _invCo;
    Color[] _originalColors;

    // stun state & component references
    PlayerMovement _move;
    Rigidbody2D _rb;
    PlayerAnimationBinder _binder; // 애니메이션 바인더 캐시
    Coroutine _stunCo;
    bool _wasMoveEnabled = true;
    RigidbodyConstraints2D _rbConstraintsBackup;

    [Header("Health System")]
    [Tooltip("플레이어의 메인 체력 시스템 컴포넌트")]
    public StatusSystem healthSystem;

    void Awake()
    {
        _selfCol = GetComponent<Collider2D>();
        _move = GetComponent<PlayerMovement>();
        _rb = GetComponent<Rigidbody2D>();
        _binder = GetComponent<PlayerAnimationBinder>(); // 시작 시 바인더를 찾아 캐싱

        if (blinkRenderers == null || blinkRenderers.Length == 0)
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        _originalColors = new Color[blinkRenderers.Length];
        for (int i = 0; i < blinkRenderers.Length; i++)
            _originalColors[i] = blinkRenderers[i] ? blinkRenderers[i].color : Color.white;
    }

    void OnDisable()
    {
        // blink/iframe reset
        RestoreSpriteColors();
        _invincible = false;
        if (_invCo != null) StopCoroutine(_invCo);
        _invCo = null;

        // stun reset
        StopStunImmediate();
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (_invincible) return;
        if (healthSystem != null && healthSystem.IsDead) return;
        if (!string.IsNullOrEmpty(attackTag) && !other.CompareTag(attackTag)) return;
        if ((attackLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // DamageAmount 검색
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

        // === A) 데미지 먼저, 사망 여부 기록 ===
        bool diedThisHit = false;
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damageDealer.damageAmount);
            diedThisHit = healthSystem.IsDead;
            // 사망이어도 '연출'(FX/카메라)은 아래에서 계속 진행한다!
        }
        // === A-추가) 스크린 혈흔 FX ===
        float sev = Mathf.Clamp(damageDealer.damageAmount / 25f, 0.4f, 2.0f);
        BloodScreenFX.Instance?.ShowHit(sev);


        // === B) FX/카메라: 죽어도 항상 실행 ===
        Vector2 basePos = fx && fx.pos ? (Vector2)fx.pos.position : (Vector2)transform.position;
        Vector2 hitPoint = other.ClosestPoint(basePos);
        if (lockYToPos) hitPoint.y = basePos.y + yOffset;
        if (clampToSelfBounds && _selfCol != null)
        {
            var b = _selfCol.bounds;
            hitPoint.x = Mathf.Clamp(hitPoint.x, b.min.x, b.max.x);
            hitPoint.y = Mathf.Clamp(hitPoint.y, b.min.y, b.max.y);
        }
        Vector2 attackOrigin = other.bounds.center;
        fx?.PlayFromOriginAt(attackOrigin, hitPoint);   // ← 피/스파크 등 (죽어도 재생)
        if (cameraShaker) cameraShaker.StartShake();    // ← 카메라 흔들림 (죽어도 재생)

        // 공격 방향 계산(넉백/애니 참고)
        var hitDir = ((Vector2)transform.position - attackOrigin).normalized;

        // === C) 사다리 인터럽트는 항상 (죽었을 때도 붙어있지 않도록)
        GetComponent<PlayerClimb>()?.OnHitInterrupt();

        // === D) 사망 히트면 나머지 생존 전용 로직 스킵 ===
        if (diedThisHit)
        {
            // 죽을 때 점프/스턴/무적/넉백/허트 애니는 건너뜀
            // (죽음 애니는 PlayerDeathHandler.OnDie에서 처리)
            return;
        }

        // === E) 생존 시에만: 허트 애니, 무적, 점프 리셋, 넉백, 스턴 ===
        _binder?.PlayHurt(hitDir.x);

        StartInvincibility();

        _move?.ResetJump();

        if (applyKnockback && _rb)
        {
            Vector2 knockDir = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
            knockDir.y = Mathf.Max(knockDir.y, 0.5f);
            knockDir.Normalize();

            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (applyStun) StartStun(stunDuration);
    }


    // ===== Invincibility =====
    public void StartInvincibility()
    {
        if (_invCo != null) StopCoroutine(_invCo);
        _invCo = StartCoroutine(InvincibilityCo());
    }

    IEnumerator InvincibilityCo()
    {
        _invincible = true;
        float t = 0f;
        bool lowAlpha = false;

        while (t < invincibleTime)
        {
            lowAlpha = !lowAlpha;
            ApplySpriteAlpha(lowAlpha ? blinkAlpha : 1f);
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        RestoreSpriteColors();
        _invincible = false;
        _invCo = null;
    }

    void ApplySpriteAlpha(float a)
    {
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            var sr = blinkRenderers[i];
            if (!sr) continue;
            Color c = _originalColors != null && i < _originalColors.Length ? _originalColors[i] : sr.color;
            c.a = Mathf.Clamp01(a);
            sr.color = c;
        }
    }

    void RestoreSpriteColors()
    {
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            var sr = blinkRenderers[i];
            if (!sr) continue;
            if (_originalColors != null && i < _originalColors.Length) sr.color = _originalColors[i];
            else { var c = sr.color; c.a = 1f; sr.color = c; }
        }
    }

    // ===== Stun =====
    public void StartStun(float duration)
    {
        if (_stunCo != null) StopCoroutine(_stunCo);
        _stunCo = StartCoroutine(StunCo(duration));
    }

    IEnumerator StunCo(float duration)
    {
        // [수정됨] 스턴 시작 시 애니메이션 바인더에 상태 알림
        if (_binder) _binder.SetStunState(true);

        // 입력/이동 컴포넌트 비활성
        if (_move)
        {
            _wasMoveEnabled = _move.enabled;
            _move.enabled = false;
        }

        // 수평속도 0으로 (넉백 후 바로 적용되지 않도록 순서 조정)
        if (zeroHorizontalVelocityOnStart && _rb)
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        // [수정됨] 물리 프레임을 한번 기다려 넉백과 점프 입력 충돌 방지
        yield return new WaitForFixedUpdate();

        // X축 물리 고정
        if (freezeXDuringStun && _rb)
        {
            _rbConstraintsBackup = _rb.constraints;
            _rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        }

        yield return new WaitForSeconds(duration);

        // [수정됨] 스턴 종료 시 애니메이션 바인더에 상태 알림
        if (_binder) _binder.SetStunState(false);

        // 복구
        if (_move) _move.enabled = _wasMoveEnabled;
        if (freezeXDuringStun && _rb) _rb.constraints = _rbConstraintsBackup;

        _stunCo = null;
    }

    void StopStunImmediate()
    {
        if (_stunCo != null) StopCoroutine(_stunCo);
        _stunCo = null;

        if (_move) _move.enabled = true; // 강제 활성화
        if (freezeXDuringStun && _rb) _rb.constraints = _rbConstraintsBackup;

        // [수정됨] 즉시 중단 시에도 애니메이션 바인더 상태 복구
        if (_binder) _binder.SetStunState(false);
    }

    public void IFrameOn()
    {
        _invincible = true;
    }

    public void IFrameOff()
    {
        _invincible = false;
        RestoreSpriteColors(); // 혹시 투명도나 깜빡임 리셋
    }

}