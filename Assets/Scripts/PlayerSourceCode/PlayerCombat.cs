using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAnimationBinder))]
public class PlayerCombat : MonoBehaviour
{
    private PlayerAnimationBinder animBinder;
    private PlayerMovement movement;
    private Animator anim;

    [Header("Combo")]
    [SerializeField] private int maxCombo = 3;
    private int comboCounter = 0;
    private bool isAttacking = false;
    private bool saveAttack = false;

    [Header("Failsafe (sec)")]
    [SerializeField] private float attackFailSafe = 1.2f;
    private Coroutine failSafeCo;
    private int attackToken = 0; // FailSafe 무력화 토큰

    // 캔슬 윈도우 (애니메이션 이벤트로 On/Off)
    public bool CanCancelNow { get; private set; }
    public bool IsAttacking => isAttacking;

    private StatusSystem status;

    [Header("Awaken Attack AfterImage")]
    [SerializeField] private CharacterTransform characterTransform;
    [SerializeField] private SpriteRenderer sprite;   // 몸통 스프라이트
    [SerializeField] private float attackAfterImageInterval = 0.03f;
    [SerializeField] private float attackAfterImageLifetime = 0.18f;

    private Coroutine attackAfterImageRoutine;

    void Awake()
    {
        animBinder = GetComponent<PlayerAnimationBinder>();
        movement = GetComponent<PlayerMovement>();
        status = GetComponent<StatusSystem>();
        anim = GetComponent<Animator>();

        if (characterTransform == null)
            characterTransform = GetComponent<CharacterTransform>();
        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
     if(movement.IsGrounded)
        { anim.SetBool("AirAttackUsed", false); }
    }


    // InputSystem Attack 액션
    /// <summary>
    /// Unity Input System으로부터 공격 입력을 받아 처리합니다.
    /// 상태에 따라 즉시 공격을 실행하거나 다음 콤보를 예약합니다.
    /// </summary>
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        // 1. 입력의 중복 실행 방지 (버튼을 누른 시점에만 실행)
        if (!ctx.performed) return;

        // 2. [예외 처리] 구르기(회피) 중에는 공격 입력을 무시하여 액션 간 우선순위 보장
        if (movement.IsRolling) return;

        // 3. [공중 공격 제한] 공중 공격은 착지 전까지 1회만 가능하도록 제한 (에어 콤보 밸런싱)
        if (!movement.IsGrounded && anim.GetBool("AirAttackUsed")) return;

        // 4. 공격 로직 분기
        if (!isAttacking)
        {
            // 현재 공격 중이 아니라면 즉시 공격 시퀀스 시작
            Attack();
        }
        else
        {
            // 이미 공격 중이라면 다음 타수를 예약(Input Buffering)하여 
            // 플레이어에게 부드러운 콤보 조작감 제공
            saveAttack = true;
        }
    }

    private void Attack()
    {
        isAttacking = true;
        comboCounter = (comboCounter % Mathf.Max(1, maxCombo)) + 1;

        saveAttack = false;
        CanCancelNow = false;
        animBinder.PlayAttack(comboCounter);

        movement.SetInputLocked(true);

        // FailSafe 시작
        attackToken++;
        if (failSafeCo != null) StopCoroutine(failSafeCo);
        failSafeCo = StartCoroutine(FailSafeCo(attackToken));
        FaceLockOn();

        if (!movement.IsGrounded)
            anim.SetBool("AirAttackUsed", true);

        if (characterTransform != null &&
            characterTransform.IsAwakened)
        {
            if (attackAfterImageRoutine != null)
                StopCoroutine(attackAfterImageRoutine);

            attackAfterImageRoutine = StartCoroutine(AttackAfterImageCo());
        }
    }

    // === 애니메이션 이벤트 ===
    public void OpenCancelWindow() => CanCancelNow = true;
    public void CloseCancelWindow() => CanCancelNow = false;

    // 콤보 체크 이벤트(막타 직전 or 전이 구간)
    /// <summary>
    /// 애니메이션 이벤트(Animation Event)를 통해 특정 프레임에서 호출됩니다.
    /// 입력 예약(saveAttack) 여부를 확인하여 다음 콤보로 전환하거나 상태를 종료합니다.
    /// </summary>
    public void ComboCheck()
    {
        // 1. [입력 예약 확인] 공격 도중 추가적인 입력(OnAttack)이 발생했는지 체크
        if (saveAttack)
        {
            // 예약된 입력이 있다면 다음 타수(comboCounter 증가 등)를 연계하여 실행
            Attack();
            return; // 콤보 전환이 완료되었으므로 함수 종료
        }

        // 2. [상태 정리] 예약된 입력이 없다면 공격 시퀀스를 종료하고 캐릭터의 조작권 복구
        EndAttackState();
    }
    // 안전 종료(클립 마지막 프레임 보장 호출)
    public void AttackEndCleanup() => EndAttackState();

    // === Movement에서 호출: 캔슬 시도 ===
    public bool TryCancelToJump()
    {
        if (!isAttacking || !CanCancelNow) return false;
        EndAttackState();
        animBinder.CrossFadeTo("JumpBT", 0.05f); // 상태명은 프로젝트에 맞게
        movement.TryJump();
        return true;
    }

    public bool TryCancelToRoll()
    {
        if (!isAttacking || !CanCancelNow) return false;

        // (중복 방지 가드)
        if (_cancelConsumed) return false;
        _cancelConsumed = true;

        // 공격 상태 먼저 정리
        EndAttackState();

        // 중요: 여기서는 애니 직접 재생하지 말고, Movement에게만 맡기기
        return movement.TryRoll();
    }

    private bool _cancelConsumed = false;

    // 내부 정리 (공격 종료/캔슬/FailSafe 전부 여기로 통일)
    private void EndAttackState()
    {
        // FailSafe 무효화
        attackToken++;
        if (failSafeCo != null) { StopCoroutine(failSafeCo); failSafeCo = null; }

        isAttacking = false;
        saveAttack = false;
        CanCancelNow = false;

        movement.SetInputLocked(false);
        animBinder.ForceExitAttack();

        comboCounter = 0;
        _cancelConsumed = false;
        FaceLockOff();

        if (attackAfterImageRoutine != null)
        {
            StopCoroutine(attackAfterImageRoutine);
            attackAfterImageRoutine = null;
        }
    }
    // 애니메이션 이벤트에서 호출
    public void FaceLockOn()
    {
        movement.LockFacing(true, movement.LastDirection);
    }

    public void FaceLockOff()
    {
        movement.LockFacing(false);
    }
    // FailSafe: 애니메이션 이벤트가 누락되었을 때만 실행
    private IEnumerator FailSafeCo(int token)
    {
        yield return new WaitForSeconds(attackFailSafe);

        // 토큰 체크 → 이미 정상 종료됐다면 무시
        if (token != attackToken || !isAttacking) yield break;

        EndAttackState();
    }
    private IEnumerator AttackAfterImageCo()
    {
        // isAttacking && IsAwakened 동안만 반복
        while (isAttacking &&
               characterTransform != null &&
               characterTransform.IsAwakened)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(attackAfterImageInterval);
        }
    }

    //  질문에서 준 잔상 생성 코드 적용 버전
    private void CreateAfterImage()
    {
        if (sprite == null) return;

        GameObject afterImage = new GameObject("AttackAfterImage");
        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();

        sr.sprite = sprite.sprite;
        sr.flipX = sprite.flipX;
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder = sprite.sortingOrder - 1;

        // 색감(연한 회색톤)
        sr.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        afterImage.transform.position = transform.position;
        afterImage.transform.localScale = transform.localScale;

        StartCoroutine(FadeAndDestroy(sr));
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer sr)
    {
        float t = 0f;
        float life = attackAfterImageLifetime;
        Color startColor = sr.color;

        while (t < life)
        {
            if (sr == null) yield break;
            float ratio = t / life;
            float alpha = Mathf.Lerp(1f, 0f, ratio);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            t += Time.deltaTime;
            yield return null;
        }

        if (sr != null)
        {
            Destroy(sr.gameObject);
        }
    }

    // (선택) 애니 이벤트용 히트박스 함수
}
