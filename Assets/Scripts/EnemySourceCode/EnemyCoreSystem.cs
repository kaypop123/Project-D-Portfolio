using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyCoreSystem : MonoBehaviour
{
    //--------[ 공개 변수 ]----------------------------------------------------------//
    [Header("체력")]
    public int maxHP = 30;
    public int currentHP;

    [Header("이동속도")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("패트롤 행동 시간 범위 (sec)")]
    public Vector2 idleTime = new Vector2(0.7f, 1.8f);
    public Vector2 walkTime = new Vector2(1.2f, 2.8f);

    [Header("감지")]
    public float detectRange = 5f;
    public float delayAfterDetection = 0.6f;
    public AudioSource audioSource;
    public AudioClip detectSfx;
    public LayerMask ground;
    public Transform d_ground;
    public Transform f_ground;

    [Header("공격")]
    public float attackRange = 0.27f;
    public float attackCooldown = 1.2f;
    public Transform attackRangeCenter;
    public GameObject attackBox;
    public Vector2 fallbackAttackLocal = new Vector2(0.6f, 0f);
    public GameObject attackBox2;         // ------고릴라 전용
    public GameObject attackBox3;         //
    public float attackRange2 = 0.27f;    //
    public Transform attack2RangeCenter;  //
    public float attackRange3 = 0.27f;    //
    public Transform attack3RangeCenter;  //

    [Tooltip("attackRangeCenter를 자동으로 좌/우 미러링할지")]
    public bool autoMirrorAttackRangeCenterX = true;

    [Tooltip("flip 시 X 오프셋(항상 바라보는 방향으로 더해짐)")]
    public float flipXExtraOffset = 0.0f;

    [Header("공격 타겟 필터")]
    public string attackTag = "PlayerHurtPoint";
    public LayerMask attackLayers = ~0;

    [Header("피격")]
    public float perAttackerCooldown = 0.1f;
    public bool hurtStateSkip = false;
    public Transform hurtBox;

    [Header("피격 타겟 필터(플레이어 공격 수신)")]
    public string hurtboxAcceptTag = "PlayerAttack";
    public LayerMask hurtboxAcceptLayers = ~0;

    [Header("스턴")]
    public bool applyStun = true;
    public float stunDuration = 0.5f;
    public bool zeroHorizontalVelocityOnStart = true;
    public bool freezeXDuringStun = true;

    [Header("넉백")]
    public bool applyKnockback = true;
    public float knockbackForce = 9.68f;

    [Header("피격 포인트 옵션")]
    public bool usePosAsHitPoint = false;
    public bool lockYToPos = true;
    public float yOffset = 0.39f;
    public bool clampToSelfBounds = true;

    [Header("Camera Shake (optional)")]
    public CameraShaker cameraShaker;

    [Header("씬 설정")]
    public Transform player;
    public HitImpactManager fx;

    [Header("데스 이펙트")]
    public GameObject deathEffect;
    public bool destroyOnDeath = true;
    public float deathAnimationLength = 10f;

    [Header("상태")]
    [SerializeField] protected bool s_idel;
    [SerializeField] protected bool s_walk;
    [SerializeField] protected bool s_detect;
    [SerializeField] protected bool s_chase;
    [SerializeField] protected bool s_attack;
    [SerializeField] protected bool s_death;
    [SerializeField] protected bool s_hurt;

    [SerializeField] protected string currentState = "idle";
    [SerializeField] protected int moveDir = 0; // -1 / 0 / +1
    [SerializeField] protected float lastAttackTime = -999f;

    [Header("Facing")]
    [Tooltip("스프라이트 원본이 '왼쪽'을 보고 있으면 true, '오른쪽'이면 false")]
    public bool baseFacesLeft = true;

    //--------[ 내부 ]---------------------------------------------------------------//
    protected float CurrentVX => rb ? rb.linearVelocity.x : 0f;

    protected Rigidbody2D rb;
    private Collider2D _selfCol;
    protected Animator animator;
    protected SpriteRenderer sr;

    protected string _lastState = "";

    // 베이스 로컬 포지션을 저장해두고, 매번 그 기준에서만 미러링한다(Abs 제거)
    private Vector3 _hurtBoxBaseLocal;
    private Vector3 _selfColBaseOffset;
    private Vector3 _attackRangeCenterBaseLocal;
    private Vector3 _attack2RangeCenterBaseLocal;
    private Vector3 _attack3RangeCenterBaseLocal;
    private Vector3 _dGroundBaseLocal;
    private Vector3 _fGroundBaseLocal;

    // 현재 바라보는 "월드 방향" (-1: 왼쪽, +1: 오른쪽)
    protected int _facingSign = -1;

    protected Coroutine patrolCo;
    protected Coroutine detectCo;
    protected Coroutine attackCo;
    protected Coroutine stunCo;

    private readonly Dictionary<Transform, float> _lastHurtTime = new();
    private RigidbodyConstraints2D _rbConstraintsBackup;

    protected bool IsPatrolState => currentState == "idle" || currentState == "idle2" || currentState == "walk";

    RaycastHit2D d_hit;
    RaycastHit2D f_hit;

    // baseFacesLeft 기준으로 "원본 스프라이트가 바라보는 월드방향"을 만든다
    private int BaseFaceSign => baseFacesLeft ? -1 : 1;

    // 현재 facingSign이 baseFaceSign과 같으면 1(미러 없음), 다르면 -1(미러)
    private int MirrorSign => (_facingSign == BaseFaceSign) ? 1 : -1;

    //--------[ Awake ]--------------------------------------------------------------//
    protected virtual void Awake()
    {
        currentState = "idle";
        currentHP = maxHP;

        rb = GetComponent<Rigidbody2D>();
        _selfCol = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        if (!player)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("PlayerHurtPoint");
            if (!pObj) pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }

        // 콜라이더
        if (_selfCol) _selfColBaseOffset = _selfCol.offset;
        if (hurtBox) _hurtBoxBaseLocal = hurtBox.localPosition;

        // 공격 기준점 기준 로컬 저장
        if (attackRangeCenter) _attackRangeCenterBaseLocal = attackRangeCenter.localPosition;
        else _attackRangeCenterBaseLocal = new Vector3(fallbackAttackLocal.x, fallbackAttackLocal.y, 0f);
        if (attack2RangeCenter) _attack2RangeCenterBaseLocal = attack2RangeCenter.localPosition;
        if (attack3RangeCenter) _attack3RangeCenterBaseLocal = attack3RangeCenter.localPosition;

        // 레이 포인트 기준 로컬 저장
        if (d_ground) _dGroundBaseLocal = d_ground.localPosition;
        if (f_ground) _fGroundBaseLocal = f_ground.localPosition;

        // 초기 facingSign을 "원본 스프라이트 방향"으로 세팅
        _facingSign = BaseFaceSign;

        // 초기 포인트 위치 적용
        ApplyMirrorPoints();

        if (cameraShaker == null && Camera.main != null)
            cameraShaker = Camera.main.GetComponent<CameraShaker>();
    }

    //--------[ Enable/Disable ]-----------------------------------------------------//
    protected virtual void OnEnable()
    {
        if (s_walk) patrolCo = StartCoroutine(PatrolCo());
        if (attackBox) attackBox.GetComponent<BoxCollider2D>().enabled = false;
        if (attackBox2) attackBox2.GetComponent<CircleCollider2D>().enabled = false;
        if (attackBox3) attackBox3.GetComponent<CircleCollider2D>().enabled = false;
    }

    protected virtual void OnDisable()
    {
        if (patrolCo != null) { StopCoroutine(patrolCo); patrolCo = null; }
        if (detectCo != null) { StopCoroutine(detectCo); detectCo = null; }
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
        StopStunImmediate();
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    //--------[ UPDATE ]-------------------------------------------------------------//
    protected virtual void Update()
    {
        float vx = CurrentVX;

        bool stateChanged = _lastState != currentState;

        if (s_walk) animator.SetBool("IsWalking", currentState == "walk");
        animator.SetBool("IsChase", currentState == "chase");

        if (stateChanged)
        {
            if (currentState == "detect") animator.SetTrigger("Detect");
            else if (currentState == "attack") { }
            else if (currentState == "attack1") animator.SetTrigger("Attack1");
            else if (currentState == "attack2") animator.SetTrigger("Attack2");
            else if (currentState == "attack3") animator.SetTrigger("Attack3");
            else if (currentState == "hurt") animator.SetTrigger("Hurt");
            else if (currentState == "death") animator.SetTrigger("Die");
        }

        if ((currentState == "walk" || currentState == "chase") && Mathf.Abs(vx) > 0.001f)
            ApplyFacing(vx > 0f);

        if (currentState == "detect")
            ApplyFacing(_facingSign > 0f);

        _lastState = currentState;
    }

    // 페이싱(스프라이트만)
    protected void ApplyFacing(bool lookingRight)
    {
        sr.flipX = baseFacesLeft ? lookingRight : !lookingRight;
    }

    // 기준 로컬좌표에서만 좌/우 미러링(Abs 제거)
    private void ApplyMirrorPoints()
    {
        if (!autoMirrorAttackRangeCenterX) return;

        if (hurtBox)
        {
            var p = _hurtBoxBaseLocal;
            p.x = _hurtBoxBaseLocal.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset); // 오프셋은 '바라보는 방향'으로
            hurtBox.transform.localPosition = p;
        }

        if (_selfCol)
        {
            var p = _selfColBaseOffset;
            p.x = _selfColBaseOffset.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset); // 오프셋은 '바라보는 방향'으로
            _selfCol.offset = p;
        }

        // attackRangeCenter
        if (attackRangeCenter)
        {
            var p = _attackRangeCenterBaseLocal;
            p.x = _attackRangeCenterBaseLocal.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset); // 오프셋은 '바라보는 방향'으로
            attackRangeCenter.localPosition = p;
        }

        if (attack2RangeCenter)
        {
            var p = _attack2RangeCenterBaseLocal;
            p.x = _attack2RangeCenterBaseLocal.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset); // 오프셋은 '바라보는 방향'으로
            attack2RangeCenter.localPosition = p;
        }

        if (attack3RangeCenter)
        {
            var p = _attack3RangeCenterBaseLocal;
            p.x = _attack3RangeCenterBaseLocal.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset); // 오프셋은 '바라보는 방향'으로
            attack3RangeCenter.localPosition = p;
        }

        // d_ground
        if (d_ground)
        {
            var p = _dGroundBaseLocal;
            p.x = _dGroundBaseLocal.x * MirrorSign;
            d_ground.localPosition = p;
        }

        // f_ground
        if (f_ground)
        {
            var p = _fGroundBaseLocal;
            p.x = _fGroundBaseLocal.x * MirrorSign;
            f_ground.localPosition = p;
        }
    }

    // ----------[ 패트롤 ]---------- //
    protected IEnumerator PatrolCo()
    {
        yield return null;

        bool mustFlip = false;
        bool breakWalk = false;
        int lastMove = moveDir;

        while (true)
        {
            yield return new WaitUntil(() => IsPatrolState);

            // idle
            currentState = "idle";
            moveDir = 0;
            float idleDur = Random.Range(idleTime.x, idleTime.y);
            while (idleDur > 0f && currentState == "idle")
            {
                idleDur -= Time.deltaTime;
                yield return null;
            }

            // walk
            currentState = "walk";
            if (mustFlip) moveDir = lastMove * -1;
            else moveDir = (Random.value < 0.5f) ? -1 : 1;

            // facingSign 업데이트 + 포인트 미러 적용
            SetFacingSign();

            float walkDur = Random.Range(walkTime.x, walkTime.y);
            breakWalk = false;

            while (!breakWalk && walkDur > 0f && currentState == "walk")
            {
                if (!d_hit || f_hit)
                {
                    mustFlip = true;
                    breakWalk = true;
                    lastMove = moveDir;
                }
                walkDur -= Time.deltaTime;
                yield return null;
            }
        }
    }

    protected virtual void PatrolTick()
    {
        if (rb)
        {
            float vx = moveDir * moveSpeed;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        d_hit = Physics2D.Raycast(d_ground.position, Vector2.down, 0.5f, ground);
        f_hit = Physics2D.Raycast(f_ground.position, Vector2.down, 1f, ground);

        // 감지 로직은 파생에서 구현-------------------- //
        //
        //
        //
    }

    protected int delay = 0;
    protected virtual void ChaseTick()
    {
        if (!player)
        {
            currentState = "idle";
            return;
        }

        d_hit = Physics2D.Raycast(d_ground.position, Vector2.down, 0.5f, ground);

        if (delay == 0)
        {
            LookingAtPlayer();
        }

        if (delay < 10) delay++;
        else delay = 0;

        if (rb)
        {
            float targetVx = moveDir * chaseSpeed;
            float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, 999f * Time.deltaTime);
            rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);
        }

        // 공격 전환 로직은 파생에서 구현------------------- //
        //
        //
        //
    }

    protected IEnumerator DetectCo()
    {
        if (audioSource && detectSfx) audioSource.PlayOneShot(detectSfx);

        yield return new WaitForSeconds(delayAfterDetection);

        currentState = "chase";
        detectCo = null;
    }

    protected virtual IEnumerator AttackCo()
    {
        animator.SetTrigger("Attack");

        // 공격 로직은 파생에서 구현------------ //
        //
        //
        //

        yield break;
    }

    // ==========[ 표준 피격 엔트리 ]========== //
    public void TryHurt(Collider2D other)
    {
        if (!string.IsNullOrEmpty(hurtboxAcceptTag) && !other.CompareTag(hurtboxAcceptTag)) return;
        if ((hurtboxAcceptLayers.value & (1 << other.gameObject.layer)) == 0) return;

        DamageAmount damageDealer = null;
        if (!other.TryGetComponent(out damageDealer))
            damageDealer = other.GetComponentInParent<DamageAmount>();

        int dmg = damageDealer ? damageDealer.damageAmount : 0;

        Transform attacker = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        float now = Time.time;
        if (_lastHurtTime.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
            return;
        _lastHurtTime[attacker] = now;

        Vector2 attackOrigin = other.bounds.center;

        TakeDamage(dmg, attackOrigin, attacker);
    }

    public void TakeDamage(int dmg, Vector2 attackOrigin, Transform attacker = null)
    {
        if (currentState == "death") return;

        currentHP = Mathf.Max(currentHP - dmg, 0);

        HitStop.I?.Do(0.06f);

        Vector2 basePos = (fx && fx.pos) ? (Vector2)fx.pos.position : (Vector2)transform.position;
        Vector2 hitPoint = usePosAsHitPoint ? basePos : attackOrigin;
        if (lockYToPos) hitPoint.y = basePos.y + yOffset;

        if (clampToSelfBounds && _selfCol != null)
        {
            var b = _selfCol.bounds;
            hitPoint.x = Mathf.Clamp(hitPoint.x, b.min.x, b.max.x);
            hitPoint.y = Mathf.Clamp(hitPoint.y, b.min.y, b.max.y);
        }

        fx?.PlayFromOriginAt(attackOrigin, hitPoint);
        cameraShaker?.StartShake();

        if (currentHP <= 0)
        {
            currentState = "death";
            Die();
            return;
        }

        if (!hurtStateSkip) currentState = "hurt";

        if (applyKnockback && rb && attacker != null)
        {
            float dir = Mathf.Sign(transform.position.x - attackOrigin.x);
            if (dir == 0f && attacker) dir = Mathf.Sign(transform.position.x - attacker.position.x);
            if (dir == 0f) dir = 1f;

            Vector2 knockDir = new Vector2(dir, 0f);
            MoveStop();
            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (applyStun)
        {
            if (stunCo != null) { StopCoroutine(stunCo); stunCo = null; }
            stunCo = StartCoroutine(StunCo(stunDuration));
        }
    }

    // ===== Stun =====
    protected virtual IEnumerator StunCo(float duration)
    {
        if (zeroHorizontalVelocityOnStart && rb)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForFixedUpdate();

        if (freezeXDuringStun && rb)
        {
            _rbConstraintsBackup = rb.constraints;
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        }

        yield return new WaitForSeconds(duration);

        if (freezeXDuringStun && rb) rb.constraints = _rbConstraintsBackup;

        // 스턴 종료 후 상태 복귀는 파생에서 구현---------------- //
        //
        //
        //
    }

    protected void StopStunImmediate()
    {
        if (stunCo != null) { StopCoroutine(stunCo); stunCo = null; }
        if (freezeXDuringStun && rb) rb.constraints = _rbConstraintsBackup;
    }

    // ==========[ 데스 처리 ]========== //
    protected void Die()
    {
        if (rb) rb.linearVelocity = Vector2.zero;

        if (patrolCo != null) { StopCoroutine(patrolCo); patrolCo = null; }
        if (detectCo != null) { StopCoroutine(detectCo); detectCo = null; }
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
        StopStunImmediate();

        if (deathEffect) Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (destroyOnDeath) Destroy(gameObject, deathAnimationLength);
    }

    // ===== AttackRangeCenter 유틸 ===== //
    // (기존 UpdateTransform / GetAttackOrigin 은 ApplyMirrorPoints 기반으로 동작하도록 유지)
    protected void UpdateTransform()
    {
        ApplyMirrorPoints();
    }

    protected Vector2 GetAttackOrigin()
    {
        if (attackRangeCenter) return attackRangeCenter.position;

        // fallback도 baseLocal을 기준으로 MirrorSign 적용
        Vector3 local = _attackRangeCenterBaseLocal;
        local.x = _attackRangeCenterBaseLocal.x * MirrorSign
                  + _facingSign * Mathf.Abs(flipXExtraOffset);

        return (Vector2)transform.TransformPoint(local);
    }

    // ===== 플레이어 보기 ===== //
    protected void LookingAtPlayer()
    {
        if (d_hit)
        {
            float dx = player.position.x - transform.position.x;
            int dir = dx >= 0f ? 1 : -1;
            moveDir = dir;
            SetFacingSign();
        }
    }

    // ===== 방향 갱신 ===== //
    public void SetFacingSign()
    {
        if (moveDir != 0) _facingSign = moveDir;
        ApplyMirrorPoints();
    }

    // ===== X축 정지 ===== //
    public void MoveStop()
    {
        if (rb) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ===== 기즈모 ===== //
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Vector2 center = Application.isPlaying
            ? GetAttackOrigin()
            : (attackRangeCenter ? (Vector2)attackRangeCenter.position
                                 : (Vector2)(transform.position + (Vector3)fallbackAttackLocal));
        if (center != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.65f);
            Gizmos.DrawWireSphere(center, attackRange);
        }
       
        if (attack2RangeCenter)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.65f);
            Gizmos.DrawWireSphere(attack2RangeCenter.position, attackRange2);
        }

        if (attack3RangeCenter)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.65f);
            Gizmos.DrawWireSphere(attack3RangeCenter.position, attackRange3);
        }
        

        if (player)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawLine(center, player.position);
        }

        if (d_ground)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(d_ground.position, d_ground.position + Vector3.down * 0.5f);
        }

        if (f_ground)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(f_ground.position, f_ground.position + Vector3.down * 1f);
        }
    }
}
