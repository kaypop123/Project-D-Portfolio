using UnityEngine;
using UnityEngine.UI;

public class SoulGaugeUI : MonoBehaviour
{
    [Header("소울 아이콘 배열 (순서대로 넣으세요)")]
    // 여기에 불타는 애니메이션 오브젝트(Image + Animator)를 1번, 2번, 3번 순서로 드래그하세요.
    public Animator[] soulIcons;

    [Header("플레이어 연결")]
    public CharacterTransform playerStatus; // 소울 개수를 알기 위해 필요

    private int lastSoulCount = -1; // 개수 변화 감지용

    void Start()
    {
        // 시작할 때 모든 불꽃을 일단 꺼둡니다 (초기화)
        // 만약 '빈 슬롯' 이미지는 냅두고 '불꽃'만 끄고 싶다면 이 방식을 씁니다.
        for (int i = 0; i < soulIcons.Length; i++)
        {
            if (soulIcons[i] != null)
                soulIcons[i].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (playerStatus == null) return;

        // 소울 개수가 변했을 때만(먹었거나 썼을 때) 화면을 갱신합니다.
        if (lastSoulCount != playerStatus.soul)
        {
            UpdateSoulDisplay(playerStatus.soul);
            lastSoulCount = playerStatus.soul;
        }
    }

    void UpdateSoulDisplay(int currentSouls)
    {
        // 배열을 돌면서 소울 개수만큼 불꽃을 켭니다.
        for (int i = 0; i < soulIcons.Length; i++)
        {
            if (soulIcons[i] == null) continue;

            // [핵심 로직]
            // 현재 소울이 2개라면 -> 인덱스 0, 1은 켜지고 / 2는 꺼짐
            if (i < currentSouls)
            {
                // 불꽃이 꺼져있다면 켭니다
                if (!soulIcons[i].gameObject.activeSelf)
                {
                    soulIcons[i].gameObject.SetActive(true);

                    // 불타는 애니메이션 재생 (트리거 이름이 "Burn"이라고 가정)
                    soulIcons[i].SetTrigger("Burn");
                }
            }
            else
            {
                // 소울이 부족한 슬롯은 불꽃을 끕니다
                if (soulIcons[i].gameObject.activeSelf)
                {
                    soulIcons[i].gameObject.SetActive(false);
                }
            }
        }
    }
}