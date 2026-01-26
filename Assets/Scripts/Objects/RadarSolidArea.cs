using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class RadarSolidArea : MonoBehaviour
{
    [Header("FOV 설정")]
    public float distance = 10f;
    [Range(1f, 170f)] public float fovAngle = 60f;   // 시야 각
    public float centerAngle = -90f;                  // 기준 각(오른쪽이 0°, 위가 90°)

    [Header("레이어")]
    public LayerMask occluderMask;   // 벽/바닥 등 시야를 가리는 레이어
    public string targetTag = "Player";

    [Header("방향 기준(스프라이트 플립)")]
    public SpriteRenderer faceSource; // flipX 기준

    [Header("리스폰")]
    public Respawn respawn;

    Mesh _mesh;
    MeshFilter _mf;
    PolygonCollider2D _poly;

    const float biasDeg = 0.5f;      // 모서리 보정 각도
    const float dedupEps = 0.002f;   // 근접 중복 제거 임계(유닛)

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _poly = GetComponent<PolygonCollider2D>();
        _mesh = new Mesh { name = $"RadarMesh_{GetInstanceID()}" };
        _mf.mesh = _mesh;

        if (!faceSource) faceSource = GetComponentInParent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Vector2 origin = transform.position;

        // 1) 중심각/경계각(도)
        bool facingRight = faceSource ? !faceSource.flipX : true;
        float centerDeg = facingRight ? centerAngle : (180f - centerAngle);
        centerDeg = Wrap360(centerDeg);
        float halfDeg = fovAngle * 0.5f;
        float leftEdgeDeg = centerDeg - halfDeg;
        float rightEdgeDeg = centerDeg + halfDeg;

        // 2) 시야 반경 내 콜라이더 꼭짓점 수집
        var cols = Physics2D.OverlapCircleAll(origin, distance, occluderMask);
        List<Vector2> candidates = new();

        foreach (var c in cols)
        {
            if (c is BoxCollider2D box) AddBoxVertices(box, candidates);
            else if (c is PolygonCollider2D pc) AddPolyVertices(pc, candidates);
            else if (c is EdgeCollider2D ec) AddEdgeVertices(ec, candidates);
            else if (c is CompositeCollider2D cc) AddCompositeVertices(cc, candidates);
        }

        // 3) 후보 필터링 + 모서리 보강
        List<Vector2> inside = new();

        foreach (var v in candidates)
        {
            if (!IsInsideFOV_ByDelta(origin, v, centerDeg, halfDeg, distance))
                continue;

            if (IsVisible(origin, v))
                inside.Add(v);

            // 모서리 보정 레이(±biasDeg)
            AddCornerWithBiasDeg(origin, v, centerDeg, halfDeg, inside);
        }

        // 4) FOV 경계 보강(좌/우 끝 레이)
        AddEdgePointDeg(origin, leftEdgeDeg, inside);
        AddEdgePointDeg(origin, rightEdgeDeg, inside);

        if (inside.Count < 2) return;

        // 5) center 기준 Δ각으로 정렬 (왼쪽 경계→오른쪽 경계)
        inside.Sort((a, b) =>
        {
            float da = DeltaFromCenterDeg(origin, a, centerDeg);
            float db = DeltaFromCenterDeg(origin, b, centerDeg);
            return da.CompareTo(db);
        });

        // (옵션) 근접 중복 제거
        DedupClosePoints(inside, dedupEps);

        // 6) Mesh 갱신
        UpdateMesh(inside, origin);

        // 7) Collider 갱신
        UpdateCollider(inside, origin);
    }

    // ─────────── 꼭짓점 수집 ───────────
    void AddBoxVertices(BoxCollider2D box, List<Vector2> list)
    {
        var t = box.transform;
        Vector2 s = box.size;
        Vector2 off = box.offset;
        Vector2[] v = {
            off + new Vector2(-s.x,-s.y)*.5f, off + new Vector2(-s.x, s.y)*.5f,
            off + new Vector2( s.x, s.y)*.5f, off + new Vector2( s.x,-s.y)*.5f
        };
        foreach (var p in v) list.Add(t.TransformPoint(p));
    }

    void AddPolyVertices(PolygonCollider2D poly, List<Vector2> list)
    {
        var t = poly.transform;
        for (int p = 0; p < poly.pathCount; p++)
        {
            var path = poly.GetPath(p);
            foreach (var pt in path)
                list.Add(t.TransformPoint(pt));
        }
    }

    void AddEdgeVertices(EdgeCollider2D edge, List<Vector2> list)
    {
        var t = edge.transform;
        foreach (var p in edge.points)
            list.Add(t.TransformPoint(p));
    }

    void AddCompositeVertices(CompositeCollider2D comp, List<Vector2> list)
    {
        var t = comp.transform;
        int pc = comp.pathCount;
        for (int p = 0; p < pc; p++)
        {
            int vc = comp.GetPathPointCount(p);
            var arr = new Vector2[vc];
            comp.GetPath(p, arr);
            foreach (var pt in arr)
                list.Add(t.TransformPoint(pt));
        }
    }

    // ─────────── 각/판정 유틸(도) ───────────
    static float Wrap360(float deg) => (deg % 360f + 360f) % 360f;

    static Vector2 DirFromDeg(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    static float DeltaFromCenterDeg(Vector2 origin, Vector2 point, float centerDeg)
    {
        float angDeg = Mathf.Atan2(point.y - origin.y, point.x - origin.x) * Mathf.Rad2Deg;
        return Mathf.DeltaAngle(centerDeg, angDeg); // –180..+180
    }

    bool IsInsideFOV_ByDelta(Vector2 origin, Vector2 point, float centerDeg, float halfDeg, float maxDist)
    {
        Vector2 d = point - origin;
        if (d.sqrMagnitude > maxDist * maxDist) return false;

        float delta = Mathf.Abs(DeltaFromCenterDeg(origin, point, centerDeg));
        return delta <= halfDeg + 1e-4f;
    }

    bool IsVisible(Vector2 origin, Vector2 target)
    {
        Vector2 dir = target - origin;
        float dist = dir.magnitude;
        var hit = Physics2D.Raycast(origin, dir.normalized, dist, occluderMask);

        if (!hit.collider) return true;
        return Vector2.Distance(hit.point, target) < 0.01f;
    }

    // 모서리 보정 레이 (±biasDeg). FOV 밖이면 제외.
    void AddCornerWithBiasDeg(Vector2 origin, Vector2 corner, float centerDeg, float halfDeg, List<Vector2> list)
    {
        // 기준 각
        float ang = Mathf.Atan2(corner.y - origin.y, corner.x - origin.x) * Mathf.Rad2Deg;

        for (int i = -1; i <= 1; i += 2) // -bias, +bias
        {
            float a = ang + i * biasDeg;
            // FOV 바깥은 스킵
            float delta = Mathf.Abs(Mathf.DeltaAngle(centerDeg, a));
            if (delta > halfDeg + 1e-4f) continue;

            Vector2 d = DirFromDeg(a);
            var hit = Physics2D.Raycast(origin, d, distance, occluderMask);
            list.Add(hit.collider ? hit.point : origin + d * distance);
        }
    }

    void AddEdgePointDeg(Vector2 origin, float edgeDeg, List<Vector2> list)
    {
        Vector2 dir = DirFromDeg(edgeDeg);
        var hit = Physics2D.Raycast(origin, dir, distance, occluderMask);
        list.Add(hit.collider ? hit.point : origin + dir * distance);
    }

    // ─────────── Mesh & Collider ───────────
    void UpdateMesh(List<Vector2> worldPts, Vector2 origin)
    {
        _mesh.Clear();
        List<Vector3> verts = new() { Vector3.zero };

        foreach (var p in worldPts)
            verts.Add(transform.InverseTransformPoint(p));

        _mesh.SetVertices(verts);

        int triCount = verts.Count - 2;
        if (triCount <= 0)
        {
            _mesh.triangles = System.Array.Empty<int>();
            _mesh.RecalculateBounds();
            return;
        }

        int[] tris = new int[triCount * 3];
        for (int i = 0; i < triCount; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
    }

    void UpdateCollider(List<Vector2> worldPts, Vector2 origin)
    {
        Vector2[] local = new Vector2[worldPts.Count + 1];
        local[0] = Vector2.zero;
        for (int i = 0; i < worldPts.Count; i++)
            local[i + 1] = transform.InverseTransformPoint(worldPts[i]);

        _poly.pathCount = 1;
        _poly.SetPath(0, local);
    }

    // ─────────── 유틸: 근접 중복 제거 ───────────
    void DedupClosePoints(List<Vector2> pts, float eps)
    {
        if (pts.Count < 2) return;
        List<Vector2> outList = new(pts.Count);
        outList.Add(pts[0]);
        for (int i = 1; i < pts.Count; i++)
        {
            if ((pts[i] - outList[outList.Count - 1]).sqrMagnitude > eps * eps)
                outList.Add(pts[i]);
        }
        // 마지막-첫번째가 너무 가깝다면 마지막 제거(삼각분할/콜라이더 안정화)
        if (outList.Count >= 2 && (outList[0] - outList[^1]).sqrMagnitude < eps * eps)
            outList.RemoveAt(outList.Count - 1);

        pts.Clear();
        pts.AddRange(outList);
    }

    // ─────────── 플레이어 감지 ───────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
            respawn.PlayerRespawn();
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
            Debug.Log("플레이어 범위 이탈");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        bool facingRight = faceSource ? !faceSource.flipX : true;
        float centerDeg = facingRight ? centerAngle : (180f - centerAngle);
        centerDeg = Wrap360(centerDeg);
        float halfDeg = fovAngle * 0.5f;

        Vector2 o = transform.position;
        Vector2 L = DirFromDeg(centerDeg - halfDeg);
        Vector2 R = DirFromDeg(centerDeg + halfDeg);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(o, o + L * distance);
        Gizmos.DrawLine(o, o + R * distance);
    }
#endif
}
