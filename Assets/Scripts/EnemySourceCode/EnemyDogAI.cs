using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyDogAI : EnemyCoreSystem
{

    protected override void Update()
    {
        base.Update(); // ★ 부모 AI 틱(패트롤/감지/추격/공격 상태 전환) 먼저 수행

        if (currentState == "death") return;
        if (!player) return;

        UpdateTransform();

        if (currentState == "idle")
        {
            PatrolTick();
        }
        else if (currentState == "walk")
        {
            SetFacingSign();
            PatrolTick();
        }
        else if (currentState == "detect")
        {
            MoveStop();
            LookingAtPlayer();

            if (patrolCo != null) { StopCoroutine(patrolCo); patrolCo = null; }

            if (detectCo == null)
                detectCo = StartCoroutine(DetectCo()); // 중복 실행 방지
        }
        else if (currentState == "chase")
        {
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
                currentState = "detect"; // 승격만 함. 코루틴은 자동 대기 모드로 들어감
        }
    }

    protected override void ChaseTick()
    {
        base.ChaseTick();

        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, player.position);
        if (distToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            currentState = "attack";
            delay = 0;
        }
    }

    protected override IEnumerator AttackCo()
    {
        yield return base.AttackCo();

        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.1f);    // 공격 딜레이
        attackBox.GetComponent<CircleCollider2D>().enabled = true;
        yield return new WaitForSeconds(0.1f);
        attackBox.GetComponent<CircleCollider2D>().enabled = false;
        // 쿨타임 대기 (FX만 필요하면 애니 이벤트 쪽에서 TryHit 호출)
        yield return new WaitForSeconds(attackCooldown - 0.2f);

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

        currentState = player ? "chase" : "idle";
        attackCo = null;
    }

    protected override IEnumerator StunCo(float duration)
    {
        yield return base.StunCo(duration);

        currentState = player ? "chase" : "idle";
        stunCo = null;
    }

}
