using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlashWave : MonoBehaviour
{
    private float direction = 1f;
    private float speed = 10f;
    public float lifetime = 0.4f;

    [Header("AfterImage Settings")]
    public float afterImageInterval = 0.02f;
    public float afterImageLifetime = 0.15f;
    public Color afterImageColor = new Color(1f, 1f, 1f, 0.6f);

    SpriteRenderer sr;
    Coroutine afterImageRoutine;

    //  지금까지 생성된 모든 잔상 저장하는 리스트
    List<GameObject> afterImages = new List<GameObject>();

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // 플레이어와 같은 렌더 레이어로 고정
        sr.sortingLayerName = "Player";   // 네가 쓰는 레이어명 (예: Player)
        sr.sortingOrder = 65;
    }
    public void SetDirection(float dir, float spd)
    {
        direction = Mathf.Sign(dir);
        speed = spd;

        sr.flipX = (direction < 0);

        afterImageRoutine = StartCoroutine(SpawnAfterImages());
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0, 0);
    }

    IEnumerator SpawnAfterImages()
    {
        while (true)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    // 잔상 생성
    // 잔상 생성
    void CreateAfterImage()
    {
        GameObject clone = new GameObject("SlashWave_AfterImage");
        SpriteRenderer cloneSR = clone.AddComponent<SpriteRenderer>();


        cloneSR.sprite = sr.sprite;
        cloneSR.flipX = sr.flipX;
        cloneSR.flipY = sr.flipY;
        cloneSR.color = afterImageColor;


        cloneSR.sortingLayerName = "Player";


        cloneSR.sortingOrder = 65;

        cloneSR.sharedMaterial = sr.sharedMaterial;

        cloneSR.maskInteraction = sr.maskInteraction;
        cloneSR.spriteSortPoint = sr.spriteSortPoint;
        cloneSR.drawMode = sr.drawMode;
        cloneSR.size = sr.size;

        clone.transform.position = transform.position;
        clone.transform.localScale = transform.localScale;

        // 5) 리스트 기록
        afterImages.Add(clone);

        StartCoroutine(FadeAndDestroy(cloneSR, clone));
    }


    IEnumerator FadeAndDestroy(SpriteRenderer cloneSR, GameObject obj)
    {
        float t = 0f;
        Color original = cloneSR.color;

        while (t < afterImageLifetime)
        {
            if (cloneSR == null)
                yield break;
            float alpha = Mathf.Lerp(original.a, 0f, t / afterImageLifetime);
            cloneSR.color = new Color(original.r, original.g, original.b, alpha);
            t += Time.deltaTime;
            yield return null;
        }

        // 삭제 전, 리스트에서 제거
        afterImages.Remove(obj);

        Destroy(obj);
    }

    //  애니메이션 이벤트에서 호출할 함수 (잔상 전체 삭제)
    public void ClearAllAfterImages()
    {
        foreach (var img in afterImages)
        {
            if (img != null)
                Destroy(img);
        }

        afterImages.Clear();
        if (afterImageRoutine != null)
            StopCoroutine(afterImageRoutine);
    }
}
