using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class AnimeAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyAICoreRandomPatrol ai;
    private Animator animator;
    private SpriteRenderer sr;

    [Header("Facing")]
    public bool baseFacesLeft = true;

    private EnemyAICoreRandomPatrol.State _lastState;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        if (!ai) ai = GetComponent<EnemyAICoreRandomPatrol>();
        _lastState = ai ? ai.CurrentState : EnemyAICoreRandomPatrol.State.Idle;
    }

    void Update()
    {
        if (!ai) return;

        var state = ai.CurrentState;
        float vx = ai.CurrentVX;

        // 사망 애니메이션 트리거 1회 (가장 먼저 체크)
        if (_lastState != state && state == EnemyAICoreRandomPatrol.State.Death)
        {
            animator.SetTrigger("Die");
            // 죽으면 더 이상 다른 애니메이션 로직을 실행할 필요가 없음
            _lastState = state;
            return;
        }
        animator.SetBool("IsWalking", ai.IsWalking);
        animator.SetBool("IsRunning", ai.IsRunning);

        // Bark 트리거 1회
        if (_lastState != state && state == EnemyAICoreRandomPatrol.State.Bark)
            animator.SetTrigger("Bark");

        // 방향 적용
        if ((ai.IsWalking || ai.IsRunning) && Mathf.Abs(vx) > 0.001f)
            ApplyFacing(vx > 0f);
        if (state == EnemyAICoreRandomPatrol.State.Bark && ai.player)
        {
            float dx = ai.player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.001f) ApplyFacing(dx > 0f);
        }

        _lastState = state;
    }

    void ApplyFacing(bool lookingRight)
    {
        sr.flipX = baseFacesLeft ? lookingRight : !lookingRight;
    }
}
