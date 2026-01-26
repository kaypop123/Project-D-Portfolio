using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerAnimationBinder : MonoBehaviour
{
    Animator anim;
    Rigidbody2D rb;
    PlayerMovement move;

    [Header("Component Refs")]
    public HandSuction handSuction;

    static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    static readonly int HashDirX = Animator.StringToHash("DirX");
    static readonly int HashYVel = Animator.StringToHash("YVel");
    static readonly int HashIsFalling = Animator.StringToHash("IsFalling");
    static readonly int HashHurt = Animator.StringToHash("Hurt");
    static readonly int HashDie = Animator.StringToHash("Die");
    static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    static readonly int HashAttack = Animator.StringToHash("Attack");
    static readonly int HashAttackCombo = Animator.StringToHash("AttackCombo");

    [Header("Falling Hysteresis")]
    [SerializeField] float fallEnterV = -0.15f;
    [SerializeField] float fallExitV = 0.05f;

    [Header("Hurt Direction Options")]
    public bool useSplitHurtTriggers = false;
    public string hurtTrigger = "Hurt";
    public string hurtRightTrigger = "Hurt_R";
    public string hurtLeftTrigger = "Hurt_L";

    [Header("Roll Direction Options")]
    public bool useSplitRollTriggers = false;
    public string rollTrigger = "Roll";
    public string rollRightTrigger = "Roll_R";
    public string rollLeftTrigger = "Roll_L";

    [Tooltip("Animator에 FacingRight(bool) 파라미터가 있다면 추가로 세팅")]
    public string paramFacingRight = "FacingRight";
    bool hasFacingRightParam;
    int _hashFacingRight;

    bool wasGrounded = true;
    bool isFalling;
    HashSet<int> _paramSet;

    private bool _isStunned = false;

    [Header("Suction Options")]
    public bool suctionUsesBool = true;
    public string suctionBoolParam = "IsSuction";
    public string suctionStartTrigger = "SuctionStart";
    public string suctionStopTrigger = "SuctionStop";
    public bool suctionUsesDirection = true;
    int _hashSuctionBool;

    private bool _isRolling = false; // 롤 상태 캐시

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        move = GetComponent<PlayerMovement>();

        _paramSet = new HashSet<int>();
        foreach (var p in anim.parameters) _paramSet.Add(p.nameHash);

        _hashFacingRight = Animator.StringToHash(paramFacingRight);
        hasFacingRightParam = !string.IsNullOrEmpty(paramFacingRight) && _paramSet.Contains(_hashFacingRight);

        _hashSuctionBool = Animator.StringToHash(suctionBoolParam);

        if (handSuction == null) handSuction = GetComponentInChildren<HandSuction>();
    }

    public void SetStunState(bool isStunned) => _isStunned = isStunned;

    void LateUpdate()
    {
        if (_isStunned) return;

        bool grounded = move.IsGrounded;
        float vy = rb ? rb.linearVelocity.y : 0f;
        float dir = (move.LastDirection < 0f) ? -1f : +1f;

        // 낙하 히스테리시스
        if (!grounded)
        {
            if (!isFalling && vy <= fallEnterV) isFalling = true;
            else if (isFalling && vy >= fallExitV) isFalling = false;
        }
        else isFalling = false;

        anim.SetBool(HashIsGrounded, grounded);
        anim.SetBool(HashIsFalling, isFalling);
        anim.SetFloat(HashDirX, dir, 0f, 0f);
        anim.SetFloat(HashYVel, vy, 0f, 0f);

        if (hasFacingRightParam) anim.SetBool(_hashFacingRight, dir > 0f);
        if (handSuction != null) handSuction.SetFacing(dir);

        if (wasGrounded && !grounded) anim.Update(0f);
        wasGrounded = grounded;

        Vector2 input = move.GetMoveInput();
        bool isWalking =
            Mathf.Abs(input.x) > 0.01f &&
            grounded &&
            !isFalling &&
            !move.InputLocked &&
            !move.IsRolling;

        anim.SetBool(HashIsWalking, isWalking);
    }

    // === 외부 호출 ===
    public void PlayHurt(float hitDirX)
    {
        bool faceRight = hitDirX > 0f;
        anim.SetFloat(HashDirX, faceRight ? 1f : -1f, 0f, 0f);
        if (hasFacingRightParam) anim.SetBool(_hashFacingRight, faceRight);

        if (useSplitHurtTriggers)
        {
            if (faceRight)
            {
                if (_paramSet.Contains(Animator.StringToHash(hurtLeftTrigger)))
                    anim.ResetTrigger(hurtLeftTrigger);
                anim.SetTrigger(hurtRightTrigger);
            }
            else
            {
                if (_paramSet.Contains(Animator.StringToHash(hurtRightTrigger)))
                    anim.ResetTrigger(hurtRightTrigger);
                anim.SetTrigger(hurtLeftTrigger);
            }
        }
        else
        {
            anim.SetTrigger(HashHurt);
        }

        anim.Update(0f);
    }

    public void PlayRoll(float rollDirX)
    {
        _isRolling = true;
        bool faceRight = rollDirX >= 0f;

        anim.SetFloat(HashDirX, faceRight ? 1f : -1f, 0f, 0f);
        if (hasFacingRightParam) anim.SetBool(_hashFacingRight, faceRight);

        if (useSplitRollTriggers)
        {
            if (faceRight)
            {
                if (_paramSet.Contains(Animator.StringToHash(rollLeftTrigger)))
                    anim.ResetTrigger(rollLeftTrigger);
                anim.SetTrigger(rollRightTrigger);
            }
            else
            {
                if (_paramSet.Contains(Animator.StringToHash(rollRightTrigger)))
                    anim.ResetTrigger(rollRightTrigger);
                anim.SetTrigger(rollLeftTrigger);
            }
        }
        else
        {
            if (_paramSet.Contains(Animator.StringToHash(rollTrigger)))
                anim.SetTrigger(rollTrigger);
        }

        anim.Update(0f);
    }

    public void SetSuctionActive(bool active, float? dirX = null)
    {
        if (suctionUsesDirection)
        {
            float useDir = dirX.HasValue ? Mathf.Sign(dirX.Value) : ((move.LastDirection < 0f) ? -1f : +1f);
            anim.SetFloat(HashDirX, useDir, 0f, 0f);
            if (hasFacingRightParam) anim.SetBool(_hashFacingRight, useDir > 0f);
        }

        if (suctionUsesBool && _paramSet.Contains(_hashSuctionBool))
        {
            anim.SetBool(_hashSuctionBool, active);
        }
        else
        {
            if (active)
            {
                if (_paramSet.Contains(Animator.StringToHash(suctionStopTrigger)))
                    anim.ResetTrigger(suctionStopTrigger);
                if (_paramSet.Contains(Animator.StringToHash(suctionStartTrigger)))
                    anim.SetTrigger(suctionStartTrigger);
            }
            else
            {
                if (_paramSet.Contains(Animator.StringToHash(suctionStartTrigger)))
                    anim.ResetTrigger(suctionStartTrigger);
                if (_paramSet.Contains(Animator.StringToHash(suctionStopTrigger)))
                    anim.SetTrigger(suctionStopTrigger);
            }
        }

        anim.Update(0f);
    }

    public void PlayAttack(int combo)
    {
        anim.SetInteger(HashAttackCombo, combo);
        anim.SetTrigger(HashAttack);
    }

    // 롤 애니 끝(애니 이벤트)
    public void OnRollAnimationEnd() => _isRolling = false;

    public void PlayDeath()
    {
        _isStunned = false;
        anim.SetTrigger(HashDie);
    }

    //  Combat이 호출하는 전이 유틸
    public void ForceExitAttack()
    {
        anim.ResetTrigger(HashAttack);
        anim.SetInteger(HashAttackCombo, 0);
        anim.Update(0f);
    }

    public void CrossFadeTo(string stateName, float fade = 0.05f)
    {
        anim.CrossFade(stateName, fade);
        anim.Update(0f);
    }
}
