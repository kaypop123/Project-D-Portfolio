using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.Collections.AllocatorManager;

public class URPManager : MonoBehaviour
{
    public static URPManager urpm;

    public Volume URPVolume;      // Volume 인스펙터
    public float flashSpeed = 2f;
    public float weightCOE = 0.8f;
    public string targetTag;
    public bool isFlashing = false;
    public bool isBase = false;

    private float currentWeight; // 현재 weight 읽기

    private void Awake()
    {
        urpm = this;
    }
    void Start()
    {
        // Volume 데이터 가져오기
        if (URPVolume != null)
        {
            currentWeight = URPVolume.weight;
        }
    }

    void Update()
    {
        if (URPVolume != null)
        {
            if (isFlashing)
            {
                // 0~1 사이로 깜빡깜빡
                URPFlicker(flashSpeed);
            }
            else if(isBase)
            {
                URPVolume.weight = currentWeight;
            }
        }
        
    }

    public void StartAlarm() // 알람 시작
    {
        isFlashing = true;
    }

    public void StopAlarm() // 알람 종료
    {
        isFlashing = false;
        URPVolume.weight = currentWeight;
    }

    public void URPFlicker(float speed) // 깜빡임 계산
    {
        URPVolume.weight = (1 - Mathf.Abs(Mathf.Sin(Time.time * speed))) * weightCOE;
    }

    
}
