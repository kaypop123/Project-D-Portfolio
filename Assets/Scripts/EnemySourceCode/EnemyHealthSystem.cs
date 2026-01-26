using UnityEngine;

public class EnemyHealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 30;
    public int currentHP;

    [Header("Death")]
    public GameObject deathEffect;       // 죽을 때 이펙트 프리팹 (선택)
    public bool destroyOnDeath = true;   // 죽으면 오브젝트 제거?

    public float deathAnimationLength = 1.5f;

    private EnemyAICoreRandomPatrol aiCore;
    public bool IsDead => currentHP <= 0;
    

    
    void Awake()
    {
        currentHP = maxHP;
        aiCore = GetComponent<EnemyAICoreRandomPatrol>();
    }

    public void TakeDamage(int dmg)
    {
        if (IsDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);

        if (IsDead)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 사망!");

        // 1. AI에게 죽음을 알림
        if (aiCore != null)
        {
            aiCore.NotifyDeath();
        }

        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 2. 애니메이션 길이만큼 기다렸다가 파괴
        if (destroyOnDeath)
            Destroy(gameObject, deathAnimationLength);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }
}
