using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    public Light2D spotLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 0.7f;
    public float step = 0.1f; // 목표값 변경 시 증가/감소 단위
    public float flickerSpeed = 0.1f; // 목표값 바꾸는 주기
    public float smoothSpeed = 5f; // 부드러운 이동 속도

    private float targetIntensity;
    private bool increasing = true;

    void Start()
    {
        if (spotLight == null)
            spotLight = GetComponent<Light2D>();

        targetIntensity = minIntensity;
        spotLight.intensity = targetIntensity;

        InvokeRepeating(nameof(ChangeTargetIntensity), 0f, flickerSpeed);
    }

    void Update()
    {
        // 현재 intensity를 targetIntensity로 부드럽게 보간
        spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, Time.deltaTime * smoothSpeed);
    }

    void ChangeTargetIntensity()
    {
        if (increasing)
        {
            targetIntensity += step;
            if (targetIntensity >= maxIntensity)
            {
                targetIntensity = maxIntensity;
                increasing = false;
            }
        }
        else
        {
            targetIntensity -= step;
            if (targetIntensity <= minIntensity)
            {
                targetIntensity = minIntensity;
                increasing = true;
            }
        }
    }
}
