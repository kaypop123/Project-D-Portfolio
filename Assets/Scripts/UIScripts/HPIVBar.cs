using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HPIVBar : MonoBehaviour
{
    [Header("Refs")]
    public Image liquidFill;

    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("Tween (Fill)")]
    public float smoothSpeed = 8f;
    private float targetFill;

    [Header("Hit Shake")]
    [SerializeField] RectTransform root;   // 흔들 대상(보통 HP_IVBag의 RectTransform)
    [SerializeField] float shakeX = 12f;   // 좌우 진폭(px)
    [SerializeField] float shakeDuration = 0.18f;
    [SerializeField] int vibrato = 18;     // 잔진동
    [SerializeField, Range(0f, 180f)] float randomness = 90f;
    [SerializeField] bool fadeOut = true;


    [Header("Hit Flash")]
    [SerializeField] Image hitFlashImage; // 색상을 변경할 이미지 (예: HP바 테두리)
    [SerializeField] Color hitColor = new Color(1, 0.6f, 0.6f, 1); // 피격 시 변경될 색상 (살짝 밝은 빨강)
    [SerializeField] float flashDuration = 0.1f;  // 빨갛게 변하는 데 걸리는 시간
    [SerializeField] float fadeOutDuration = 0.4f; // 원래 색으로 돌아오는 데 걸리는 시간

    private Color baseColor;

    Vector2 baseAnchoredPos;

    void Awake()
    {
        if (!root) root = transform as RectTransform;
        baseAnchoredPos = root ? root.anchoredPosition : Vector2.zero;

        if (liquidFill != null)
            targetFill = liquidFill.fillAmount;
        if (hitFlashImage != null)
        {
            baseColor = hitFlashImage.color;
        }
    }

    void OnDisable()
    {
        // 끄는 순간 위치/트윈 정리
        if (root)
        {
            root.DOKill();
            root.anchoredPosition = baseAnchoredPos;
        }
        if (liquidFill) liquidFill.DOKill();

        if (hitFlashImage)
        {
            hitFlashImage.DOKill();
            hitFlashImage.color = baseColor; // 원래 색상으로 즉시 복원
        }
    }

    void Update()
    {
        float currentFill = liquidFill.fillAmount;
        liquidFill.fillAmount = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
    }

    public void SetHP_DOTween(float hp, float dur = 0.25f)
    {
        currentHP = Mathf.Clamp(hp, 0, maxHP);
        float next = maxHP > 0 ? currentHP / maxHP : 0f;

        targetFill = next; // Lerp 타깃 동기화
        liquidFill.DOKill();
        liquidFill.DOFillAmount(next, dur).SetEase(Ease.OutCubic);
    }

    public void UpdateInstant()
    {
        targetFill = maxHP > 0 ? currentHP / maxHP : 0f;
        liquidFill.fillAmount = targetFill;
    }

    /// <summary>
    /// 피격 시 좌우 흔들기 (수위 애니메이션과 독립)
    /// </summary>
    public void PlayHitShake()
    {
        if (!root) return;
        root.DOKill();                           // 이전 흔들림 종료
        root.anchoredPosition = baseAnchoredPos; // 기준점으로 리셋

        // X축만 흔들도록 강도 벡터를 (shakeX, 0)로 설정
        root.DOShakeAnchorPos(
            shakeDuration,
            new Vector2(shakeX, 0f),
            vibrato,
            randomness,
            fadeOut,
            true
        )
        .OnComplete(() => root.anchoredPosition = baseAnchoredPos);
    }
    public void PlayHitFlash()
    {
        if (hitFlashImage == null) return;

        // 이전 색상 트윈이 실행 중이면 즉시 중지
        hitFlashImage.DOKill();

        // DOTween의 Sequence를 사용하여 색상 변경을 순차적으로 실행
        Sequence sequence = DOTween.Sequence();
        sequence.Append(hitFlashImage.DOColor(hitColor, flashDuration));       // 지정된 색으로 빠르게 변경
        sequence.Append(hitFlashImage.DOColor(baseColor, fadeOutDuration));    // 원래 색으로 천천히 복귀
    }
}
