using System;
using UnityEngine;
using System.Collections;

public class CCTVMove : MonoBehaviour
{
    public float[] moveSpeed;             // 항상 양수로 사용
    public Transform groundCheck;
    public Transform groundCheck2;
    public float groundCheckDistance = 2f;
    public LayerMask groundLayer;
    private SpriteRenderer sr;
    public Transform radarTransform;
    public Transform transform;
    public Animator anim;
    public GameObject radar;
    

    public bool movingRight = false;         
    bool scanC = false;
    bool moveCheck6 = false, moveCheck14 = false;
    private int moveIndex;
    public RaycastHit2D hit;
    public RaycastHit2D hit2;


    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>();
        transform = GetComponent<Transform>();
    }

    void Update()
    {
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        if (info.IsName("Scan1")){
            // Debug.Log("Scan1");
            Scan1();
        }
        else if (info.IsName("Scaning"))
        {
            // Debug.Log("Scaning");

            if (!scanC){
                Scaning();
                scanC = true;
            }
        }
        else if (info.IsName("Scan2")){
            // Debug.Log("Scan2");
            Scan2();
        }
        else if (info.IsName("Move") || info.IsName("Move 0") || info.IsName("Move 1")){
            // Debug.Log("Move");
            // Move();
        }

        hit = Physics2D.Raycast(groundCheck.position, Vector2.up, groundCheckDistance, groundLayer);
        hit2 = Physics2D.Raycast(groundCheck2.position, Vector2.up, groundCheckDistance, groundLayer);

    }

    public void Flip()
    {
        movingRight = !movingRight;

        // 스프라이트 position 유지 보정
        if (!movingRight)
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(width, 0f, 0f);
        }
        else
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(-width, 0f, 0f);
        }

        // 비주얼 반전
        if (sr) sr.flipX = !sr.flipX;

        // 발 앞 체크 포인트 좌우 미러
        if (groundCheck)
        {
            var lp = groundCheck.localPosition;
            lp.x = -lp.x;
            groundCheck.localPosition = lp;
        }

        if (groundCheck2)
        {
            var lp = groundCheck2.localPosition;
            lp.x = -lp.x;
            groundCheck2.localPosition = lp;
        }

        Vector3 pos = radarTransform.localPosition;
        pos.x = -pos.x;
        radarTransform.localPosition = pos;

        
        
    }


    public void InMove2()
    {
        if (movingRight)
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(width, 0f, 0f);
        }
        else
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(-width, 0f, 0f);
        }
    }

    public void OutMove2()
    {
        if (movingRight)
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(-width, 0f, 0f);
        }
        else
        {
            float width = sr.bounds.size.x;
            transform.position += new Vector3(width, 0f, 0f);
        }
    }
    /*
    void Move()
    {
        
        // 방향
        Sprite currentSprite = sr.sprite;

        if (movingRight)
        {
            if (currentSprite.name == "Frame_6" && !moveCheck6)
            {
                float width = sr.bounds.size.x;
                transform.position += new Vector3(width, 0f, 0f);
                moveCheck6 = true;
                moveCheck14 = false;
            }
            if (currentSprite.name == "Frame_14" && !moveCheck14)
            {
                float width = sr.bounds.size.x;
                transform.position += new Vector3(-width, 0f, 0f);
                moveCheck14 = true;
                moveCheck6 = false;
            }
        }
        else
        {
            
            if (currentSprite.name == "Frame_6" && !moveCheck6)
            {
                float width = sr.bounds.size.x;
                transform.position += new Vector3(-width, 0f, 0f);
                moveCheck6 = true;
                moveCheck14 = false;
                Debug.Log("6");
                Debug.Log("moveCheck6 = " + moveCheck6);
                Debug.Log("moveCheck14 = " + moveCheck14);
            }
            if (currentSprite.name == "Frame_14" && !moveCheck14)
            {
                float width = sr.bounds.size.x;
                transform.position += new Vector3(width, 0f, 0f);
                moveCheck14 = true;
                moveCheck6 = false;
                Debug.Log("14");
                Debug.Log("moveCheck6 = " + moveCheck6);
                Debug.Log("moveCheck14 = " + moveCheck14);
            }
        }


    }
    */
    void Scan1()
    {
        anim.SetBool("isScan", true);
    }

    void Scaning()
    {
        radar.SetActive(true);
        StartCoroutine(Co());
    }

    void Scan2()
    {
        scanC = false;
        moveIndex = 0;
    }

    IEnumerator Co()
    {
        Debug.Log("코루틴 시작");
        yield return new WaitForSeconds(3f);  // 3초 대기 (timeScale 영향)
        radar.SetActive(false);
        anim.SetBool("isScan", false);

        Debug.Log("코루틴 종료");


    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.up * groundCheckDistance);
        }

        if (groundCheck2 != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheck2.position, groundCheck2.position + Vector3.up * groundCheckDistance);
        }

    }
}
