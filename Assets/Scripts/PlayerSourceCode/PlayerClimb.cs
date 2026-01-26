using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClimb : MonoBehaviour
{
    [Header("Refs")]
    public NonCombatPlayerMovement movement;
    public Animator animator;

    [Header("Climb Settings")]
    public float climbSpeed = 4f;
    public float detachJumpForce = 8f;
    public bool snapToCenterX = true;
    public float topExitYOffset = 0.05f;
    public float climbHorizontalControl = 0f;
    public LayerMask groundLayer;
    public float groundCheckDist = 0.1f;
    private Collider2D col;


    [Header("Hit Interrupt")]
    [Tooltip("피격 시 사다리에서 떨어질지 여부")]
    public bool dropOnHit = true;
    [Tooltip("피격 후 이 시간 동안은 사다리에 다시 붙을 수 없음")]
    public float reattachLockDuration = 0.35f;
    [Tooltip("피격 시 위로 치솟는 중이면 최소 이 값까지 Y속도를 내림(음수=아래로)")]
    public float hitFallYVelocity = -4f;

    private Rigidbody2D rb;
    private Ladder currentLadder;

    private float defaultGravity;
    private Vector2 moveInput;

    private bool canClimb;
    private bool isClimbing;

    // 재부착 금지 타이머
    private float _reattachUnlockAt = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!movement) movement = GetComponent<NonCombatPlayerMovement>();
        if (!animator) animator = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;
    }

    private void Update()
    {
        // 재부착 잠금 중이면 사다리 시작 금지
        bool lockReattach = Time.time < _reattachUnlockAt;

        if (!isClimbing && canClimb && !lockReattach && Mathf.Abs(moveInput.y) > 0.1f)
        {
            StartClimb();
        }

        if (isClimbing)
        {
            CheckTopBottom();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            ClimbMovement();
        }
    }
    public void OnClimb(InputAction.CallbackContext context)
    {
        moveInput.y = context.ReadValue<float>();
    }
    #region Input

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isClimbing)
        {
            // 사다리에서 점프 탈출
            StopClimb();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, detachJumpForce);
        }
    }
    #endregion

    #region Climb Core
    private void StartClimb()
    {
        isClimbing = true;

        if (movement) movement.enabled = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (snapToCenterX && currentLadder != null)
        {
            transform.position = new Vector3(
                currentLadder.CenterX,
                transform.position.y,
                transform.position.z
            );
        }
    }

    private void StopClimb()
    {
        isClimbing = false;

        if (movement) movement.enabled = true;

        rb.gravityScale = defaultGravity;
    }

    private void ClimbMovement()
    {
        float vy = moveInput.y * climbSpeed;
        float vx = climbHorizontalControl == 0f ? 0f : moveInput.x * climbHorizontalControl;

        rb.linearVelocity = new Vector2(vx, vy);

        if (Mathf.Abs(moveInput.y) < 0.05f && Mathf.Approximately(climbHorizontalControl, 0f))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void CheckTopBottom()
    {
        if (!currentLadder) return;

        // 꼭대기
        if (transform.position.y >= currentLadder.TopY)
        {
            transform.position = new Vector3(
                transform.position.x,
                currentLadder.TopY + topExitYOffset,
                transform.position.z
            );
            StopClimb();
            return;
        }
            if (moveInput.y < 0f)
            {
                // 내 콜라이더의 발바닥 위치에서 아주 조금 더 아래로 레이를 쏨
                float rayDist = (col.bounds.extents.y) + groundCheckDist;

                // Bounds.center를 쓰는 이유: 피벗이 발바닥이 아닌 중앙에 있을 경우를 대비
                RaycastHit2D hit = Physics2D.Raycast(col.bounds.center, Vector2.down, rayDist, groundLayer);

                // 바닥 레이어에 닿았다면 사다리 타기 종료
                if (hit.collider != null)
                {
                    Debug.Log("바닥 감지됨: 사다리 종료");
                    StopClimb();
                    return;
                }
            }
        

      
    }

    #endregion

    #region Trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        var ladder = other.GetComponent<Ladder>();
        if (!ladder) return;

        // 재부착 잠금 중이면 무시
        if (Time.time < _reattachUnlockAt) return;

        canClimb = true;
        currentLadder = ladder;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var ladder = other.GetComponent<Ladder>();
        if (!ladder || ladder != currentLadder) return;

        canClimb = false;
        if (isClimbing)
            StopClimb();
        currentLadder = null;
    }
    #endregion

    private void UpdateAnimator()
    {
        if (!animator) return;
        animator.SetBool("isClimbing", isClimbing);
        animator.SetFloat("climbSpeed", Mathf.Abs(rb.linearVelocity.y));
    }

    // ==== 외부에서 피격 시 호출 ====
    public void OnHitInterrupt()
    {
        // 재부착 잠금 시작
        _reattachUnlockAt = Time.time + reattachLockDuration;

        if (!dropOnHit) return;

        // 사다리 상태면 즉시 해제 + 아래로 떨어뜨림
        if (isClimbing)
        {
            StopClimb();

            // 위로 상승 중이면 잘라내고, 최소 하강 속도로 설정
            float newVy = rb.linearVelocity.y;
            if (newVy > 0f) newVy = 0f;
            newVy = Mathf.Min(newVy, hitFallYVelocity); // hitFallYVelocity가 더 음수면 더 빨리 떨어짐

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVy);
        }

        // 트리거 안에 계속 서 있어도 바로 다시 붙지 않도록
        canClimb = false;
        currentLadder = null;
    }
}
