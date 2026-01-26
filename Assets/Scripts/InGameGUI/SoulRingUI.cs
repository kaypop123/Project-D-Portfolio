using UnityEngine;
using UnityEngine.UI;

public class SoulRingUI : MonoBehaviour
{
    [Header("연결할 대상")]
    public CharacterTransform player;  // 캐릭터(소울, 각성 상태 가지고 있는 스크립트)
    public Image ringImage;            // 이 슬롯에 해당하는 링 이미지

    [Header("옵션")]
    public bool hideWhenNormal = true;

    [Tooltip("0 = 맨 왼쪽, 1 = 두 번째, ... 이런 식으로 UI 배치 순서대로 번호 부여")]
    public int slotIndex = 0;          // 이 링이 몇 번째 링인지 (0부터 시작)

    void Update()
    {
        if (player == null || ringImage == null) return;

        // 각성 상태가 아니면
        if (!player.IsAwakened)
        {
            if (hideWhenNormal)
            {
                ringImage.enabled = false;
            }
            else
            {
                ringImage.enabled = true;
                ringImage.fillAmount = 0f;
            }
            return;
        }

        ringImage.enabled = true;

        int remainingSouls = Mathf.Max(player.soul, 0);
        float progress = Mathf.Clamp01(player.SoulDrainProgress); // 1 = 꽉 참, 0 = 다 닳기 직전

        // 소울이 아예 없으면 전부 0
        if (remainingSouls <= 0)
        {
            ringImage.fillAmount = 0f;
            return;
        }

        // "사용 중인 마지막 소울"이 차지하는 슬롯 인덱스
        // 예) 소울 3개면 사용 중인 슬롯 인덱스 = 2 (0,1은 꽉찬 상태, 2가 깎이는 중)
        int lastUsedIndex = remainingSouls - 1;

        if (slotIndex < lastUsedIndex)
        {
            // 아직 손도 안 댄 소울들 → 항상 1.0 (꽉 찬 상태)
            ringImage.fillAmount = 1f;
        }
        else if (slotIndex == lastUsedIndex)
        {
            // 지금 깎이는 중인 소울 → 진행도에 따라 1 → 0
            ringImage.fillAmount = progress;
        }
        else
        {
            // 이미 사라진 소울들 → 0
            ringImage.fillAmount = 0f;
        }
    }
}
