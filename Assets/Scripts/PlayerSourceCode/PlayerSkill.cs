using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerSkill : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashForce = 22f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 0.5f;
    public float dashInvincibleTime = 0.15f;

    [Header("AfterImage Settings")]
    public float afterImageInterval = 0.02f;
    public float afterImageLifetime = 0.25f;

    [Header("Trail Effect")]
    public TrailRenderer dashTrail;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;

    private float lastDashTime = -999f;
    private bool isDashing = false;
    private Coroutine dashRoutine;
    private Coroutine afterImageRoutine;

    private Vector2 moveInput;
    private int facing = 1;


    [Header("Slash Skill Settings")]
    public GameObject slashWavePrefab;
    public Transform slashSpawnPoint;
    public float slashSpeed = 12f;
    public float slashCoolDown = 1.2f;

    private float lastSlashTime = -999;


    [Header("Awakening Controller 설정")]
    public RuntimeAnimatorController awakeningController;

    [Header("Cooldown UI")]
    public SkillCooldownUI dashCooldownUI;
    public SkillCooldownUI slashCooldownUI;
    public bool IsDashing => isDashing;
    private PlayerMovement movement;
    private PlayerCombat combat;


    private float defaultGravity;


    [Header("Skill Requirement")]
    public int dashUnlockCost = 2;  // 대쉬 해금 소울 개수
    public int slashUnlockCost = 3; // 슬래쉬 해금 소울 개수
    private CharacterTransform charTransform; // [추가] 소울 확인용
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        charTransform = GetComponent<CharacterTransform>();
        if (dashTrail) dashTrail.emitting = false;
        defaultGravity = rb.gravityScale;
    }
    void Start()
    {

        if (dashCooldownUI != null && charTransform != null)
        {
            dashCooldownUI.Init(charTransform, dashUnlockCost);
        }

        if (slashCooldownUI != null && charTransform != null)
        {
            slashCooldownUI.Init(charTransform, slashUnlockCost);
        }
    }
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();

        if (moveInput.x > 0.1f) facing = 1;
        else if (moveInput.x < -0.1f) facing = -1;
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            TryDash();
    }

    void TryDash()
    {
        if (combat.IsAttacking)
            return;
        if (anim.runtimeAnimatorController != awakeningController)
            return;
        if (charTransform.soul < dashUnlockCost)
        {
            Debug.Log("소울이 부족하여 대쉬를 쓸 수 없습니다.");
            return;
        }

        if (isDashing) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        lastDashTime = Time.time;

        dashCooldownUI?.TriggerCoolDown(dashCooldown);

        dashRoutine = StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        if (anim) anim.SetBool("IsDashing", true);
        if (dashTrail) dashTrail.emitting = true;

        StartCoroutine(Invincible(dashInvincibleTime));

        // [수정] 로컬 변수 대신 Awake에서 저장한 defaultGravity 사용 (여기서는 0으로 만드는 것만 수행)
        rb.gravityScale = 0;

        float dashDir = movement.LastDirection;
        afterImageRoutine = StartCoroutine(SpawnAfterImages());
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0);

        yield return new WaitForSeconds(dashDuration);

        // 정상적으로 대쉬가 끝났을 때의 종료 처리
        EndDash();
    }
    public void EndDash()
    {
        // 이미 대쉬가 끝났다면 중복 실행 방지
        if (!isDashing) return;

        isDashing = false;

        // 물리값 복구
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravity; // [수정] 백업해둔 기본값으로 복구

        if (dashTrail) dashTrail.emitting = false;
        if (anim) anim.SetBool("IsDashing", false);

        // 코루틴들이 돌고 있다면 정지
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        if (afterImageRoutine != null) StopCoroutine(afterImageRoutine);
    }

    public void ForceStopDash()
    {
        EndDash();
    }
    IEnumerator SpawnAfterImages()
    {
        while (isDashing)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    void CreateAfterImage()
    {

        GameObject afterImage = new GameObject("AfterImage");
        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();


        sr.sprite = sprite.sprite;
        sr.flipX = sprite.flipX;
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder = sprite.sortingOrder - 1;


        sr.color = new Color(0.7f, 0.7f, 0.7f, 1f);


        afterImage.transform.position = transform.position;
        afterImage.transform.localScale = transform.localScale;


        StartCoroutine(FadeAndDestroy(sr));
    }

    IEnumerator FadeAndDestroy(SpriteRenderer sr)
    {
        float t = 0f;
        Color original = sr.color;

        while (t < afterImageLifetime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / afterImageLifetime);
            sr.color = new Color(original.r, original.g, original.b, alpha);

            t += Time.deltaTime;
            yield return null;
        }

        if (sr != null)
            Destroy(sr.gameObject);
    }

    public void OnSlash(InputAction.CallbackContext ctx)
    {
        if(ctx.started)
        {
            TrySlash();
        }
    }


    void TrySlash()
    {
        if (combat.IsAttacking)
            return;
        if (!movement.IsGrounded)
            return;
        if (anim.runtimeAnimatorController != awakeningController)
            return;
        if (charTransform.soul < slashUnlockCost)
        {
            Debug.Log("소울이 부족하여 슬래쉬를 쓸 수 없습니다.");
            return;
        }
        if (Time.time < lastSlashTime + slashCoolDown)
            return;
        if (isDashing) return;

        lastSlashTime = Time.time;

        slashCooldownUI?.TriggerCoolDown(slashCoolDown);

        anim.SetTrigger("Slash");

    }
    public void OnSlashShoot()
    {
        if (slashWavePrefab == null || slashSpawnPoint == null)
            return;

        GameObject wave = Instantiate(slashWavePrefab, slashSpawnPoint.position, Quaternion.identity);

        float dir = movement.LastDirection;


        SlashWave projectile = wave.GetComponent<SlashWave>();
        if (projectile != null)
            projectile.SetDirection(dir, slashSpeed);
    }


    IEnumerator Invincible(float t)
    {
        yield return new WaitForSeconds(t);
    }
}
