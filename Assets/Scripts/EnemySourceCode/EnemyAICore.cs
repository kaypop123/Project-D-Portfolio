using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAICoreRandomPatrol : MonoBehaviour
{
    public enum State { Idle, Walk, Bark, Chase, Attack , Death}

    [Header("Move")]
    public float moveSpeed = 2f;

    [Header("Patrol Durations (sec)")]
    public Vector2 idleTime = new Vector2(0.7f, 1.8f);
    public Vector2 walkTime = new Vector2(1.2f, 2.8f);

    [Header("Bark")]
    public float barkDuration = 0.6f;
    public AudioSource audioSource;
    public AudioClip barkSfx;

    [Header("Chase")]
    public float chaseSpeed = 6f;       // 추격 속도(걷기보다 빠르게)
    public float chaseAccel = 20f;      // 가속

    [Header("Attack")]
    [Tooltip("AttackPoint(빨간 원) 중심에서 이 2D 거리 이하면 공격 전환")]
    public float attackRange = 1.5f;    // AttackPoint 기준 거리
    public float attackCooldown = 1.2f; // 공격 쿨타임(공격 애니 길이 겸용)

    [Header("Attack Point")]
    [Tooltip("공격 기준 위치(빈 오브젝트). 설정하면 이 월드 위치를 기준으로 거리 체크/기즈모 표시")]
    public Transform attackPoint;
    [Tooltip("AttackPoint가 없을 때 사용할 로컬 기준점 (적 오브젝트 로컬 기준)")]
    public Vector2 fallbackAttackLocal = new Vector2(0.6f, 0f);
    [Tooltip("Flip 시 AttackPoint를 X축으로 미러링할지")]
    public bool autoMirrorAttackPointX = true;
    [Tooltip("Flip 시 X에 살짝 더해줄 추가 오프셋(+우향 / -좌향)")]
    public float flipXExtraOffset = 0.0f;

    [Header("Sense")]
    public Transform player;            // 없으면 Awake에서 Player 태그로 찾음
    public float detectRadius = 6f;     // 짖기 시작 반경

    [Header("Runtime (read-only)")]
    [SerializeField] private State state = State.Idle;
    [SerializeField] private int moveDir = 0; // -1 / 0 / +1

    // === private ===
    private Rigidbody2D rb;
    private Coroutine patrolBrain;
    private bool isBarking = false;
    private float lastAttackTime = -999f;

    // 공격 포인트 로컬 기준 저장(미러링용)
    private Vector3 _attackPointBaseLocal;
    // 현재 바라보는 방향(+1 우 / -1 좌)
    private int _facingSign = 1;

    // === 외부에서 읽기용 ===
    public State CurrentState => state;
    public float CurrentVX => rb ? rb.linearVelocity.x : 0f;
    public bool IsWalking => (state == State.Walk);
    public bool IsRunning => (state == State.Chase);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!player)
        {
            var pObj = GameObject.FindGameObjectWithTag("PlayerHurtPoint");
            if (pObj) player = pObj.transform;
        }

        // AttackPoint 로컬 기준 저장 (없으면 fallback 사용)
        if (attackPoint)
            _attackPointBaseLocal = attackPoint.localPosition;
        else
            _attackPointBaseLocal = new Vector3(fallbackAttackLocal.x, fallbackAttackLocal.y, 0f);
    }

    void OnEnable()
    {
        patrolBrain = StartCoroutine(PatrolBrain());
    }

    void OnDisable()
    {
        if (patrolBrain != null) StopCoroutine(patrolBrain);
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (state == State.Death) return;
        if (!player) return;

        // 현재 바라보는 방향 갱신(걷기/추격 중엔 moveDir 따라감, 나머지는 유지)
        if (state == State.Walk || state == State.Chase)
        {
            if (moveDir != 0) _facingSign = moveDir;
        }

        // AttackPoint 미러/오프셋 적용
        UpdateAttackPointTransform();

        // 플레이어가 반경 안에 들어오면 짖기 시작(Idle/Walk 중일 때)
        if ((state == State.Idle || state == State.Walk) && !isBarking)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            if (dist <= detectRadius) StartCoroutine(BarkCo());
        }

        switch (state)
        {
            case State.Idle:
            case State.Walk:
                PatrolTick();
                break;

            case State.Chase:
                ChaseTick();
                break;

            case State.Bark:
            case State.Attack:
                // 짖기/공격 중엔 이동 멈춤
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }
    }

    // ===== 순찰 브레인: Idle ↔ Walk 루프, 고우선 상태(Bark/Chase/Attack) 땐 대기 =====
    IEnumerator PatrolBrain()
    {
        yield return null; // 첫 프레임 대기

        while (true)
        {
            // 고우선 상태 동안 대기
            yield return new WaitUntil(() => state == State.Idle || state == State.Walk);

            // ---- Idle ----
            state = State.Idle;
            moveDir = 0;
            float idleDur = Random.Range(idleTime.x, idleTime.y);
            float t = 0f;
            while (t < idleDur && (state == State.Idle || state == State.Walk))
            {
                if (state == State.Bark || state == State.Chase || state == State.Attack) break;
                t += Time.deltaTime;
                yield return null;
            }
            if (state == State.Bark || state == State.Chase || state == State.Attack) continue;

            // ---- Walk ----
            state = State.Walk;
            moveDir = (Random.value < 0.5f) ? -1 : 1;
            // 걷기 시작할 때 방향 갱신
            _facingSign = moveDir != 0 ? moveDir : _facingSign;

            float walkDur = Random.Range(walkTime.x, walkTime.y);
            t = 0f;
            while (t < walkDur && (state == State.Walk || state == State.Idle))
            {
                if (state == State.Bark || state == State.Chase || state == State.Attack) break;
                t += Time.deltaTime;
                yield return null;
            }
            // 이후 루프 재시작
        }
    }

    // ===== 틱 처리 =====
    void PatrolTick()
    {
        float vx = moveDir * moveSpeed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void ChaseTick()
    {
        if (!player) { state = State.Idle; return; }

        // 추격 이동
        float dx = player.position.x - transform.position.x;
        int dir = dx >= 0f ? 1 : -1;
        moveDir = dir;

        float targetVx = dir * chaseSpeed;
        float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, chaseAccel * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);

        // 공격 전환은 AttackPoint 기준으로 체크
        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);

        if (distToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            StartCoroutine(AttackCo());
    }

    // ===== 짖기 / 공격 =====
    IEnumerator BarkCo()
    {
        isBarking = true;

        state = State.Bark;
        moveDir = 0;
        rb.linearVelocity = Vector2.zero;

        if (audioSource && barkSfx) audioSource.PlayOneShot(barkSfx);
        // Bark 애니 트리거는 AnimeAnimation에서 상태 변화를 보고 쏨

        yield return new WaitForSeconds(barkDuration);

        // 짖기 끝나면 끝까지 추격
        state = State.Chase;
        isBarking = false;
    }

    IEnumerator AttackCo()
    {
        state = State.Attack;
        lastAttackTime = Time.time;

        // 공격 애니 트리거
        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.SetTrigger("Attack");

        // 공격 도중 잠깐 멈췄다가 다시 추격
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(attackCooldown);

        if (player) state = State.Chase;
        else state = State.Idle;
    }

    // ===== AttackPoint 유틸 =====
    void UpdateAttackPointTransform()
    {
        if (!autoMirrorAttackPointX) return;

        // 기준 로컬 위치 결정(AttackPoint가 없으면 잠시 transform 아래 임시 기준 사용)
        if (!attackPoint)
        {
            // 에디터에서 AttackPoint를 안 넣었을 때도 동작하도록 transform 하위 기준으로 계산
            // (씬상에 보이는 오브젝트는 없지만, 거리 체크/기즈모는 fallback 위치로 동작)
            _attackPointBaseLocal = new Vector3(fallbackAttackLocal.x, fallbackAttackLocal.y, 0f);
            return;
        }

        float mirroredX = Mathf.Abs(_attackPointBaseLocal.x) * _facingSign;
        mirroredX += (_facingSign > 0 ? +Mathf.Abs(flipXExtraOffset) : -Mathf.Abs(flipXExtraOffset));

        attackPoint.localPosition = new Vector3(
            mirroredX,
            _attackPointBaseLocal.y,
            _attackPointBaseLocal.z
        );
    }

    Vector2 GetAttackOrigin()
    {
        if (attackPoint) return attackPoint.position;
        // AttackPoint 미지정 시, 로컬 기준을 미러링해서 월드로 변환
        Vector3 local = _attackPointBaseLocal;
        float mirroredX = Mathf.Abs(local.x) * _facingSign;
        mirroredX += (_facingSign > 0 ? +Mathf.Abs(flipXExtraOffset) : -Mathf.Abs(flipXExtraOffset));
        local.x = mirroredX;

        return (Vector2)(transform.TransformPoint(local));
    }


    public void NotifyDeath()
    {
        Debug.Log("2. NotifyDeath() 함수 호출됨! 상태를 Death로 변경합니다.");
        state = State.Death;
        rb.linearVelocity = Vector2.zero;

        // 순찰 로직 코루틴을 확실하게 중지
        if (patrolBrain != null)
        {
            StopCoroutine(patrolBrain);
        }

    }
    // ===== 기즈모: 탐지/공격 반경 표시 (공격은 AttackPoint 기준) =====
    void OnDrawGizmosSelected()
    {
        // detectRadius(초록)
        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        // 공격 범위(빨강) - AttackPoint 기준
        Vector2 center = Application.isPlaying ? GetAttackOrigin()
                                               : (attackPoint ? (Vector2)attackPoint.position
                                                              : (Vector2)(transform.position + (Vector3)fallbackAttackLocal));
        Gizmos.color = new Color(1f, 0f, 0f, 0.65f);
        Gizmos.DrawWireSphere(center, attackRange);

        // 디버그 라인
        if (player)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawLine(center, player.position);
        }
    }
}
