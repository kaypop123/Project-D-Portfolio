using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Jump Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.1f;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    [SerializeField] private float lastDirection = 1f;
    public float LastDirection => lastDirection;

    [Header("Roll Settings")]
    public float rollForce = 10f;
    public float rollDuration = 0.3f;
    private bool isRolling = false;
    private bool canRoll = true;
    public bool IsRolling => isRolling;

    private bool _isDead = false;

    // 애니메이션 바인더
    private PlayerAnimationBinder animBinder;

    // 외부(공격/석션 등) 입력락
    private bool _inputLocked = false;
    public bool InputLocked => _inputLocked;

    // 캔슬 연동
    private PlayerCombat combat;

    private float rollDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animBinder = GetComponent<PlayerAnimationBinder>();
        combat = GetComponent<PlayerCombat>();
    }

    void FixedUpdate()
    {
        if (_isDead) return;
        Movement();
        CheckGround();

    }

    void Movement()
    {
        // 조건문 분기를 통해 상태별 이동 로직을 분리(Early Return 패턴 활용)
        if (_inputLocked)
        {
            rb.linearVelocity = Vector2.zero; // 물리적 정지 보장
            return;
        }

        if (GetComponent<PlayerSkill>().IsDashing) return;

        if (isRolling)
        {
            // 구르기 시 y축 속도를 유지함으로써 경사로 이동이나 점프 중 구르기 대응
            rb.linearVelocity = new Vector2(rollDirection * rollForce, rb.linearVelocity.y);
            return;
        }

        // rb.linearVelocity.y를 유지하여 중력의 영향을 받는 자유 낙하 상태에서도 
        // 수평 이동이 자연스럽게 이루어지도록 설계
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isDead) return;


        moveInput = context.ReadValue<Vector2>();

        // 방향 갱신은 그대로
        if (!_faceLocked && moveInput.x != 0)
            lastDirection = Mathf.Sign(moveInput.x);
    }
    public Vector2 GetMoveInput() => moveInput;

    /// <summary>
    /// Unity Input System을 통해 점프 입력 발생 시 호출되는 콜백 함수입니다.
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        // 1. 점프 키를 누른 시점(Started/Performed)에만 로직을 실행하도록 최적화
        if (!context.performed) return;

        // 2. 캐릭터 사망 시 모든 입력 차단
        if (_isDead) return;

        // 3. [액션 캔슬 로직] 전투 시스템(PlayerCombat)과 연동하여 공격 동작 중 
        // 특정 프레임(Cancel Window)에서 점프를 시도할 경우, 공격을 즉시 중단하고 점프를 실행
        if (combat != null && combat.TryCancelToJump()) return;

        // 4. 일반적인 상태 체크: 구르기 중이거나 다른 로직에 의해 입력이 잠긴 경우 점프 불가
        if (isRolling || _inputLocked) return;

        // 5. 최종 점프 실행 조건 검사 후 점프 수행
        TryJump();
    }

    // Combat이 직접 호출할 수 있게 공개
    public bool TryJump()
    {
        if (_isDead || isRolling) return false;
        if (!isGrounded) return false; // 공중점프 허용이면 이 로직 수정
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        return true;
    }

    public void CheckGround()
    {
        if (!groundCheck) { isGrounded = true; return; }
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_isDead) return;

        //  락 체크 전에 '캔슬 윈도우'로 롤 캔슬 시도
        if (combat != null && combat.TryCancelToRoll()) return;

        // 평소 정책
        if (_inputLocked) return;
        TryRoll();
    }

    /// <summary>
    /// 구르기 동작의 조건을 검사하고 실행 가능 여부를 판단합니다.
    /// </summary>
    public bool TryRoll()
    {
        // 1. 상태 예외 처리: 사망, 입력 잠금, 공중(상승 중) 상태에서는 구르기 불가
        if (_isDead || _inputLocked) return false;
        if (rb.linearVelocity.y > 0.1f) return false; // 점프 중 구르기 방지 (낙하 중은 허용 가능)

        // 2. 쿨타임 및 중복 실행 방지: 지면 접촉 여부와 구르기 가능 상태를 체크
        if (!canRoll || isRolling || !isGrounded) return false;

        // 3. 비동기 처리를 위한 코루틴 실행
        StartCoroutine(PerformRoll());
        return true;
    }

    /// <summary>
    /// 코루틴을 이용해 일정 시간 동안 구르기 물리와 애니메이션을 처리합니다.
    /// </summary>
    private IEnumerator PerformRoll()
    {
        isRolling = true;  // 구르기 상태 활성화 (Movement 로직에서 이동 제어에 활용)
        canRoll = false;   // 쿨타임 시작

        // 구르기 시작 시점의 방향을 고정하여 중도에 방향 전환이 불가능하도록 설계
        rollDirection = lastDirection;

        if (animBinder != null)
            animBinder.PlayRoll(rollDirection); // 애니메이션 바인더를 통한 연출 실행

        // 구르기 지속 시간 동안 대기 (물리 엔진이 Movement에서 구르기 속도를 적용함)
        yield return new WaitForSeconds(rollDuration);

        isRolling = false; // 구르기 물리 상태 종료

        // [디테일] 구르기 액션이 끝난 후 추가적인 쿨타임을 주어 무분별한 스팸 방지
        yield return new WaitForSeconds(0.5f);
        canRoll = true;
    }

    public void ResetJump() => isGrounded = false;

    public void SetDeadState()
    {
        _isDead = true;
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // 외부에서 입력/이동 락 설정
    public void SetInputLocked(bool locked, bool zeroHorizontal = true)
    {
        _inputLocked = locked;

        //  입력 자체를 0으로 만들지 말고 그대로 유지
        if (rb != null && zeroHorizontal)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
    private bool _faceLocked = false;
    private float _lockedDir = 1f;

    public void LockFacing(bool on, float? dirOverride = null)
    {
        _faceLocked = on;
        if (on)
        {
            _lockedDir = dirOverride.HasValue ? Mathf.Sign(dirOverride.Value) : lastDirection;
            lastDirection = _lockedDir; // 즉시 고정
        }
        else
        {

            if (moveInput.x != 0)
            {
                lastDirection = Mathf.Sign(moveInput.x);
            }
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
