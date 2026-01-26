using Unity.VisualScripting;
using UnityEngine;

public class CoreStatus : MonoBehaviour
{

    [Header("코어 상태관리")]
    [SerializeField] private int maxHp = 40;
    private int currentHp;
    private bool isDead = false;

    [Header("레퍼런스")]
    [SerializeField] private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHp = maxHp;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        DamageAmount DamageSource = other.GetComponent<DamageAmount>();

        if (DamageSource != null)
        {
            TakeDamage(DamageSource.damageAmount);
        }
    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHp -= damage;
        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("hasHit");
        }
  
    }

    private void Die()
    {
        isDead = true;
        currentHp = 0;
        Debug.Log("코어가 죽었습니다.");
        animator.SetTrigger("hasDie");
    }
    // Update is called once per frame

}
