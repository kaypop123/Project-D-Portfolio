using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyWormAI : EnemyCoreSystem
{
    [Header("침")]
    [SerializeField] Transform firePoint;
    [SerializeField] EnemyWormAcid acid;

    [Header("계산")]
    [SerializeField] float shootSpeed = 10f;                 // (요구사항) 고정 속도
    [SerializeField] bool useHighArc = false;                // 높은 포물선/낮은 포물선 선택
    [SerializeField] Vector2 targetOffset = new(0f, 0.2f);   // 플레이어 중심 보정

    [Header("Gizmos")]
    [SerializeField] bool drawTrajectory = true;
    [SerializeField] int gizmoSteps = 30;          // 선분 개수
    [SerializeField] float gizmoTimeStep = 0.05f;  // dt (초)


    protected override void Update()
    {
        base.Update(); // ★ 부모 AI 틱(패트롤/감지/추격/공격 상태 전환) 먼저 수행

        if (currentState == "death") return;
        if (!player) return;

        UpdateTransform();

        if (currentState == "idle")
        {
            patrolCo ??= StartCoroutine(PatrolCo());
            PatrolTick();
        }
        else if (currentState == "walk")
        {
            SetFacingSign();
            PatrolTick();
        }
        else if (currentState == "chase")
        {
            if (patrolCo != null) { StopCoroutine(patrolCo); patrolCo = null; }
            ChaseTick();
        }
        else if (currentState == "attack")
        {
            MoveStop();

            if (attackCo == null)
                attackCo = StartCoroutine(AttackCo()); // 중복 실행 방지
        }
        else if (currentState == "hurt")
        {
            if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
            // 스턴 코루틴에서 상태 복귀를 담당
        }

    }


    //----------------------------------코루틴 및 함수 오버라이드-----------------------------------//

    protected override void PatrolTick()
    {
        base.PatrolTick();

        if (IsPatrolState && player)
        {
            if (Vector2.Distance(player.position, transform.position) <= detectRange)
                currentState = "chase";
        }
    }

    protected override void ChaseTick()
    {
        base.ChaseTick();

        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);
        if (distToPlayer <= attackRange)
        {
            
            currentState = "attack";
            delay = 0;
        }
    }

    protected override IEnumerator AttackCo()
    {
        yield return base.AttackCo();

        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.6f);    // 공격 딜레이
        Shoot();
        yield return new WaitForSeconds(attackCooldown - 0.6f);


        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);
        if (distToPlayer <= attackRange)
        {
            currentState = " ";     // 상태 변화 판정을 주기 위함
            currentState = "attack";
            float dx = player.position.x - transform.position.x;
            ApplyFacing(dx >= 0);
        }
        else
        {
            currentState = "idle";
        }
        attackCo = null;
    }

    protected override IEnumerator StunCo(float duration)
    {
        yield return base.StunCo(duration);

        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);
        if (distToPlayer <= attackRange)
        {
            currentState = "attack";
        }
        else
        {
            currentState = "idle";
        }
        stunCo = null;
    }



    // ---------------------애벌레 공격------------------------ //

    public void Shoot()
    {
        if (!firePoint || !acid || !player) return;

        Vector2 start = firePoint.position;
        Vector2 target = (Vector2)player.position + targetOffset;

        if (!TryGetBallisticVelocity(start, target, shootSpeed, useHighArc, out var v0))
        {
            // 사거리 밖/해 없음 → 안전한 폴백(그냥 직선 발사)
            v0 = (target - start).normalized * shootSpeed;
        }

        var proj = Instantiate(acid, start, Quaternion.identity);
        proj.Launch(v0);
    }

    /// <summary>
    /// 고정 speed로 target에 도착하는 초기속도 벡터를 계산.
    /// </summary>
    static bool TryGetBallisticVelocity(
        Vector2 start, Vector2 target, float speed, bool highArc, out Vector2 velocity)
    {
        velocity = default;

        Vector2 diff = target - start;
        float dx = diff.x;
        float dy = diff.y;

        // 중력(양수) : 아래로 당기는 크기만 사용
        float g = Mathf.Abs(Physics2D.gravity.y);
        if (g < 0.0001f) return false;

        float x = Mathf.Abs(dx);
        if (x < 0.0001f) return false; // 거의 수직이면 별도 처리 필요(여기선 false)

        float v2 = speed * speed;
        float v4 = v2 * v2;

        float disc = v4 - g * (g * x * x + 2f * dy * v2);
        if (disc < 0f) return false; // 도달 불가

        float sqrt = Mathf.Sqrt(disc);

        // tan(theta) = (v^2 ± sqrt(disc)) / (g x)
        float tan = (v2 + (highArc ? sqrt : -sqrt)) / (g * x);
        float theta = Mathf.Atan(tan);

        float vx = speed * Mathf.Cos(theta);
        float vy = speed * Mathf.Sin(theta);

        // 실제 방향(왼/오) 반영
        vx *= Mathf.Sign(dx);

        velocity = new Vector2(vx, vy);
        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!drawTrajectory || firePoint == null) return;

        if (player == null) return;

        Vector2 start = firePoint.position;
        Vector2 target = (Vector2)player.position + targetOffset;

        // 1) 타겟 지점 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(target, 0.08f);

        // 2) 발사점 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(start, 0.06f);

        // 3) 궤적 계산
        if (!TryGetBallisticVelocity(start, target, shootSpeed, useHighArc, out var v0))
        {
            // 도달 불가능하면 빨간 선으로만 표시
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, target);
            return;
        }

        // 4) 포물선 궤적 그리기: p(t)=p0+v0*t+0.5*g*t^2
        Vector2 gVec = Physics2D.gravity; // (0, -9.81) 같은 벡터
        Vector2 prev = start;

        Gizmos.color = Color.green;

        float t = 0f;
        for (int i = 0; i < gizmoSteps; i++)
        {
            t += gizmoTimeStep;
            Vector2 p = start + v0 * t + 0.5f * gVec * t * t;

            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // 5) 시작 방향(초기속도)도 짧게 표시
        Gizmos.color = Color.white;
        Gizmos.DrawLine(start, start + v0.normalized * 0.6f);
    }
}