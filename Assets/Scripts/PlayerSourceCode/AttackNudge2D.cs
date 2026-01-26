using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class AttackNudge2D : MonoBehaviour
{
    [Header("Settings")]
    public float defaultDistance = 0.35f;   // 기본 전진 거리 (X축)
    public float defaultDuration = 0.08f;   // 이동 시간
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Leap Settings (대각선)")]
    public float leapHeight = 0.2f;         // 대각선 공격 시 점프할 높이 (Y축 고정값)

    [Header("Collision")]
    public LayerMask blockMask = ~0;
    public float skin = 0.02f;
    public bool requireGrounded = true;

    Rigidbody2D rb;
    Collider2D col;
    PlayerMovement move;
    Coroutine nudgeCo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        move = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// 1. [수평] 전진 공격 (기존)
    /// </summary>
    public void AttackNudge(float multiplier = 1f)
    {
        if (requireGrounded && move && !move.IsGrounded) return;

        float sign = (move && move.LastDirection < 0f) ? -1f : 1f;
        Vector2 dir = new Vector2(sign, 0f); // 순수 X축

        StartNudge(dir, defaultDistance * multiplier, defaultDuration);
    }

    /// <summary>
    /// 2. [수직] 제자리 점프/찍기 공격
    /// </summary>
    public void AttackNudgeVertical(float multiplier = 1f)
    {
        Vector2 dir = (multiplier >= 0) ? Vector2.up : Vector2.down;
        StartNudge(dir, defaultDistance * Mathf.Abs(multiplier), defaultDuration);
    }

    /// <summary>
    /// 3. [대각선] 전진 점프 공격 (Leap)
    /// 사용법: 애니메이션 이벤트에서 이 함수 선택.
    /// multiplier: 1이면 설정된 defaultDistance만큼 전진하며 점프.
    /// </summary>
    public void AttackLeap(float forwardMultiplier = 1f)
    {
        // 땅에 있을 때만 점프 시작 가능
        if (requireGrounded && move && !move.IsGrounded) return;

        // X축 계산: 바라보는 방향 * (기본거리 * 배율)
        float sign = (move && move.LastDirection < 0f) ? -1f : 1f;
        float xDist = defaultDistance * forwardMultiplier;

        // Y축 계산: 설정된 leapHeight 고정값 사용
        float yDist = leapHeight;

        // 대각선 벡터 생성
        Vector2 moveVector = new Vector2(xDist * sign, yDist);

        // 방향(Normalized)과 총 거리(Magnitude)로 분리하여 전달
        StartNudge(moveVector.normalized, moveVector.magnitude, defaultDuration);
    }

    public void CancelNudge()
    {
        if (nudgeCo != null) StopCoroutine(nudgeCo);
        nudgeCo = null;
    }

    private void StartNudge(Vector2 dir, float dist, float dur)
    {
        if (nudgeCo != null) StopCoroutine(nudgeCo);
        nudgeCo = StartCoroutine(NudgeCo(dir, dist, dur));
    }

    IEnumerator NudgeCo(Vector2 dir, float distance, float duration)
    {
        // 충돌 감지 (대각선 방향으로 레이를 쏨)
        float allowed = distance;
        var hits = new List<RaycastHit2D>(4);
        ContactFilter2D filter = new ContactFilter2D() { useLayerMask = true, layerMask = blockMask, useTriggers = false };

        int count = col.Cast(dir, filter, hits, distance + skin);
        if (count > 0)
        {
            float hitDist = Mathf.Max(0f, hits[0].distance - skin);
            allowed = Mathf.Min(distance, hitDist);
        }

        Vector2 start = rb.position;
        Vector2 target = start + (dir * allowed);

        float t = 0f;
        while (t < duration)
        {
            t += Time.fixedDeltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / duration));
            rb.MovePosition(Vector2.Lerp(start, target, k));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(target);
        nudgeCo = null;
    }
}