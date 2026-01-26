using UnityEngine;

public class PlayerDust : MonoBehaviour
{
    [Header("rollDust")]
    public GameObject rollDustPrefab;
    public Transform dustSpawnPoint;
    public float rollDustLifetime = 0.7f;

    [Header("jumpDust")]
    public GameObject jumpDustPrefab;
    public float jumpDustLifetime = 0.7f;

    [Header("attackDust")]
    public GameObject attackDustPrefab;          // 공격 먼지 프리팹
    public Transform attackDustSpawnPoint;       // 없으면 dustSpawnPoint 사용
    public float attackDustLifetime = 0.7f;

    private PlayerMovement movement;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (!movement)
            Debug.LogError("PlayerMovement를 찾지 못했습니다.");
    }

    public void SpawnRollDust()
    {
        if (!rollDustPrefab || !dustSpawnPoint) return;

        var dust = Instantiate(rollDustPrefab, dustSpawnPoint.position, Quaternion.identity);
        // 좌우 반전
        var s = dust.transform.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(movement.LastDirection);
        dust.transform.localScale = s;

        Destroy(dust, rollDustLifetime);
    }

    public void SpawnJumpDust()
    {
        if (!jumpDustPrefab || !dustSpawnPoint) return;

        var dust = Instantiate(jumpDustPrefab, dustSpawnPoint.position, Quaternion.identity);
        Destroy(dust, jumpDustLifetime);
    }

    // === 공격 먼지 (애니메이션 이벤트에서 호출) ===
    public void SpawnAttackDust()
    {
        if (!attackDustPrefab) return;

        var sp = attackDustSpawnPoint ? attackDustSpawnPoint : (dustSpawnPoint ? dustSpawnPoint : transform);
        var dust = Instantiate(attackDustPrefab, sp.position, Quaternion.identity);

        // 좌우 반전
        if (movement)
        {
            var s = dust.transform.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(movement.LastDirection);
            dust.transform.localScale = s;
        }

        Destroy(dust, attackDustLifetime);
    }

    // 이벤트에서 int 파라미터를 주는 세팅이라면 이걸 걸어도 됨 (값은 무시)
    public void SpawnAttackDust(int _)
    {
        SpawnAttackDust();
    }
}
