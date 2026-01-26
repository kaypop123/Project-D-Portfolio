// StatusSystem.cs (attack-giving 제거판)
using System;
using UnityEngine;

public class StatusSystem : MonoBehaviour
{
    [Header("Health Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField]  private int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => _currentHealth <= 0;

    [Header("Regeneration Settings")]
    [SerializeField] private bool enableRegen = true;   // 자동 회복 켜기/끄기
    [SerializeField] private float regenInterval = 3.0f; // 회복 주기 (3초)
    [SerializeField] private int regenAmount = 1;        // 회복량 (1)
    private float _regenTimer = 0f;                      // 시간 계산용 타이머


    public event Action<int, int> OnHealthChanged;
    public event Action OnTakeDamage;
    public event Action OnDie;

    void Awake()
    {
        _currentHealth = maxHealth;
    }
    void Update()
    {
        // 죽었거나, 회복 기능이 꺼져있거나, 체력이 이미 가득 찼다면 실행하지 않음
        if (IsDead || !enableRegen || _currentHealth >= maxHealth)
        {
            _regenTimer = 0f; // 
            return;
        }

        // 시간 누적
        _regenTimer += Time.deltaTime;

        // 설정값 지났는지 확인
        if (_regenTimer >= regenInterval)
        {
            Heal(regenAmount); // 1만큼 회복
            _regenTimer = 0f;  // 타이머 초기화
        }
    }
    // ===== 받는 쪽만 유지 =====
    public void TakeDamage(int damageAmount)
    {
        if (IsDead) return;

        damageAmount = Mathf.Max(0, damageAmount);
        _currentHealth = Mathf.Max(0, _currentHealth - damageAmount);
        _regenTimer = 0f;
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        

        if (IsDead)
            Die();
    }

    private void Die()
    {
        OnDie?.Invoke();
        Debug.Log($"{gameObject.name} has died.");
    }

    public void Heal(int healAmount)
    {
        if (IsDead) return;

        healAmount = Mathf.Max(0, healAmount);
        // 현재 체력이 최대 체력을 넘지 않도록 Min으로 제한
        _currentHealth = Mathf.Min(_currentHealth + healAmount, maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }
}



