using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLimit : MonoBehaviour
{
    private float camHalfWidth;
    private float camHalfHeight;

    private Vector2 minBound;
    private Vector2 maxBound;

    private Camera cam;

    [Header("현재 맵의 경계 박스")]
    public BoxCollider2D box1;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateBounds(box1);
    }

    void LateUpdate()
    {
        if (box1 == null) return;

        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * Screen.width / Screen.height;

        float clampX = Mathf.Clamp(transform.position.x, minBound.x + camHalfWidth, maxBound.x - camHalfWidth);
        float clampY = Mathf.Clamp(transform.position.y, minBound.y + camHalfHeight, maxBound.y - camHalfHeight);

        transform.position = new Vector3(clampX, clampY, transform.position.z);
    }

    // 포탈에서 호출할 함수
    public void UpdateBounds(BoxCollider2D newBox)
    {
        box1 = newBox;
        minBound = box1.bounds.min;
        maxBound = box1.bounds.max;
    }
}


