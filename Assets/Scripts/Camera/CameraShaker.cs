using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Parameters")]
    public float duration = 0.3f;
    public float magnitude = 0.15f;

    [Header("Axes")]
    [Tooltip("가로(좌우) 흔들림 사용")]
    public bool horizontal = true;   // 기본: 좌우만 흔들기
    [Tooltip("세로(상하) 흔들림 사용")]
    public bool vertical = false;  // 기본: 사용 안 함

    public Vector3 CurrentOffset { get; private set; } = Vector3.zero;
    public bool IsShaking => _shakeCoroutine != null;

    Coroutine _shakeCoroutine;

    public void StartShake(float? dur = null, float? mag = null)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(dur ?? duration, mag ?? magnitude));
    }

    public void StopShake()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = null;
        CurrentOffset = Vector3.zero;
    }

    IEnumerator ShakeCoroutine(float dur, float mag)
    {
        float t = 0f;
        while (t < dur)
        {
            float rx = Random.Range(-1f, 1f); // 가로용
            float ry = Random.Range(-1f, 1f); // 세로용

            float damping = 1f - (t / dur);
            float m = mag * damping;

            float ox = horizontal ? rx * m : 0f;
            float oy = vertical ? ry * m : 0f;

            CurrentOffset = new Vector3(ox, oy, 0f);

            t += Time.deltaTime;
            yield return null;
        }
        CurrentOffset = Vector3.zero;
        _shakeCoroutine = null;
    }
}
