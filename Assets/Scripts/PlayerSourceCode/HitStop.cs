// HitStop.cs (디버그 강화판)
using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop I;

    [Header("Behavior")]
    [Range(0f, 0.1f)] public float pausedScale = 0f;    // 0=완전정지, 0.02=슬로
    public bool skipIfGamePaused = true;                 // 이미 게임이 0으로 멈췄을 때는 스킵

    [Header("Debug")]
    public bool debugLogs = true;                        // 콘솔 로그 ON/OFF
    public bool showOverlay = true;                      // 화면 좌상단에 남은 시간 표시
    public KeyCode debugKey = KeyCode.None;              // 예: KeyCode.H 로 두면 H키로 테스트
    [Range(0.01f, 0.3f)] public float debugKeyDuration = 0.06f;

    float _origScale;
    float _origFixed;           // 앱 시작 시점의 원본 fixedDeltaTime 저장
    float _origFixedSnapshot;   // HitStop 시작 시점의 fixedDeltaTime 스냅샷
    bool _running;
    float _extendUntilRT = 0f;  // Realtime 종료 시각
    float _startedAtRT = 0f;    // Realtime 시작 시각

    public bool IsRunning => _running;
    public float RemainingRealtime => Mathf.Max(0f, _extendUntilRT - Time.realtimeSinceStartup);

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _origFixed = Time.fixedDeltaTime;

        if (debugLogs)
            Debug.Log($"[HitStop] ready | orig fixed={_origFixed:F4}");
    }

    void Update()
    {
        if (debugKey != KeyCode.None && Input.GetKeyDown(debugKey))
            Do(debugKeyDuration);
    }

    /// <summary>duration(초, 실시간 기준) 동안 히트스톱. 여러 번 호출되면 남은 시간 연장.</summary>
    public void Do(float duration)
    {
        if (duration <= 0f) return;

        if (skipIfGamePaused && Time.timeScale == 0f && !_running)
        {
            if (debugLogs) Debug.Log("[HitStop] skipped (already paused by game)");
            return;
        }

        float newDeadline = Time.realtimeSinceStartup + duration;

        if (_running)
        {
            // 연장
            _extendUntilRT = Mathf.Max(_extendUntilRT, newDeadline);
            if (debugLogs) Debug.Log($"[HitStop] extend → remain={RemainingRealtime * 1000f:F0}ms");
        }
        else
        {
            _extendUntilRT = newDeadline;
            StartCoroutine(Co());
        }
    }

    IEnumerator Co()
    {
        _running = true;
        _startedAtRT = Time.realtimeSinceStartup;

        _origScale = Time.timeScale;
        _origFixedSnapshot = Time.fixedDeltaTime;

        Time.timeScale = pausedScale;
        Time.fixedDeltaTime = _origFixed * Time.timeScale; // 물리도 함께 정지/슬로

        if (debugLogs)
            Debug.Log($"[HitStop] START ts={_origScale:F2}→{Time.timeScale:F2} | fixed={_origFixedSnapshot:F4}→{Time.fixedDeltaTime:F4}");

        // unscaled(Realtime) 기준 대기
        while (Time.realtimeSinceStartup < _extendUntilRT)
            yield return null;

        Time.timeScale = _origScale;
        Time.fixedDeltaTime = _origFixedSnapshot;

        if (debugLogs)
        {
            float durMs = (Time.realtimeSinceStartup - _startedAtRT) * 1000f;
            Debug.Log($"[HitStop] END  dur={durMs:F0}ms | ts restore={Time.timeScale:F2} | fixed restore={Time.fixedDeltaTime:F4}");
        }

        _running = false;
    }

    void OnGUI()
    {
        if (!showOverlay || !_running) return;

        var rect = new Rect(10, 10, 260, 40);
        GUI.color = new Color(1, 1, 0.3f, 0.95f);
        GUI.Box(rect, "");
        GUI.color = Color.black;
        GUI.Label(rect, $"HIT STOP  {RemainingRealtime * 1000f:F0} ms\n(ts={Time.timeScale:F2})");
        GUI.color = Color.white;
    }
}
