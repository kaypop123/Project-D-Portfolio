using UnityEngine;

public class SoulMapSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject soulPrefab;

    [Header("Spawn Points")]
    public Transform[] points;

    [Header("Spawn Rule")]
    public float spawnInterval = 3.0f;     // 몇 초마다 소울 생성할지
    public int maxAliveSouls = 10;         // 맵에 동시에 존재 가능한 소울 수 제한

    [Header("Pop Impulse (min/max)")]
    public float popUpMin = 4.5f;
    public float popUpMax = 7.0f;
    public float popSideMin = 0.8f;
    public float popSideMax = 2.0f;

    private float _nextSpawnTime;
    private int _aliveCount;

    void Update()
    {
        if (Time.time < _nextSpawnTime) return;
        _nextSpawnTime = Time.time + spawnInterval;

        if (_aliveCount >= maxAliveSouls) return;

        SpawnFromRandomPoint();
    }

    public void SpawnFromRandomPoint()
    {
        if (soulPrefab == null)
        {
            Debug.LogError("[SoulMapSpawner] soulPrefab이 비어있음");
            return;
        }

        if (points == null || points.Length == 0)
        {
            Debug.LogError("[SoulMapSpawner] points가 비어있음 (씬에 스폰포인트 배치/할당 필요)");
            return;
        }

        Transform p = points[Random.Range(0, points.Length)];
        Vector3 spawnPos = p.position;

        GameObject soul = Instantiate(soulPrefab, spawnPos, Quaternion.identity);

        // alive 카운트 관리(간단 버전): 소울이 파괴될 때 감소시키려면 아래 SoulLifeTracker 사용 권장
        _aliveCount++;

        // 튕김 물리
        var rb = soul.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            float sideDir = Random.value < 0.5f ? -1f : 1f;
            float side = Random.Range(popSideMin, popSideMax) * sideDir;
            float up = Random.Range(popUpMin, popUpMax);

            rb.AddForce(new Vector2(side, up), ForceMode2D.Impulse);
        }

        // 소울이 사라질 때 alive 카운트 감소시키려면 트래커 부착
        var tracker = soul.AddComponent<SoulLifeTracker>();
        tracker.Init(this);
    }

    // 외부에서 호출될 alive 감소
    public void NotifySoulDestroyed()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }
}

public class SoulLifeTracker : MonoBehaviour
{
    private SoulMapSpawner _owner;

    public void Init(SoulMapSpawner owner) => _owner = owner;

    private void OnDestroy()
    {
        if (_owner != null) _owner.NotifySoulDestroyed();
    }
}

