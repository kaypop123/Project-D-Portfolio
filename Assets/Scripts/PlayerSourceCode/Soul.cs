using UnityEngine;

public class Soul : MonoBehaviour
{
    public int soulAmount = 1; // 이 소울이 주는 양

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어에 CharacterTransform 컴포넌트가 있으면 소울 주기
        CharacterTransform ct = other.GetComponent<CharacterTransform>();
        if (ct != null)
        {
            ct.AddSoul(soulAmount);
            Destroy(gameObject); // 먹은 소울은 사라짐
        }
    }
}