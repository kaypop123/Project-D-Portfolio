using System.Collections;
using UnityEngine;

// 이 스크립트는 StatusSystem의 OnDie 이벤트를 구독하여
// 플레이어의 죽음과 관련된 모든 처리를 담당합니다.
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("플레이어의 상태 시스템")]
    public StatusSystem statusSystem;
    [Tooltip("플레이어의 애니메이션 바인더")]
    public PlayerAnimationBinder animationBinder;
    [Tooltip("플레이어의 이동 스크립트")]
    public PlayerMovement movement;
    [Tooltip("플레이어의 허트박스 스크립트")]
    public PlayerHurtbox2D hurtbox;
    [Tooltip("플레이어의 리지드바디")]
    public Rigidbody2D rb;

    [Header("Death Settings")]
    [Tooltip("죽음 애니메이션이 끝난 후 게임오버 UI를 띄우기까지의 대기 시간")]
    public float timeUntilGameOver = 1.5f;

    void Awake()
    {
        // 컴포넌트가 할당되지 않았다면 자동 할당
        if (statusSystem == null) statusSystem = GetComponent<StatusSystem>();
        if (animationBinder == null) animationBinder = GetComponent<PlayerAnimationBinder>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (hurtbox == null) hurtbox = GetComponentInChildren<PlayerHurtbox2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (statusSystem != null)
        {
            statusSystem.OnDie += HandleDeath;
        }
    }

    void OnDisable()
    {
        if (statusSystem != null)
        {
            statusSystem.OnDie -= HandleDeath;
        }
    }

    // OnDie 이벤트가 발생하면 이 함수가 호출
    private void HandleDeath()
    {
        // 1. 모든 관련 컴포넌트에게 "사망 상태"를 알리고 비활성화합니다.
        if (movement != null)
        {
            movement.SetDeadState(); // PlayerMovement의 모든 물리 업데이트와 입력을 중지시킴
            movement.enabled = false;
        }

        // ===== 추가하면 좋은 부분 =====
        // PlayerAnimationBinder도 업데이트를 멈추도록 비활성화합니다.
        if (animationBinder != null)
        {
            animationBinder.enabled = false;
        }
        // ============================

        if (hurtbox != null)
        {
            hurtbox.enabled = false;
        }

        // 2. 죽음 애니메이션 재생
        // (animationBinder가 비활성화되기 전에 애니메이션을 먼저 재생시킵니다)
        if (animationBinder != null)
        {
            animationBinder.PlayDeath();
        }

        // 3. 게임 오버 처리 시작
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // 죽음 애니메이션 + 연출이 끝날 때까지 기다립니다.
        yield return new WaitForSeconds(timeUntilGameOver);

        // 게임오버 직전에 물리 효과를 완전히 끕니다.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // velocity는 linearVelocity의 별칭
            rb.bodyType = RigidbodyType2D.Static;
        }

        Debug.Log("GAME OVER");
        // 예시: UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
    }
}