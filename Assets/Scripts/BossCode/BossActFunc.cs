using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossActFunc : BossCore
{
    [Header("Death Cutscene")]
    public GameObject deathCutsceneRoot;  
    private bool _deathCutsceneTriggered = false;
    protected override void Update()
    {
        base.Update();
        //Debug.Log(attackEnd);
    }
    // ---------- [ 감지 ] ---------- //
    private int attackNum = 5;
    public bool attackEnd = false;
    public void Detect()
    {
        if (currentPhase == "normal")
        {
            N_Attack();
        }

        if (currentPhase == "broken_L_Leg" || currentPhase == "broken_R_Leg")
        {
            if (attackNum < 5)
            {
                N_Attack();
                attackNum++;
            }
            else
            {
                currentState = "sumon";
                attackNum = 0;
            }
        }

        if (currentPhase == "noLeg")
        {
            if (Random.value < 0.5f)
                currentState = "gas";
            else
                currentState = "acid";
        }
    }

    private void N_Attack()
    {
        if (detectArea1.IsTouching(playerHitBox))
        {
            currentState = "attack1_L";
        }

        if (detectArea2.IsTouching(playerHitBox))
        {
            currentState = "attack2_L";
        }

        if (detectArea3.IsTouching(playerHitBox))
        {
            currentState = "attack2_R";
        }

        if (detectArea4.IsTouching(playerHitBox))
        {
            currentState = "attack1_R";
        }
    }

    public IEnumerator Broken_LCo()
    {
        invincibility = true;
        yield return null;

        animator.SetTrigger("Broken_L");

        yield return new WaitForSeconds(2f);

        if (currentPhase == "normal") currentPhase = "broken_L_Leg";
        else if(currentPhase == "broken_R_Leg") currentPhase = "noLeg";
        currentState = "idle(delay)";
        invincibility = false;
    }

    public IEnumerator Broken_RCo()
    {
        invincibility = true;
        yield return null;

        animator.SetTrigger("Broken_R");

        yield return new WaitForSeconds(2f);

        if (currentPhase == "normal") currentPhase = "broken_R_Leg";
        else if (currentPhase == "broken_L_Leg") currentPhase = "noLeg";
        currentState = "idle(delay)";
        invincibility = false;
    }

    public IEnumerator Attack1_LCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Attack1_L");

        Debug.Log("1");

        if (currentPhase == "normal") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.5f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.42f);

        attackBox1.enabled = true;

        Debug.Log("2");

        if (currentPhase == "normal") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.15f);

        attackBox1.enabled = false;

        Debug.Log("3");

        yield return new WaitUntil(() => attackEnd);

        Debug.Log("4");

        currentState = "idle(delay)";
    }
    public IEnumerator Attack1_RCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Attack1_R");

        if (currentPhase == "normal") yield return new WaitForSeconds(0.83f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.83f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.83f);

        attackBox2.enabled = true;

        if (currentPhase == "normal") yield return new WaitForSeconds(0.20f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.15f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.15f);

        attackBox2.enabled = false;

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    public IEnumerator Attack2_LCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Attack2_L");

        if (currentPhase == "normal") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.33f);

        attackBox1.enabled = true;

        if (currentPhase == "normal") yield return new WaitForSeconds(0.25f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.25f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.25f);

        attackBox1.enabled = false;

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    public IEnumerator Attack2_RCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Attack2_R");

        if (currentPhase == "normal") yield return new WaitForSeconds(0.66f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.83f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.66f);

        attackBox2.enabled = true;

        if (currentPhase == "normal") yield return new WaitForSeconds(0.34f);
        if (currentPhase == "broken_L_Leg") yield return new WaitForSeconds(0.33f);
        if (currentPhase == "broken_R_Leg") yield return new WaitForSeconds(0.34f);

        attackBox2.enabled = false;

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    // ---------- [ 소환 ] ---------- //
    public EnemyWormAI worm;
    public Transform spawnPos1;
    public Transform spawnPos2;


    public IEnumerator SumonCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Sumon");

        yield return new WaitForSeconds(0.55f);

        Vector2 SP1 = spawnPos1.position;
        Vector2 SP2 = spawnPos2.position;
        Instantiate(worm, SP1, Quaternion.identity);
        Instantiate(worm, SP2, Quaternion.identity);

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    // ---------- [ 가스 ] ----------//
    public Gas gas;
    public Transform gasPos;
    public IEnumerator GasCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Gas");

        yield return new WaitForSeconds(0.8f);

        Vector2 GP = gasPos.position;
        Instantiate(gas, GP, Quaternion.identity);

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    // ---------- [ 산성 ] ---------- //
    public EnemyWormAcid acid;
    public Transform[] aSPos;
    public Transform[] aEPos;

    [Header("계산")]
    [SerializeField] float shootSpeed = 6f;                 // (요구사항) 고정 속도
    [SerializeField] bool useHighArc = true;                // 높은 포물선/낮은 포물선 선택
    public IEnumerator AcidCo()
    {
        attackEnd = false;
        yield return null;

        animator.SetTrigger("Acid");

        yield return new WaitForSeconds(0.55f); // -------------------------------------------------------- 수정 요함 ---- //

        Shoot(0);
        yield return new WaitForSeconds(0.15f);

        Shoot(1);
        yield return new WaitForSeconds(0.15f);

        Shoot(2);
        yield return new WaitForSeconds(0.15f);

        Shoot(3);
        yield return new WaitForSeconds(0.15f);

        Shoot(4);
        yield return new WaitForSeconds(0.15f);

        Shoot(5);
        yield return new WaitForSeconds(0.15f);

        Shoot(6);
        yield return new WaitForSeconds(0.15f);

        Shoot(4);
        yield return new WaitForSeconds(0.15f);

        Shoot(1);
        yield return new WaitForSeconds(0.15f);

        Shoot(0);
        yield return new WaitForSeconds(0.15f);

        Shoot(2);
        yield return new WaitForSeconds(0.15f);

        Shoot(3);

        yield return new WaitUntil(() => attackEnd);

        currentState = "idle(delay)";
    }

    public void Shoot(int i)
    {
        if (!acid) return;

        Vector2 start = aSPos[i].position;
        Vector2 target = aEPos[i].position;

        if (!TryGetBallisticVelocity(start, target, shootSpeed, useHighArc, out var v0))
        {
            // 사거리 밖/해 없음 → 안전한 폴백(그냥 직선 발사)
            v0 = (target - start).normalized * shootSpeed;
        }

        var proj = Instantiate(acid, start, Quaternion.identity);
        proj.Launch(v0);
    }

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

    // ---------- [ 딜레이 ] ---------- //
    public IEnumerator DelayCo()
    {
        yield return new WaitForSeconds(afterDelay);

        currentState = "idle";
    }

    public IEnumerator StartDelayCo()
    {
        yield return new WaitForSeconds(startAfterDelay);
        detectArea1.enabled = true;
        detectArea2.enabled = true;
        detectArea3.enabled = true;
        detectArea4.enabled = true;

        currentState = "idle";
    }

    // ==========[ 표준 피격 엔트리 ]========== //
    private readonly Dictionary<Transform, float> _lastHurtTime1 = new();
    private readonly Dictionary<Transform, float> _lastHurtTime2 = new();
    private readonly Dictionary<Transform, float> _lastHurtTime3 = new();

    public void TryHurt(Collider2D other, int part)
    {
        if (!string.IsNullOrEmpty(hurtboxAcceptTag) && !other.CompareTag(hurtboxAcceptTag)) return;
        if ((hurtboxAcceptLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (invincibility) return;

        DamageAmount damageDealer = null;
        if (!other.TryGetComponent(out damageDealer))
            damageDealer = other.GetComponentInParent<DamageAmount>();

        int dmg = damageDealer ? damageDealer.damageAmount : 0;

        Transform attacker = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        float now = Time.time;
        if (part == 1)
        {
            if (_lastHurtTime1.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
                return;
        }
        else if (part == 2)
        {
            if (_lastHurtTime2.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
                return;
        }
        else if (part == 3)
        {
            if (_lastHurtTime3.TryGetValue(attacker, out float last) && now - last < perAttackerCooldown)
                return;
        }


        if (part == 1) _lastHurtTime1[attacker] = now;
        else if (part == 2) _lastHurtTime2[attacker] = now;
        else if (part == 3) _lastHurtTime3[attacker] = now;

        Vector2 attackOrigin = other.bounds.center;

        TakeDamage(dmg, attackOrigin, part, attacker);
    }

    public void TakeDamage(int dmg, Vector2 attackOrigin, int part, Transform attacker = null)
    {
        if (currentState == "die") return;

        if (part == 1)
        {
            leg_L_CurrentHP = Mathf.Max(leg_L_CurrentHP - dmg, 0);
            for (int i = 0; i < hfL.Length; i++)
            {
                hfL[i].DoFlash();
            }
        }
        if (part == 2)
        {
            leg_R_CurrentHP = Mathf.Max(leg_R_CurrentHP - dmg, 0);
            for (int i = 0; i < hfR.Length; i++)
            {
                hfR[i].DoFlash();
            }
        }
        if (part == 3)
        {
            body_CurrentHP = Mathf.Max(body_CurrentHP - dmg, 0);
            for (int i = 0; i < hfM.Length; i++)
            {
                hfM[i].DoFlash();
            }
        }

        HitStop.I?.Do(0.06f);

        Vector2 basePos = (fx && fx.pos) ? (Vector2)fx.pos.position : (Vector2)transform.position;
        Vector2 hitPoint = attackOrigin;

        fx?.PlayFromOriginAt(attackOrigin, hitPoint);
        cameraShaker?.StartShake();

        if (part == 1 && leg_L_CurrentHP <= 0)
        {
            StopCoroutine(motionCo);
            currentState = "broken_L";
            return;
        }

        if (part == 2 && leg_R_CurrentHP <= 0)
        {
            StopCoroutine(motionCo);
            currentState = "broken_R";
            return;
        }

        if (part == 3 && body_CurrentHP <= 0)
        {
            StopCoroutine(motionCo);
            currentState = "die";
            Die();
            return;
        }
    }

    // ==========[ 데스 처리 ]========== //
    public void Die()
    {
        animator.SetTrigger("Die");

        if (motionCo != null) { motionCo = null; }

        if (deathEffect) Instantiate(deathEffect, transform.position, Quaternion.identity);
        if (!_deathCutsceneTriggered)
        {
            _deathCutsceneTriggered = true;

            if (deathCutsceneRoot != null)
                deathCutsceneRoot.SetActive(true);
            else
                Debug.LogWarning("deathCutsceneRoot가 연결되지 않았습니다.");
        }
    }

    // ===== 기즈모 ===== //
    [Header("Gizmos")]
    [SerializeField] bool drawTrajectory = true;
    [SerializeField] int gizmoSteps = 30;          // 선분 개수
    [SerializeField] float gizmoTimeStep = 0.05f;  // dt (초)
    protected void OnDrawGizmosSelected()
    {
        if (!drawTrajectory || aEPos == null) return;
        if (!(currentState == "acid")) return;

        for (int i = 0; i < aSPos.Length; i++)
        {
            Vector2 start = aSPos[i].position;
            Vector2 target = aEPos[i].position;

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
            for (int ii = 0; ii < gizmoSteps; ii++)
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
}
