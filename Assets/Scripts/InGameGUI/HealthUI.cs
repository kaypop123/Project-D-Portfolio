using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class HealthUI : MonoBehaviour
{
    [Header("연동 설정")]
    public StatusSystem statusSystem; // 캐릭터 스탯시스템 연결
    public Image heartImage; // 바꿀 Ui 이미지
    public Image glassImage; // 유리관 이미지

    [Header("기본 상태")]
    public Sprite fullHeartSprite; // 심장 기본 상태 이미지
    public Sprite fullGlassImage;

    [System.Serializable]
    public struct DamageStage
    {
        [Range(0f, 1f)]
        public float threshold; // 비율 측정할 비율 
        public Sprite sprite; // 해당 비율 이하일때 보여줄 이미지 슬롯  
        public Sprite GlassImage; //해당 비율 이하일때 유리관 이미지 슬롯
    }

    [Header("손상 단계 설정")]
    public List<DamageStage> damageStates; // 백분율에 사용할 데이터 삽입


    [Header("피격 흔들림 설정 (DOTween)")]
    [SerializeField] private float shakeDuration = 0.5f;// 흔들리는 시간
    [SerializeField] private float shakeStrength = 30f;// 흔들리는 세기
    [SerializeField] private int shakeVibrator = 20;// 진동 횟수
    [SerializeField] private float shakeRandomness = 90f;// 무작위성
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //스테이터스 스크립트 Null값 방지 
        if (statusSystem != null)
        {
            statusSystem.OnHealthChanged += UpdateHealthImage;
            // 게임 시작 시 현재 체력 상태로 한번 갱신
            statusSystem.OnTakeDamage += PlayShakeEffect;
            UpdateHealthImage(statusSystem.CurrentHealth, statusSystem.MaxHealth);
        }

    }
    // 피격 시 호출되는 흔들림 함수
    void PlayShakeEffect()
    {
        if (heartImage != null)
        {
            heartImage.rectTransform.DOKill(true);
            heartImage.rectTransform.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrator, shakeRandomness);
        }
        if (glassImage != null)
        {
            glassImage.rectTransform.DOKill(true);
            glassImage.rectTransform.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrator, shakeRandomness);
        }
    }
    // StatusSystem의 OnHealthChanged(int, int)와 시그니처를 맞춤
    void UpdateHealthImage(int currentHealth, int maxHealth)
    {
        // 정수 나눗셈 방지를 위해 Float값 비율 계산 (정규화) 
        float healthPercent = (float )currentHealth / maxHealth;
        // 기본 heart이미지 설정
        Sprite targetSprite = fullHeartSprite;
        Sprite targetGlassSprite = fullGlassImage;
        // 가장 낮은 상태 찾기위한 변수
        DamageStage? currentStage = null;

        // 순회 하며 현제 체력상태 확인 하면서 맞는 이미지 찾기
        foreach (var stage in damageStates)
        {
            if (healthPercent <= stage.threshold)
            {
                if (currentStage == null || stage.threshold < currentStage.Value.threshold)
                {
                    currentStage = stage;
                }

            }
        }
        if (currentStage != null)
        {
            targetSprite = currentStage.Value.sprite;
            targetGlassSprite = currentStage.Value.GlassImage;
        }
        if (heartImage.sprite != targetSprite)
        {
            heartImage.sprite = targetSprite;
        }
        if (glassImage != null && glassImage.sprite != targetGlassSprite)
        {
            glassImage.sprite = targetGlassSprite;
        }
    }
    void OnDestroy()
    {
        // 메모리 누수 방지
        if (statusSystem != null)
        {
            statusSystem.OnHealthChanged -= UpdateHealthImage;
            statusSystem.OnTakeDamage -= PlayShakeEffect;
        }
        if (heartImage != null) heartImage.rectTransform.DOKill();
        if (glassImage != null) glassImage.rectTransform.DOKill();
    }
}
