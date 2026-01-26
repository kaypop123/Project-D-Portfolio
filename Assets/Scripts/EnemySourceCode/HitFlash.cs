using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D 사용하려면 필요

/// <summary>
/// 피격 시 반짝이는 효과 + Light2D 활성화/플립 대응
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color flashColor = Color.white;     // 반짝일 때 색상
    public float flashDuration = 0.1f;         // 한 번 반짝이는 시간
    public int flashCount = 2;                 // 몇 번 깜빡일지

    [Header("Light Settings")]
    public Light2D hitLight;                   // 라이트(2D Light) 참조
    public float lightIntensity = 3f;          // 반짝일 때 세기
    public bool followFlip = true;             // flipX에 맞춰 라이트도 반전

    private SpriteRenderer _renderer;
    private Color _originalColor;
    private Coroutine _flashCo;
    private float _originalLightIntensity;
    private Vector3 _originalLightScale;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _originalColor = _renderer.color;

        if (hitLight != null)
        {
            _originalLightIntensity = hitLight.intensity;
            _originalLightScale = hitLight.transform.localScale;
            hitLight.enabled = false; // 기본은 꺼두기
        }
    }

    /// <summary>
    /// 외부에서 호출해서 Flash 시작
    /// </summary>
    public void DoFlash()
    {
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(FlashCo());
    }

    private IEnumerator FlashCo()
    {
        for (int i = 0; i < flashCount; i++)
        {
            // 1) 스프라이트 반짝
            _renderer.color = flashColor;

            // 2) 라이트 켜기
            if (hitLight != null)
            {
                hitLight.enabled = true;
                hitLight.intensity = lightIntensity;

                // flipX 따라 라이트 반전
                if (followFlip)
                {
                    Vector3 scale = _originalLightScale;
                    scale.x = _renderer.flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                    hitLight.transform.localScale = scale;
                }
            }

            yield return new WaitForSeconds(flashDuration * 0.5f);

            // 3) 원래 색/라이트 복구
            _renderer.color = _originalColor;

            if (hitLight != null)
            {
                hitLight.enabled = false;
                hitLight.intensity = _originalLightIntensity;
            }

            yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        _flashCo = null;
    }

}
