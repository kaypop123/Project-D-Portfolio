using UnityEngine;

public class SmoothCameraFollow2D : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;

    [Header("Camera Control")]
    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Dead Zone (width, height)")]
    public Vector2 deadZoneSize = new Vector2(3f, 2f);

    [Header("Recentering")]
    public bool recenterWhenInsideDeadZone = true;
    public float recenterSmoothTime = 0.2f;

    [Header("Optional Shaker")]
    public CameraShaker shaker;

    [Header("Camera Boundary (Optional)")]
    public BoxCollider2D boundary;   // ← 제한 박스 (없으면 제한 안 함)

    Vector3 _basePos;
    Vector3 _vel;
    Camera _cam;

    Vector2 _minBound;
    Vector2 _maxBound;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                target = playerObj.transform;
            else
                Debug.LogWarning("경고: 씬에서 'Player' 태그를 찾을 수 없습니다! 태그 설정을 확인하세요.");
        }

        if (!shaker) shaker = GetComponent<CameraShaker>();

        if (target != null)
        {
            _basePos = target.position + offset;
            transform.position = _basePos;
        }
        else
        {
            _basePos = transform.position;
        }

        CacheBounds(boundary);
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 center = target.position + offset;
        Vector3 desired = _basePos;

        float halfW = deadZoneSize.x * 0.5f;
        float halfH = deadZoneSize.y * 0.5f;
        Vector3 delta = center - _basePos;

        if (delta.x > halfW) desired.x = center.x - halfW;
        else if (delta.x < -halfW) desired.x = center.x + halfW;
        else if (recenterWhenInsideDeadZone) desired.x = center.x;
        else desired.x = _basePos.x;

        if (delta.y > halfH) desired.y = center.y - halfH;
        else if (delta.y < -halfH) desired.y = center.y + halfH;
        else desired.y = _basePos.y;

        desired.z = offset.z;

        float useSmooth = (recenterWhenInsideDeadZone && recenterSmoothTime > 0f)
            ? recenterSmoothTime
            : smoothTime;

        _basePos = Vector3.SmoothDamp(_basePos, desired, ref _vel, useSmooth);

        Vector3 shake = (shaker != null) ? shaker.CurrentOffset : Vector3.zero;
        Vector3 finalPos = _basePos + shake;

        //  여기서만 바운더리 제한 적용
        if (boundary != null)
            finalPos = ClampToBounds(finalPos);

        transform.position = finalPos;
    }

    public void RecenterNow()
    {
        if (!target) return;
        _vel = Vector3.zero;
        _basePos = target.position + offset;

        Vector3 shake = (shaker != null) ? shaker.CurrentOffset : Vector3.zero;
        Vector3 finalPos = _basePos + shake;

        if (boundary != null)
            finalPos = ClampToBounds(finalPos);

        transform.position = finalPos;
    }

    void CacheBounds(BoxCollider2D b)
    {
        if (b == null) return;
        _minBound = b.bounds.min;
        _maxBound = b.bounds.max;
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        float minX = _minBound.x + halfW;
        float maxX = _maxBound.x - halfW;
        float minY = _minBound.y + halfH;
        float maxY = _maxBound.y - halfH;

        if (minX > maxX) pos.x = (_minBound.x + _maxBound.x) * 0.5f;
        else pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY) pos.y = (_minBound.y + _maxBound.y) * 0.5f;
        else pos.y = Mathf.Clamp(pos.y, minY, maxY);

        pos.z = offset.z;
        return pos;
    }

    /// <summary>
    /// 포탈, 방 이동, 보스룸 진입 시 호출
    /// </summary>
    public void SetBoundary(BoxCollider2D newBoundary, bool snap = true)
    {
        boundary = newBoundary;
        CacheBounds(boundary);

        if (snap)
            RecenterNow();
    }

    void OnDrawGizmos()
    {
        Vector3 drawCenter = Application.isPlaying ? _basePos : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(drawCenter, new Vector3(deadZoneSize.x, deadZoneSize.y, 0f));
    }
}
