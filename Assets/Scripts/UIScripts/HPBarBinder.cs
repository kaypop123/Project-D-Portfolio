// HPBarBinder.cs

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HPIVBar))]
public class HPBarBinder : MonoBehaviour
{
    [Header("Refs")]
    public StatusSystem status;
    public HPIVBar hpUI;
    public HPIVTicks ticks;

    void Reset()
    {
        hpUI = GetComponent<HPIVBar>();
  
    }

    void Start()
    {
        if (!hpUI) hpUI = GetComponent<HPIVBar>();

       

        if (status == null)
        {

            GameObject playerObject = GameObject.FindWithTag("Player");

     
            if (playerObject != null)
            {
                status = playerObject.GetComponent<StatusSystem>();
            }
        }
      


        if (status == null)
        {
            Debug.LogError("StatusSystem 참조를 찾을 수 없습니다! 'Player' 태그가 올바른 오브젝트에 설정되었는지 확인해주세요.", this);
            return; 
        }


        status.OnHealthChanged += OnHealthChanged;
        status.OnDie += OnDie;

        hpUI.maxHP = status.MaxHealth;
        hpUI.currentHP = status.CurrentHealth;
        hpUI.UpdateInstant();

        if (ticks) ticks.RebuildByMaxHP(status.MaxHealth);
    }


    void OnDestroy()
    {
        if (!status) return;
        status.OnHealthChanged -= OnHealthChanged;
        status.OnDie -= OnDie;
    }

    void OnHealthChanged(int current, int max)
    {
        if (current < hpUI.currentHP)
        {
            hpUI.PlayHitShake();
            hpUI.PlayHitFlash();
        }

        hpUI.maxHP = max;
        hpUI.SetHP_DOTween(current);
        if (ticks) ticks.RebuildByMaxHP(max);
    }

    void OnDie()
    {
        hpUI.SetHP_DOTween(0);
    }
}