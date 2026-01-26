using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// ===== PixelPerfectCamera 네임스페이스 자동 대응 =====
#if UNITY_6000_0_OR_NEWER
using PPC = UnityEngine.Rendering.Universal;                 // Unity 6+ URP
#elif UNITY_2021 || UNITY_2022
using PPC = UnityEngine.Experimental.Rendering.Universal;    // 구 URP
// (만약 2D Pixel Perfect 패키지를 쓴다면 위 둘 주석 처리하고 ↓ 한 줄만)
// using PPC = UnityEngine.U2D;
#else
using PPC = UnityEngine.Rendering.Universal;
#endif
// ===============================================

public class ResolutionMenuUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Dropdown aspectDropdown;
    public Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle enforceAspectToggle;
    public Button applyButton;

    [Header("Camera")]
    public Camera targetCamera;

    // ===== [수정됨 1] Pixel Perfect Camera 관련 변수 삭제 =====
    // public PPC.PixelPerfectCamera pixelPerfectCamera; // 이제 이 스크립트에서 직접 제어하지 않음
    // public Vector2Int baseRef16x9 = new Vector2Int(480, 270); // 삭제
    // public bool autoScalePixelPerfect = true; // 삭제
    // public Toggle setPPCRefFromResToggle; // 삭제

    [Header("Letterbox 옵션")]
    public bool useViewportLetterbox = true;

    private readonly List<ResolutionDatabase.Res> _currentChoices = new();

    void Awake()
    {
        BuildAspectDropdown();
        if (aspectDropdown) aspectDropdown.onValueChanged.AddListener(_ => RebuildResolutionDropdown());
        if (applyButton) applyButton.onClick.AddListener(Apply);

        if (fullscreenToggle) fullscreenToggle.isOn = Screen.fullScreenMode != FullScreenMode.Windowed;
        if (enforceAspectToggle) enforceAspectToggle.isOn = true;

        // ===== [수정됨 2] 삭제된 UI 관련 초기화 코드 제거 =====
        // if (setPPCRefFromResToggle) setPPCRefFromResToggle.isOn = false;

        RebuildResolutionDropdown();
    }

    // BuildAspectDropdown(), RebuildResolutionDropdown() 함수는 수정할 필요 없이 완벽하므로 그대로 둡니다.
    #region Dropdown Build Logic (수정 없음)
    void BuildAspectDropdown()
    {
        var options = new List<string> { "Auto (Native)", "16:9", "16:10", "21:9", "32:9", "3:2", "4:3", "5:4" };
        aspectDropdown.ClearOptions();
        aspectDropdown.AddOptions(options);

        var g = ResolutionDatabase.GuessGroupFromWH(Display.main.systemWidth, Display.main.systemHeight);
        int idx = g switch
        {
            ResolutionDatabase.AspectGroup._16x9 => 1,
            ResolutionDatabase.AspectGroup._16x10 => 2,
            ResolutionDatabase.AspectGroup._21x9 => 3,
            ResolutionDatabase.AspectGroup._32x9 => 4,
            ResolutionDatabase.AspectGroup._3x2 => 5,
            ResolutionDatabase.AspectGroup._4x3 => 6,
            ResolutionDatabase.AspectGroup._5x4 => 7,
            _ => 0
        };
        aspectDropdown.value = idx;
    }

    void RebuildResolutionDropdown()
    {
        _currentChoices.Clear();
        var options = new List<string>();

        if (aspectDropdown.value == 0) // Auto (Native)
        {
            int dw = Display.main.systemWidth;
            int dh = Display.main.systemHeight;
            var nativeGroup = ResolutionDatabase.GuessGroupFromWH(dw, dh);

            var list = ResolutionDatabase.All
                        .Where(r => r.group == nativeGroup)
                        .OrderBy(r => r.w * r.h);

            _currentChoices.Add(new ResolutionDatabase.Res(dw, dh, ResolutionDatabase.AspectGroup.Native, $"Native {dw}×{dh}"));
            options.Add(_currentChoices[^1].label);

            foreach (var r in list)
            {
                _currentChoices.Add(r);
                options.Add(r.label);
            }
        }
        else
        {
            var wanted = (ResolutionDatabase.AspectGroup)(aspectDropdown.value - 1);
            var list = ResolutionDatabase.All
                        .Where(r => r.group == wanted)
                        .OrderBy(r => r.w * r.h);

            foreach (var r in list)
            {
                _currentChoices.Add(r);
                options.Add(r.label);
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(resolutionDropdown.value, 0, Mathf.Max(0, options.Count - 1));
    }
    #endregion

    public void Apply()
    {
        if (_currentChoices.Count == 0) return;

        var chosen = _currentChoices[resolutionDropdown.value];
        var mode = (fullscreenToggle && fullscreenToggle.isOn)
                     ? FullScreenMode.FullScreenWindow
                     // ===== [수정됨 3] 창모드 종류 명시적으로 변경 (선택사항, 더 안정적) =====
                     : FullScreenMode.Windowed;

        // 실제 출력 해상도 적용 (이 부분은 원래도 완벽했음)
        Screen.SetResolution(chosen.w, chosen.h, mode);
        Debug.Log($"[ResolutionMenuUI] SetResolution {chosen.w}x{chosen.h}  mode={mode}");

        // 비율 강제(레터/필러박스) (이 부분도 원래 완벽했음)
        if (enforceAspectToggle && enforceAspectToggle.isOn && targetCamera)
        {
            var group = (aspectDropdown.value == 0)
                ? ResolutionDatabase.GuessGroupFromWH(chosen.w, chosen.h)
                : (ResolutionDatabase.AspectGroup)(aspectDropdown.value - 1);

            float targetAspect = group switch
            {
                ResolutionDatabase.AspectGroup._16x9 => 16f / 9f,
                ResolutionDatabase.AspectGroup._16x10 => 16f / 10f,
                ResolutionDatabase.AspectGroup._21x9 => 21f / 9f,
                ResolutionDatabase.AspectGroup._32x9 => 32f / 9f,
                ResolutionDatabase.AspectGroup._3x2 => 3f / 2f,
                ResolutionDatabase.AspectGroup._4x3 => 4f / 3f,
                ResolutionDatabase.AspectGroup._5x4 => 5f / 4f,
                _ => (float)chosen.w / chosen.h
            };

            if (useViewportLetterbox) ApplyViewportLetterbox(targetCamera, targetAspect);
        }
        else if (targetCamera)
        {
            targetCamera.rect = new Rect(0, 0, 1, 1); // 레터박스 해제
        }

        // ===== [수정됨 4] Pixel Perfect Camera를 제어하는 모든 코드를 삭제 =====
        // 이 스크립트는 이제 Pixel Perfect Camera의 존재를 전혀 알지 못합니다.
        // 따라서 Pixel Perfect Camera는 항상 Inspector에 설정된 고정 Reference Resolution 값만 사용하게 됩니다.
    }

    // ApplyViewportLetterbox() 함수는 수정할 필요 없이 완벽하므로 그대로 둡니다.
    #region Letterbox Logic (수정 없음)
    public static void ApplyViewportLetterbox(Camera cam, float targetAspect)
    {
        float screenAspect = (float)Screen.width / Screen.height;

        if (Mathf.Approximately(targetAspect, screenAspect))
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        if (screenAspect > targetAspect) // 필러박스 (좌우 여백)
        {
            float newW = targetAspect / screenAspect;
            float x = (1f - newW) * 0.5f;
            cam.rect = new Rect(x, 0, newW, 1);
        }
        else // 레터박스 (상하 여백)
        {
            float newH = screenAspect / targetAspect;
            float y = (1f - newH) * 0.5f;
            cam.rect = new Rect(0, y, 1, newH);
        }
    }
    #endregion
}