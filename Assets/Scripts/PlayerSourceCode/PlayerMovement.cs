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
        if (_inputLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (GetComponent<PlayerSkill>().IsDashing)
            return;
        if (isRolling)
        {
            rb.linearVelocity = new Vector2(rollDirection * rollForce, rb.linearVelocity.y);
            return;
        }

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

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_isDead) return;

        // ?? 락 체크 전에 '캔슬 윈도우'로 점프 캔슬 시도
        if (combat != null && combat.TryCancelToJump()) return;

        // 평소 정책
        if (isRolling || _inputLocked) return;
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

        // ?? 락 체크 전에 '캔슬 윈도우'로 롤 캔슬 시도
        if (combat != null && combat.TryCancelToRoll()) return;

        // 평소 정책
        if (_inputLocked) return;
        TryRoll();
    }

    public bool TryRoll()
    {
        if (_isDead || _inputLocked) return false;
        if (rb.linearVelocity.y > 0.1f) return false;

        if (!canRoll || isRolling || !isGrounded) return false;

        StartCoroutine(PerformRoll());
        return true;
    }

    private IEnumerator PerformRoll()
    {
        isRolling = true;
        canRoll = false;

        rollDirection = lastDirection; // 저장
        if (animBinder != null)
            animBinder.PlayRoll(rollDirection);

        yield return new WaitForSeconds(rollDuration);

        isRolling = false;

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
