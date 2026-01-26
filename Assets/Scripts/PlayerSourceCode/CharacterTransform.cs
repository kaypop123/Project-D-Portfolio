using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class CharacterTransform : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("캐릭터가 공중으로 떠오르는 데 걸리는 시간")]
    public float riseDuration = 0.5f;
    [Tooltip("캐릭터가 뜰 높이")]
    public float liftHeight = 0.5f;

    [Header("변신 임팩트 프리팹")]
    public GameObject transformEffectPrefab;
    [Header("변신 해제 이펙트 프리팹")]
    public GameObject endTransformEffectPrefab;

    [Header("각성 오브젝트 설정 (순서대로 0, 1...)")]
    public GameObject[] awakeningObjects;

    [Header("소울 설정")]
    public int maxSoul = 3;
    public int soul = 0;          // 남은 소울 개수
    public int soulCost = 1;      // 변신 시작에 필요한 최소 소울 (C키 조건)
    [Tooltip("변신 후 소울이 줄어드는 간격 (초 단위, 소울 1개당 시간)")]
    public float soulDrainInterval = 1.0f;

    [Header("무적 처리")]
    public Collider2D hurtbox;

    [Header("컴포넌트 연결")]
    public Animator animator;
    private PlayerMovement move;
    private Rigidbody2D rb;
    private PlayerSkill playerSkill;

    [Header("Animator Controllers")]
    public RuntimeAnimatorController normalController;
    public RuntimeAnimatorController transformController;
    public RuntimeAnimatorController awakenedController;

    // 상태 변수
    private bool isTransforming = false;
    public bool IsTransforming => isTransforming;

    private bool isAwakened = false;
    public bool IsAwakened => isAwakened;

    private Vector2 fixedPosition;


    bool rbSimulatedBackup;
    float rbGravityBackup;
    RigidbodyType2D rbBodyTypeBackup;


    public float SoulDrainProgress { get; private set; } = 0f;
    private float currentSoulTimer = 0f;


    private Coroutine drainCoroutine;
    private Coroutine transformCoroutine;


    [Header("Signal / Cutscene Awakening")]
    [SerializeField] private bool infiniteAwakening = false;     // 무한 각성 모드
    [SerializeField] private bool freezeTimeWhileAwakened = false; // 각성 중 시간 정지(선택)
    private float _timeScaleBackup = 1f;

    [Header("Input Gate")]
    public bool blockTransformInput = false;
    void Awake()
    {
        move = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        playerSkill = GetComponent<PlayerSkill>();

        if (awakeningObjects != null)
        {
            foreach (var obj in awakeningObjects)
                if (obj) obj.SetActive(false);
        }

        SoulDrainProgress = 0f;
        currentSoulTimer = 0f;
    }

    public void AddSoul(int amount = 1)
    {
        int add = Mathf.Max(0, amount);           
        soul = Mathf.Clamp(soul + add, 0, maxSoul); 
        UpdateAwakeningObjects();
        if (soul >= maxSoul)
        {
            currentSoulTimer = soulDrainInterval; 
            SoulDrainProgress = 1f;               
        }
    }


    void Update()
    {
        HandleSoulDrain();
    }

    /// <summary>
    /// 각성 상태일 때만 소울 시간 감소 처리.
    /// 각성이 아니면 currentSoulTimer / SoulDrainProgress 그대로 유지(일시정지).
    /// </summary>
    void HandleSoulDrain()
    {
        if (!isAwakened) return;
        if (infiniteAwakening)
        {
            SoulDrainProgress = 1f;
            currentSoulTimer = soulDrainInterval; // UI가 필요하면 꽉 찬 상태 유지
            return;
        }
        if (soul <= 0)
        {
            SoulDrainProgress = 0f;
            return;
        }

        if (currentSoulTimer <= 0f)
        {
            currentSoulTimer = soulDrainInterval;
            SoulDrainProgress = 1f;
        }

        currentSoulTimer -= Time.deltaTime;
        SoulDrainProgress = Mathf.Clamp01(currentSoulTimer / soulDrainInterval);

        if (currentSoulTimer <= 0f)
        {
            soul--;
            UpdateAwakeningObjects();

            if (soul <= 0)
            {
                SoulDrainProgress = 0f;
                EndAwakening();
            }
            else
            {
                currentSoulTimer = soulDrainInterval;
                SoulDrainProgress = 1f;
            }
        }
    }

    private void StartTransformSequence()
    {

        StartTransform();

        StartCoroutine(RiseRoutine());

        if (transformCoroutine != null) StopCoroutine(transformCoroutine);
        transformCoroutine = StartCoroutine(TransformAnimationProcess());
    }

    private void StartTransform()
    {
        if (isTransforming) return;


        if (playerSkill != null && playerSkill.IsDashing)
        {
            playerSkill.ForceStopDash();
        }

        isTransforming = true;

        fixedPosition = transform.position;

        if (move) move.enabled = false;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rbSimulatedBackup = rb.simulated;
            rbGravityBackup = rb.gravityScale;
            rbBodyTypeBackup = rb.bodyType;
            rb.simulated = false;
        }

        if (hurtbox) hurtbox.enabled = false;

        PlayTransformEffect();
    }

    private IEnumerator RiseRoutine()
    {
        float t = 0f;
        Vector2 startPos = fixedPosition;
        Vector2 targetPos = startPos + (Vector2.up * liftHeight);

        while (t < riseDuration)
        {
            float ratio = t / riseDuration;
            transform.position = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, ratio));
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
    }

    public void FinishTransformEvent()
    {
        if (!isTransforming) return;

        if (rb)
        {
            rb.simulated = rbSimulatedBackup;
            rb.gravityScale = rbGravityBackup;
            rb.bodyType = rbBodyTypeBackup;
            rb.linearVelocity = Vector2.zero;
        }

        if (move) move.enabled = true;
        if (hurtbox) hurtbox.enabled = true;

        isTransforming = false;
    }

    private void PlayTransformEffect()
    {
        if (!transformEffectPrefab) return;

        var effect = Instantiate(transformEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.SetParent(transform);

        float life = 2f;
        var ps = effect.GetComponent<ParticleSystem>();
        if (ps) life = ps.main.duration;

        var anim = effect.GetComponent<Animator>();
        if (anim)
        {
            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.length > 0.01f) life = Mathf.Max(life, state.length);
        }

        Destroy(effect, life);
    }

    private IEnumerator TransformAnimationProcess()
    {
        animator.runtimeAnimatorController = transformController;
        yield return null;

        animator.Play("AwakeningTransFormBT", 0, 0f);

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("AwakeningTransFormBT") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
        );

        if (!isAwakened) isAwakened = true;

        animator.runtimeAnimatorController = awakenedController;

        UpdateAwakeningObjects();


        if (soul > 0 && currentSoulTimer <= 0f && SoulDrainProgress <= 0f)
        {
            currentSoulTimer = soulDrainInterval;
            SoulDrainProgress = 1f;
        }

        transformCoroutine = null;
    }

    private void PlayEndTransformEffect()
    {
        if (!endTransformEffectPrefab) return;

        var effect = Instantiate(endTransformEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.SetParent(transform);

        float life = 2f;

        var ps = effect.GetComponent<ParticleSystem>();
        if (ps)
            life = ps.main.duration;

        var anim = effect.GetComponent<Animator>();
        if (anim)
        {
            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.length > 0.01f)
                life = Mathf.Max(life, state.length);
        }

        Destroy(effect, life);
    }

    public void EndAwakening()
    {

        if (!isAwakened) return;

        isAwakened = false;


        if (transformCoroutine != null) StopCoroutine(transformCoroutine);

        PlayEndTransformEffect();

        if (normalController != null)
        {
            animator.runtimeAnimatorController = normalController;
        }

        if (awakeningObjects != null)
        {
            foreach (var obj in awakeningObjects)
                if (obj) obj.SetActive(false);
        }

        FinishTransformEvent();
    }

    private void UpdateAwakeningObjects()
    {
        if (awakeningObjects == null || awakeningObjects.Length == 0) return;

        // 1) 전부 끈다 (이전 상태 초기화)
        for (int i = 0; i < awakeningObjects.Length; i++)
        {
            if (awakeningObjects[i] != null)
                awakeningObjects[i].SetActive(false);
        }

        // 2) 현재 소울 수에 맞는 것만 켠다
        if (soul >= maxSoul)
        {
            // 최대 단계
            if (awakeningObjects.Length > 1 && awakeningObjects[1] != null)
                awakeningObjects[1].SetActive(true);
        }
        else if (soul >= 2)
        {
            // 중간 단계
            if (awakeningObjects.Length > 0 && awakeningObjects[0] != null)
                awakeningObjects[0].SetActive(true);
        }
        // soul 0~1이면 아무 것도 안 켜짐
    }


    public void AwakeCharacter()
    {
        if (isAwakened) return;
    }

    public void OnTransformCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;


        if (isAwakened)
        {
            EndAwakening();
            return;
        }
        if (blockTransformInput) return;

        if (!isTransforming && soul >= soulCost)
        {
            StartTransformSequence();
        }
    }
    public void Signal_ForceInfiniteAwakening()
    {
        // 1) 소울 3개로 세팅
        soul = maxSoul;
        currentSoulTimer = soulDrainInterval;
        SoulDrainProgress = 1f;
        UpdateAwakeningObjects();

        // 2) 무한 각성 ON
        infiniteAwakening = true;

        // 3) 이미 각성이면 끝(소울만 채우고 무한 유지)
        if (isAwakened)
        {
            ApplyFreezeTimeIfNeeded(true);
            return;
        }

        // 4) 즉시 변신 시작
        // 변신 중이 아니고, 현재 변신도 아니면 시퀀스 시작
        if (!isTransforming)
        {
            StartTransformSequence(); // 네 기존 함수 그대로 사용
        }

        // 5) 시간 정지(원하면)
        ApplyFreezeTimeIfNeeded(true);
    }

    public void Signal_StopFreezeAndRelease()
    {
        // 컷씬 끝에서 호출(선택)
        ApplyFreezeTimeIfNeeded(false);

        // 무한 각성을 유지할지 해제할지 선택 가능
        // infiniteAwakening = false;
    }
    private void ApplyFreezeTimeIfNeeded(bool freeze)
    {
        if (!freezeTimeWhileAwakened) return;

        if (freeze)
        {
            _timeScaleBackup = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _timeScaleBackup <= 0f ? 1f : _timeScaleBackup;
        }
    }

}
