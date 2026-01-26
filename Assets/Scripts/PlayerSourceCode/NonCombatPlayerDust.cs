using UnityEngine;

public class NonCombatPlayerDust : MonoBehaviour
{
    [Header("jumpDust")]
    public GameObject jumpDustPrefab;
    public float jumpDustLifetime = 0.7f;
    public Transform dustSpawnPoint;

    private NonCombatPlayerMovement Movement; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Movement = GetComponent<NonCombatPlayerMovement>();

    }

    public void SpawnJumpDust()
    {
        if (!jumpDustPrefab || !dustSpawnPoint) return;
        var dust = Instantiate(jumpDustPrefab, dustSpawnPoint.position, Quaternion.identity);
        Destroy(dust, jumpDustLifetime);
    }
}
