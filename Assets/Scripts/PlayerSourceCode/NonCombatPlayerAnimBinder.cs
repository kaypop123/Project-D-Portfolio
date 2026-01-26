using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(999)]
[RequireComponent(typeof(NonCombatPlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NonCombatPlayerAnimBinder : MonoBehaviour
{
    Animator anim;
    Rigidbody2D rb;
    NonCombatPlayerMovement move;

    // Animator Hash
    static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    static readonly int HashDirX = Animator.StringToHash("DirX");
    static readonly int HashYVel = Animator.StringToHash("YVel");
    static readonly int HashIsFalling = Animator.StringToHash("IsFalling");
    static readonly int HashIsWalking = Animator.StringToHash("IsWalking");

    [Header("Falling Hysteresis")]
    [SerializeField] float fallEnterV = -0.15f;
    [SerializeField] float fallExitV = 0.05f;

    [Header("Facing Param (Optional)")]
    public string paramFacingRight = "FacingRight";
    bool hasFacingRightParam;
    int hashFacingRight;

    bool isFalling = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        move = GetComponent<NonCombatPlayerMovement>();

        // Animator에 FacingRight 파라미터가 있는지 체크
        foreach (var p in anim.parameters)
        {
            if (p.name == paramFacingRight)
            {
                hasFacingRightParam = true;
                hashFacingRight = Animator.StringToHash(paramFacingRight);
                break;
            }
        }
    }

    void LateUpdate()
    {
        ApplyGrounded();
        ApplyMovement();
        ApplyFallingState();
        ApplyFacingDirection();
    }

    void ApplyGrounded()
    {
        anim.SetBool(HashIsGrounded, move.IsGrounded);
    }

    void ApplyMovement()
    {
        float dir = (move.LastDirection < 0f) ? -1f : +1f;

        // Combat과 동일하게 DirX에 -1 또는 +1을 그대로 넣기
        anim.SetFloat(HashDirX, dir, 0f, 0f);

        // 걷는 중인지 판정
        bool isWalking = Mathf.Abs(move.GetMoveInput().x) > 0.01f;
        anim.SetBool(HashIsWalking, isWalking);

        anim.SetFloat(HashYVel, rb.linearVelocity.y);
    }

    void ApplyFallingState()
    {
        float y = rb.linearVelocity.y;

        if (!isFalling && y < fallEnterV)
            isFalling = true;
        else if (isFalling && y > fallExitV)
            isFalling = false;

        anim.SetBool(HashIsFalling, isFalling);
    }

    void ApplyFacingDirection()
    {
        if (!hasFacingRightParam) return;

        float dir = move.LastDirection;
        anim.SetBool(hashFacingRight, dir > 0);
    }
}
