using System.Collections;
using UnityEngine;

public class HitImpactManager : MonoBehaviour
{
    [Header("Spawn Point (옵션)")]
    public Transform pos;                          // 기본 호출용 마커(Play/PlayFacing에서만 사용)

    [Header("Effects (랜덤 선택)")]
    public GameObject[] effects;                   // 임팩트 프리팹 후보 (랜덤)
    public ParticleSystem extraParticle;           // 선택: 추가 파티클

    [Header("Timing")]
    public float lifetime = 0.35f;                 // 파티클 없는 임팩트 제거 시간

    [Header("Facing 기본값")]
    public bool defaultFacingRight = true;         // Play() 기본 방향
    public bool flipEffectToFacing = true;         // 임팩트 좌/우 플립

    [Header("Facing by Attack (추천)")]
    [Tooltip("공격 방향 X성분이 이 값보다 작으면 이전/기본 방향 유지")]
    public float attackDirDeadZoneX = 0.05f;

    // ====== 데칼: 임팩트 인덱스와 1:1 매칭 (Prefab 전용) ======
    [Header("Impact Decals (Prefab, index-mapped)")]
    [Tooltip("effects 배열과 같은 인덱스로 매칭되는 데칼 프리팹 배열")]
    public GameObject[] decalPrefabs;
    public bool leaveDecal = true;
    public bool flipDecalToFacing = true;
    public Vector2 decalScaleRange = new Vector2(0.6f, 1.2f);
    public bool decalRandomRotateZ = true;
    public Vector2 decalOffset = Vector2.zero;
    public float decalZ = 0f;
    public string decalSortingLayerName = "GroundDecal";
    public int decalSortingOrder = 5;
    public Transform decalParent = null;
    [Tooltip("0이면 영구, >0이면 서서히 페이드아웃 후 삭제")]
    public float decalLife = 0f;
    [Header("Extra Random Effects (no decal match)")]
    public GameObject[] extraEffects;        // 데칼 매칭 없이 섞을 임팩트 풀
    public bool spawnExtra = true;           // 추가 임팩트 사용 여부
    [Range(0f, 1f)] public float extraSpawnChance = 1f; // 확률(원하면 0~1)
    public bool flipExtraToFacing = true;    // 방향 반전 여부
    public float extraLifetime = 0.35f;      // 파티클 없는 경우의 수명
    public bool allowDuplicateWithMain = true; // 메인과 같은 프리팹 허용?

    // 내부 상태(데드존에서 방향 유지)
    bool _lastFacingRight = true;
    bool _initializedFacing = false;

    // ==================== Public API ====================

    public void Play() => PlayFacing(defaultFacingRight);

    public void PlayFacing(bool facingRight)
    {
        if (!pos) return;
        PlayAtFacing(pos.position, facingRight);
    }

    public void PlayAtFacing(Vector3 worldPoint, bool facingRight)
    {
        if (effects == null || effects.Length == 0) return;

        Vector3 p = worldPoint; p.z = 0f;

        // --- 메인 임팩트 (기존 로직) ---
        int idx = Random.Range(0, effects.Length);
        var effectPrefab = effects[idx];
        if (!effectPrefab) return;

        var go = Instantiate(effectPrefab, p, Quaternion.identity);
        if (flipEffectToFacing)
        {
            var sc = go.transform.localScale;
            sc.x = Mathf.Abs(sc.x) * (facingRight ? +1f : -1f);
            go.transform.localScale = sc;
        }

        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true); ps.Play(true);
            float killAfter = ps.main.duration + ps.main.startLifetime.constantMax + 0.2f;
            Destroy(go, killAfter);
        }
        else Destroy(go, lifetime);

        if (extraParticle)
        {
            var ep = Instantiate(extraParticle, p, Quaternion.identity);
            if (flipEffectToFacing)
            {
                var eps = ep.transform.localScale;
                eps.x = Mathf.Abs(eps.x) * (facingRight ? +1f : -1f);
                ep.transform.localScale = eps;
            }
            ep.Play();
            Destroy(ep.gameObject, ep.main.duration + ep.main.startLifetime.constantMax + 0.2f);
        }

        // --- ★ 추가 임팩트 (데칼과 무관) ---
        if (spawnExtra && extraEffects != null && extraEffects.Length > 0 && Random.value <= extraSpawnChance)
        {
            // 중복 비허용이면 메인 프리팹과 다른 것 고르기
            GameObject extraPrefab = null;
            if (allowDuplicateWithMain)
            {
                extraPrefab = extraEffects[Random.Range(0, extraEffects.Length)];
            }
            else
            {
                // extraEffects 안에 메인 프리팹이 섞여 있을 수 있다고 가정하고 회피
                for (int tries = 0; tries < 5; tries++)
                {
                    var cand = extraEffects[Random.Range(0, extraEffects.Length)];
                    if (cand != effectPrefab) { extraPrefab = cand; break; }
                }
                if (extraPrefab == null) extraPrefab = extraEffects[Random.Range(0, extraEffects.Length)];
            }

            if (extraPrefab)
            {
                var extraGo = Instantiate(extraPrefab, p, Quaternion.identity);
                if (flipExtraToFacing)
                {
                    var sc2 = extraGo.transform.localScale;
                    sc2.x = Mathf.Abs(sc2.x) * (facingRight ? +1f : -1f);
                    extraGo.transform.localScale = sc2;
                }

                var ps2 = extraGo.GetComponentInChildren<ParticleSystem>();
                if (ps2 != null)
                {
                    ps2.Clear(true); ps2.Play(true);
                    float killAfter2 = ps2.main.duration + ps2.main.startLifetime.constantMax + 0.2f;
                    Destroy(extraGo, killAfter2);
                }
                else Destroy(extraGo, extraLifetime);
            }
        }

        // --- 데칼은 '메인 임팩트'의 idx만 사용 (추가 임팩트와 매칭 없음) ---
        SpawnDecalByIndex(idx, p, facingRight);
    }


    public void PlayFromAttackerAt(Transform attacker, Vector3 worldPoint)
    {
        bool facingRight = ComputeFacingFromAttacker(attacker, worldPoint);
        PlayAtFacing(worldPoint, facingRight);
    }

    public void PlayFromAttacker(Transform attacker)
    {
        Vector3 p = pos ? pos.position : Vector3.zero;
        bool facingRight = ComputeFacingFromAttacker(attacker, p);
        PlayAtFacing(p, facingRight);
    }

    public void PlayFromOriginAt(Vector2 attackOrigin, Vector3 worldPoint)
    {
        bool shouldKickRight = ComputeFacingFromPoints(attackOrigin, worldPoint);
        PlayAtFacing(worldPoint, shouldKickRight);
    }

    // ==================== Facing helpers ====================

    bool ComputeFacingFromPoints(Vector2 attackOrigin, Vector3 hitPoint)
    {
        Vector2 dir = (Vector2)hitPoint - attackOrigin;

        if (Mathf.Abs(dir.x) < attackDirDeadZoneX)
        {
            InitFacingIfNeeded();
            return _lastFacingRight;
        }

        _lastFacingRight = (dir.x >= 0f);
        _initializedFacing = true;
        return _lastFacingRight;
    }

    bool ComputeFacingFromAttacker(Transform attacker, Vector3 hitPoint)
    {
        if (!attacker)
        {
            InitFacingIfNeeded();
            return _lastFacingRight;
        }

        Vector2 dir = (Vector2)hitPoint - (Vector2)attacker.position;

        if (Mathf.Abs(dir.x) < attackDirDeadZoneX)
        {
            InitFacingIfNeeded();
            return _lastFacingRight;
        }

        _lastFacingRight = dir.x >= 0f;
        _initializedFacing = true;
        return _lastFacingRight;
    }

    void InitFacingIfNeeded()
    {
        if (!_initializedFacing)
        {
            _lastFacingRight = defaultFacingRight;
            _initializedFacing = true;
        }
    }

    // ==================== Decal (Prefab only) ====================

    void SpawnDecalByIndex(int idx, Vector3 hitPos, bool facingRight)
    {
        if (!leaveDecal || decalPrefabs == null || idx >= decalPrefabs.Length) return;

        var decalPrefab = decalPrefabs[idx];
        if (!decalPrefab) return;

        Vector3 p = hitPos;
        p.z = decalZ;
        p.x += Random.Range(-decalOffset.x, decalOffset.x);
        p.y += Random.Range(-decalOffset.y, decalOffset.y);

        Quaternion rot = decalRandomRotateZ
            ? Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward)
            : Quaternion.identity;

        var go = Instantiate(decalPrefab, p, rot, decalParent);

        float s = Random.Range(decalScaleRange.x, decalScaleRange.y);
        var baseScale = go.transform.localScale;
        var newScale = baseScale * s;
        if (flipDecalToFacing)
            newScale.x = Mathf.Abs(baseScale.x) * s * (facingRight ? +1f : -1f);
        go.transform.localScale = newScale;

        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!string.IsNullOrEmpty(decalSortingLayerName))
                sr.sortingLayerName = decalSortingLayerName;
            sr.sortingOrder = decalSortingOrder;
        }

        if (decalLife > 0f)
        {
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr) StartCoroutine(FadeAndKill(sr, decalLife));
            else Destroy(go, decalLife);
        }
    }

    IEnumerator FadeAndKill(SpriteRenderer sr, float t)
    {
        if (!sr) yield break;
        float t0 = Time.time;
        Color c0 = sr.color;
        while (sr && Time.time - t0 < t)
        {
            float k = 1f - (Time.time - t0) / t;
            var c = c0; c.a = c0.a * k;
            sr.color = c;
            yield return null;
        }
        if (sr) Destroy(sr.gameObject);
    }
}