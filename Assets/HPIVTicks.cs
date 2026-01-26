using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPIVTicks : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform tickContainer;   // 눈금이 생성될 부모(비우면 this)
    public RectTransform maskArea;        // 마스크(좌표 변환 기준)
    public Image tickPrefab;              // 작은 하얀 막대 프리팹(에셋)

    [Tooltip("액체 눈금 분할에 사용할 정확한 상/하 기준점(선택). 지정 시 이 범위 안에서만 등분")]
    public RectTransform topRef;          // MaskArea 자식 권장
    public RectTransform bottomRef;       // MaskArea 자식 권장

    [Header("Logic")]
    public int hpPerMinorTick = 10;       // 작은 눈금 간격(HP)
    public int majorHpStep = 50;          // 50,100,150…은 굵게
    public bool includeTopBottom = false; // 양 끝 포함 여부
    public float minPixelGap = 10f;       // 너무 촘촘하면 step을 자동 증가

    [Header("Layout (px)")]
    public float insetFromRight = 0f;     // 오른쪽에서 안쪽으로 들여쓰기
    public float tickWidthMinor = 10f;
    public float tickWidthMajor = 16f;
    public float tickThickness = 2f;

    readonly List<Image> pool = new();
    RectTransform _tc;

    void Awake()
    {
        _tc = tickContainer ? tickContainer : (RectTransform)transform;

        // 안전 앵커(오른쪽 세로 스트레치)
        _tc.anchorMin = new Vector2(1f, 0f);
        _tc.anchorMax = new Vector2(1f, 1f);
        _tc.pivot = new Vector2(1f, 0.5f);
    }

    public void RebuildByMaxHP(int maxHP)
    {
        if (!maskArea || !tickPrefab) return;
        if (!_tc) _tc = tickContainer ? tickContainer : (RectTransform)transform;

        // 1) 유효 세로 범위 계산 (TopRef/BottomRef가 있으면 그것을 우선 사용)
        float yTopHR, yBottomHR;
        if (topRef && bottomRef)
        {
            // MaskArea 기준 로컬 Y
            yTopHR = maskArea.InverseTransformPoint(topRef.position).y;
            yBottomHR = maskArea.InverseTransformPoint(bottomRef.position).y;
        }
        else
        {
            // 전체 rect를 쓰되 테두리/곡면이 포함되면 부정확해질 수 있음
            yTopHR = maskArea.rect.height * 0.5f;
            yBottomHR = -maskArea.rect.height * 0.5f;
        }

        if (yTopHR <= yBottomHR) { HideAll(); return; }

        // 2) 픽셀 간격 기준으로 step 자동 보정
        int step = Mathf.Max(1, hpPerMinorTick);
        float unitH = (yTopHR - yBottomHR) / Mathf.Max(1f, maxHP); // 1HP당 px
        while (step * unitH < minPixelGap && step <= majorHpStep * 16)
            step *= 2;

        // 3) 눈금 HP 리스트
        List<int> hpList = new();
        int startHP = includeTopBottom ? 0 : step;
        int endHP = includeTopBottom ? maxHP : maxHP - step;
        for (int hp = startHP; hp <= endHP; hp += step) hpList.Add(hp);

        EnsurePool(hpList.Count);

        // 4) 오른쪽 기준 X
        float xRightLocal = _tc.rect.width * 0.5f;
        float xLocal = xRightLocal - insetFromRight;

        // 5) 배치 (좌표 변환 + 픽셀 스냅)
        HideAll();
        for (int i = 0; i < hpList.Count; i++)
        {
            int hpAtTick = hpList[i];
            float t = Mathf.Clamp01(hpAtTick / (float)maxHP);
            float yInHR = Mathf.Lerp(yBottomHR, yTopHR, t);

            Vector3 worldPos = maskArea.TransformPoint(new Vector3(0f, yInHR, 0f));
            Vector3 localInTC = _tc.InverseTransformPoint(worldPos);

            // 픽셀 스냅(세로 위치 정수화)
            float ySnap = Mathf.Round(localInTC.y);

            bool isMajor = (majorHpStep > 0) && (hpAtTick % majorHpStep == 0);

            var img = pool[i];
            var rt = img.rectTransform;

            img.gameObject.SetActive(true);
            rt.SetParent(_tc, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(isMajor ? tickWidthMajor : tickWidthMinor, tickThickness);
            rt.anchoredPosition = new Vector2(xLocal, ySnap);
        }
    }

    void EnsurePool(int need)
    {
        for (int i = pool.Count; i < need; i++)
        {
            var inst = Instantiate(tickPrefab);
            inst.name = $"Tick_{i}";
            inst.raycastTarget = false;
            pool.Add(inst);
        }
    }

    void HideAll()
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }
}
