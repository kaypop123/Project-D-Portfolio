using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class BloodScreenFX : MonoBehaviour
{
    public static BloodScreenFX Instance { get; private set; }

    [Header("References")]
    [SerializeField] Canvas canvas;                 // Screen Space - Overlay 권장
    [SerializeField] RectTransform container;       // 화면 전체 Rect
    [SerializeField] Image splatPrefab;             // 혈흔 Image 프리팹(여기에 Sprite가 이미 들어있어야 함)

    [Header("Pool & Limits")]
    [SerializeField] int poolSize = 8;
    [SerializeField] bool recycleOldest = true;

    [Header("Visual")]
    public float scale = 1.2f;                      // 고정 스케일
    public float lifetime = 5f;                     // 수명(초)
    [Range(0f, 1f)] public float baseAlpha = 0.85f; // 프리팹 Image.color.a 와 곱해짐
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    Camera cam;

    class Splat
    {
        public RectTransform rt;
        public Image img;
        public Coroutine co;
        public bool busy;
        public float origAlpha;
    }
    readonly List<Splat> pool = new();
    int nextIndex;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!canvas) canvas = GetComponentInChildren<Canvas>(true);
        InitPool();
    }

    void InitPool()
    {
        if (!splatPrefab || !container) return;

        pool.Clear();
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(splatPrefab.gameObject, container);
            go.name = $"BloodSplat_{i:D2}";
            var img = go.GetComponent<Image>();
            if (img)
            {
                img.raycastTarget = false;
                img.enabled = false;
            }

            pool.Add(new Splat
            {
                rt = go.transform as RectTransform,
                img = img,
                co = null,
                busy = false,
                origAlpha = img ? img.color.a : 1f
            });
        }
    }

    Splat GetFree()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = (nextIndex + i) % pool.Count;
            if (!pool[idx].busy)
            {
                nextIndex = (idx + 1) % pool.Count;
                return pool[idx];
            }
        }
        if (!recycleOldest) return null;

        var s = pool[nextIndex];
        if (s.co != null) StopCoroutine(s.co);
        nextIndex = (nextIndex + 1) % pool.Count;
        return s;
    }

    /// <summary>
    /// 화면 정가운데에 혈흔 생성
    /// </summary>
    public void ShowHit(float severity = 1f)
    {
        if (!splatPrefab || !container) return;

        var s = GetFree();
        if (s == null || s.img == null) return;

        // 비주얼 셋업 (프리팹 RGB 유지, alpha만 0에서 시작)
        var col = s.img.color;
        s.origAlpha = col.a <= 0f ? 1f : col.a;
        col.a = 0f;
        s.img.color = col;

        s.rt.localScale = Vector3.one * scale;
        s.rt.localRotation = Quaternion.identity;

        // 무조건 중앙
        s.rt.anchoredPosition = Vector2.zero;

        s.img.enabled = true;
        s.busy = true;
        if (s.co != null) StopCoroutine(s.co);

        s.co = StartCoroutine(FadeAndDrip(s, lifetime, severity));
    }

    IEnumerator FadeAndDrip(Splat s, float lifetime, float severity)
    {
        float t = 0f;
        var col = s.img.color;
        while (t < lifetime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / lifetime);

            col.a = baseAlpha * s.origAlpha * alphaCurve.Evaluate(u);
            s.img.color = col;

            yield return null;
        }

        s.img.enabled = false;
        s.busy = false;
        s.co = null;
    }

    // 외부 간편 호출
    public static void TryShow(float severity = 1f) => Instance?.ShowHit(severity);

    // 인스펙터 메뉴에서 강제 테스트
    [ContextMenu("DEBUG/Spawn One (Center)")]
    void DebugSpawnOneCenter()
    {
        ShowHit();
        Debug.Log("[BloodScreenFX] Spawned test splat at center.");
    }
}
