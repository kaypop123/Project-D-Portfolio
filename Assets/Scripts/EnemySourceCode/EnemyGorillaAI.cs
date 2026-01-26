using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyGorillaAI : EnemyCoreSystem
{
    public float attack3Speed = 20f;

    [Header("Jump Attack (Attack1) - Apex Based")]
    [Tooltip("타겟보다 얼마나 더 높은 최고점을 찍을지")]
    [SerializeField] private float apexAboveTarget = 4.0f;

    [Tooltip("타겟이 평지/더 아래여도, 고릴라 기준 최소한 이 높이는 뛰게")]
    [SerializeField] private float minApexAboveSelf = 2.5f;

    [Tooltip("착지점 주변(원형) 반경 안에 들어오면 충돌/바닥감지 활성")]
    [SerializeField] private float landingEnableRadius = 1.2f;

    [Tooltip("착지 판정 Ray 길이")]
    [SerializeField] private float landingRayDistance = 0.55f;

    [Header("Jump Gizmo")]
    [SerializeField] private bool jumpGiz = false;
    [SerializeField] private int gizmoSegments = 60;

    public float atk1Cool;
    public float atk2Cool;
    public float atk3Cool;

    private bool attack1Move = false;
    private bool attack3Move = false;
    private bool detectRangeAdd = true;

    // 점프 착지 제어용
    private Vector2 _landingPoint;
    public Transform groundCheck;

    // gizmo cache
    private bool _hasJumpCache = false;
    private Vector2 _jumpStart;
    private Vector2 _jumpTarget;
    private Vector2 _jumpV0;
    private float _jumpTotalTime;

    // 통과 처리(내 콜라이더만 trigger)
    private Collider2D _mainCol;
    private bool _mainColWasTrigger;

    protected override void Awake()
    {
        base.Awake();

        _mainCol = GetComponent<Collider2D>();
        if (_mainCol) _mainColWasTrigger = _mainCol.isTrigger;
    }

    protected override void Update()
    {
        base.Update();
        bool stateChanged = _lastState != currentState;

        if (currentState == "death") return;
        if (!player) return;

        UpdateTransform();

        if (currentState == "idle" || currentState == "idle2")
        {
            PatrolTick();
        }
        else if (currentState == "detect")
        {
            if (detectRangeAdd)
            {
                detectRange += 3f;
                detectRangeAdd = false;
            }

            MoveStop();
            LookingAtPlayer();
            ApplyFacing(_facingSign > 0f);

            if (patrolCo != null) { StopCoroutine(patrolCo); patrolCo = null; }

            if (detectCo == null)
                detectCo = StartCoroutine(DetectCo());
        }
        else if (currentState == "chase")
        {
            if (!attack1Move) ChaseTick();
            ApplyFacing(CurrentVX > 0f);
        }
        else if (currentState == "attack1")
        {
            if (!attack1Move) MoveStop();

            if (stateChanged)
            {
                ApplyFacing(_facingSign > 0f);
                LookingAtPlayer();
            }

            if (attackCo == null)
                attackCo = StartCoroutine(Attack1Co());

        }
        else if (currentState == "attack2")
        {
            MoveStop();
            if (stateChanged) LookingAtPlayer();

            if (attackCo == null)
                attackCo = StartCoroutine(Attack2Co());
        }
        else if (currentState == "attack3")
        {
            if (!attack3Move) MoveStop();

            if (stateChanged)
            {
                ApplyFacing(_facingSign > 0f);
                LookingAtPlayer();
            }

            if (attackCo == null)
                attackCo = StartCoroutine(Attack3Co());
        }
        else if (currentState == "hurt")
        {
            // 피격동작 없음
        }
    }

    // ------------------ Overrides ------------------ //

    protected override void PatrolTick()
    {
        if (IsPatrolState && player)
        {
            if (Vector2.Distance(player.position, transform.position) <= detectRange)
                currentState = "detect";
        }
    }

    protected override void ChaseTick()
    {
        if (attack1Move) return;
        base.ChaseTick();

        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);

        if (Physics2D.Raycast(groundCheck.position, Vector2.down, landingRayDistance, ground))
        {
            if (distToPlayer <= attackRange)
            {
                currentState = "attack2";
                delay = 0;
            }
            else if (distToPlayer >= attackRange3)
            {
                currentState = "attack1";
                delay = 0;
            }
            else if (distToPlayer >= attackRange2 && distToPlayer <= attackRange3)
            {
                currentState = "attack3";
                delay = 0;
            }
        }
    }

    // ------------------ Attack1 (Jump) ------------------ //
    public Collider2D nudge;
    protected IEnumerator Attack1Co()
    {
        lastAttackTime = Time.time;

        jumpGiz = true;
        attack1Move = true;

        // 점프 시작 시점의 플레이어 위치를 목표로 고정
        _landingPoint = (Vector2)player.position;

        yield return new WaitForSeconds(0.2f);

        JumpApexBased(); // 통과 ON

        // 시작하자마자 grounded true 방지: 일단 땅에서 떨어질 때까지
        yield return new WaitUntil(() => !IsGroundedNow());

        // 하강 시작
        // target이 위에 있어도 올라가는 중에는 절대 충돌/땅감지 켜지지 않도록 보장
        yield return new WaitUntil(() => rb.linearVelocity.y <= 0f);

        // "하강 중" + "착지점 원형 반경 진입"을 동시에 만족할 때까지 기다림
        yield return new WaitUntil(() => rb.linearVelocity.y <= 0f && IsNearLandingCircle());

        // 이제 착지 가능하게 충돌 ON
        SetPassThrough(false);

        // 착지 감지
        yield return new WaitUntil(() => IsGroundedNow());

        attack1Move = false;
        animator.SetTrigger("Down");

        yield return new WaitForSeconds(0.1f);
        // 착지 타격
        var col = attackBox ? attackBox.GetComponent<BoxCollider2D>() : null;
        if (col)
        {
            col.enabled = true;
            yield return new WaitForSeconds(0.1f);
            col.enabled = false;
        }

        yield return new WaitForSeconds(atk1Cool);

        if (Vector2.Distance(player.position, transform.position) <= detectRange)
        {
            currentState = "chase";
            LookingAtPlayer();
        }
        else
        {
            currentState = "idle2";
        }

        jumpGiz = false;
        attackCo = null;
    }

    // ------------------ Jump (Apex-Based) ------------------ //
    private void JumpApexBased()
    {
        if (!player) return;

        Vector2 start = rb.position;
        Vector2 target = _landingPoint;

        if (!TryGetVelocityByApex(start, target, apexAboveTarget, minApexAboveSelf, rb, out var v0, out var totalTime))
        {
            // 거의 실패 안 함(중력 0 등 예외). 폴백
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            float vy = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, minApexAboveSelf));
            float vx = (target.x - start.x) * 0.5f;
            v0 = new Vector2(vx, vy);
            totalTime = 0.5f;
        }

        SetPassThrough(true);
        rb.linearVelocity = v0;

        // gizmo cache
        _jumpStart = start;
        _jumpTarget = target;
        _jumpV0 = v0;
        _jumpTotalTime = Mathf.Max(0.01f, totalTime);
        _hasJumpCache = true;
    }

    /// <summary>
    /// 최고점(apex)을 강제해서 start -> target로 가는 초기속도와 총 비행시간을 계산
    /// apexY = max(target.y + apexAboveTarget, start.y + minApexAboveSelf)
    /// </summary>
    static bool TryGetVelocityByApex(
        Vector2 start, Vector2 target,
        float apexAboveTarget, float minApexAboveSelf,
        Rigidbody2D rb,
        out Vector2 v0,
        out float totalTime)
    {
        v0 = default;
        totalTime = 0f;

        float g = Mathf.Abs(Physics2D.gravity.y) * (rb ? rb.gravityScale : 1f);
        if (g < 0.0001f) return false;

        float apexY = Mathf.Max(target.y + apexAboveTarget, start.y + minApexAboveSelf);
        apexY = Mathf.Max(apexY, Mathf.Max(start.y, target.y) + 0.01f);

        // up phase
        float upH = apexY - start.y;
        float vy = Mathf.Sqrt(2f * g * upH);
        float tUp = vy / g;

        // down phase
        float downH = apexY - target.y;
        float tDown = Mathf.Sqrt(2f * downH / g);

        totalTime = tUp + tDown;
        if (totalTime < 0.0001f) return false;

        float vx = (target.x - start.x) / totalTime;

        v0 = new Vector2(vx, vy);
        return true;
    }

    // ------------------ Landing helpers ------------------ //
    private bool IsNearLandingCircle()
    {
        return Vector2.Distance(rb.position, _landingPoint) <= landingEnableRadius;
    }

    private bool IsGroundedNow()
    {
        if (!groundCheck) return false;

        return (Physics2D.Raycast(groundCheck.position, Vector2.down, landingRayDistance, ground));
    }

    private void SetPassThrough(bool on)
    {
        if (!_mainCol) return;

        if (on)
        {
            _mainColWasTrigger = _mainCol.isTrigger;
            _mainCol.isTrigger = true;

            nudge.enabled = false;
        }
        else
        {
            _mainCol.isTrigger = _mainColWasTrigger;
            nudge.enabled = true;
        }
    }

    // ------------------ Attack2 ------------------ //
    protected IEnumerator Attack2Co()
    {
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.0f);

        var c = attackBox2 ? attackBox2.GetComponent<CircleCollider2D>() : null;
        if (c) c.enabled = true;

        yield return new WaitForSeconds(0.3f);
        if (c) c.enabled = false;

        yield return new WaitForSeconds(atk2Cool);

        if (Vector2.Distance(player.position, transform.position) <= detectRange)
        {
            currentState = "chase";
            LookingAtPlayer();
        }
        else
        {
            currentState = "idle2";
        }

        attackCo = null;
    }

    // ------------------ Attack3 ------------------ //
    protected IEnumerator Attack3Co()
    {
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.2f);

        attack3Move = true;

        float vx = moveDir * attack3Speed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        var c = attackBox3 ? attackBox3.GetComponent<CircleCollider2D>() : null;
        if (c) c.enabled = true;

        yield return new WaitForSeconds(0.4f);

        attack3Move = false;
        if (c) c.enabled = false;

        yield return new WaitForSeconds(atk3Cool);

        if (Vector2.Distance(player.position, transform.position) <= detectRange)
        {
            currentState = "chase";
            LookingAtPlayer();
        }
        else
        {
            currentState = "idle2";
        }

        attackCo = null;
    }

    // ------------------ Stun ------------------ //
    protected override IEnumerator StunCo(float duration)
    {
        yield return base.StunCo(duration);

        attack1Move = false;
        attack3Move = false;
        SetPassThrough(false);

        currentState = player ? "chase" : "idle2";
        stunCo = null;
    }

    // ------------------ Gizmos ------------------ //
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * landingRayDistance);

        if (!jumpGiz) return;

        var rb2d = Application.isPlaying ? rb : GetComponent<Rigidbody2D>();
        if (!rb2d) return;
        if (!player) return;

        Vector2 start, target, v0;
        float tTotal;

        if (Application.isPlaying && _hasJumpCache)
        {
            start = _jumpStart;
            target = _jumpTarget;
            v0 = _jumpV0;
            tTotal = _jumpTotalTime;
        }
        else
        {
            start = rb2d.position;
            target = (Vector2)player.position;

            if (!TryGetVelocityByApex(start, target, apexAboveTarget, minApexAboveSelf, rb2d, out v0, out tTotal))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(start, target);
                return;
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(target, 0.2f);

        // 착지 활성 반경 표시(원)
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
        Gizmos.DrawWireSphere(target, landingEnableRadius);

        float g = Mathf.Abs(Physics2D.gravity.y) * rb2d.gravityScale;
        Vector2 acc = new Vector2(0f, -g);

        Gizmos.color = Color.green;
        Vector2 prev = start;

        int seg = Mathf.Max(8, gizmoSegments);
        float dt = Mathf.Max(0.001f, tTotal / seg);

        for (int i = 1; i <= seg; i++)
        {
            float t = dt * i;
            Vector2 p = start + v0 * t + 0.5f * acc * (t * t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(start, start + v0.normalized * 0.6f);


    }
}
