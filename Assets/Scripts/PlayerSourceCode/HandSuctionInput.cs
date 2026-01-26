using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unity Input System과 HandSuction을 연결하는 어댑터.
/// - Absorb 액션(버튼/트리거 등)으로 흡입 시작/종료
/// - Hold/Toggle 모드 지원
/// - 아날로그 입력(패드 트리거 등) 세기로 흡입 강도 스케일링
/// - PlayerAnimationBinder와 연동하여 석션 애니메이션 On/Off
/// </summary>
[RequireComponent(typeof(HandSuction))]
public class HandSuctionInput : MonoBehaviour
{
    public enum ActivationMode { Hold, Toggle }

    #region Refs
    [Header("Refs")]
    [SerializeField] private HandSuction suction;
    private PlayerAnimationBinder _binder;
    private PlayerMovement _move;
    #endregion

    #region Input
    [Header("Input Actions")]
    [Tooltip("Absorb 액션을 드래그하세요 (InputActionReference). 타입: Button 또는 Value(float) 모두 가능")]
    [SerializeField] private InputActionReference absorbAction;
    #endregion

    #region Behavior
    [Header("Behavior")]
    [SerializeField] private ActivationMode mode = ActivationMode.Hold;
    #endregion

    #region Analog Scaling
    [Header("Analog Scaling (Optional)")]
    [Tooltip("트리거나 축 값(0~1)로 흡입 강도를 스케일링")]
    [SerializeField] private bool scaleByAnalog = true;
    [Tooltip("이 값 이하에선 0으로 간주")]
    [Range(0f, 1f)][SerializeField] private float analogDeadzone = 0.1f;
    [Tooltip("원래 힘 대비 최대 배율 (1=그대로)")]
    [Min(0f)][SerializeField] private float analogMaxMultiplier = 1.25f;
    [Tooltip("아날로그 최저 입력에서의 최소 배율 (0이면 완전 0까지)")]
    [Min(0f)][SerializeField] private float analogMinMultiplier = 0.25f;
    #endregion

    #region State
    private float _baseStrength;
    private bool _subscribed;
    #endregion

    void Reset()
    {
        suction = GetComponent<HandSuction>();
    }

    void Awake()
    {
        if (!suction) suction = GetComponent<HandSuction>();
        _baseStrength = suction ? suction.suctionStrength : 0f;

        _binder = GetComponent<PlayerAnimationBinder>();
        _move = GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        if (absorbAction && absorbAction.action != null && !_subscribed)
        {
            var a = absorbAction.action;
            a.Enable();

            if (mode == ActivationMode.Hold)
            {
                a.started += OnAbsorbStart;
                a.canceled += OnAbsorbCancel;
            }
            else // Toggle
            {
                a.performed += OnAbsorbToggle;
            }
            _subscribed = true;
        }
    }

    void OnDisable()
    {
        if (absorbAction && absorbAction.action != null && _subscribed)
        {
            var a = absorbAction.action;

            a.started -= OnAbsorbStart;
            a.canceled -= OnAbsorbCancel;
            a.performed -= OnAbsorbToggle;

            a.Disable();
            _subscribed = false;
        }

        if (suction)
        {
            suction.EndSuction();
            suction.suctionStrength = _baseStrength;
        }

        // 애니메 & 이동락 해제
        if (_binder) _binder.SetSuctionActive(false);
        if (_move) _move.SetInputLocked(false);
    }

    void Update()
    {
        if (!suction || absorbAction == null || absorbAction.action == null) return;

        // 아날로그 세기로 힘 스케일링
        if (scaleByAnalog && suction.suctionActive)
        {
            float v = Mathf.Abs(absorbAction.action.ReadValue<float>());  // Button=0/1, Trigger/Axis=0~1
            float t = Mathf.InverseLerp(analogDeadzone, 1f, v);
            float mult = Mathf.Lerp(analogMinMultiplier, analogMaxMultiplier, t);
            suction.suctionStrength = _baseStrength * mult;
        }
        else
        {
            suction.suctionStrength = _baseStrength;
        }
    }

    // === 콜백 ===
    void OnAbsorbStart(InputAction.CallbackContext ctx)
    {
        if (!suction) return;

        //  지면 체크 추가
        if (_move && !_move.IsGrounded)
        {
            // 공중이면 시작 불가
            return;
        }

        suction.BeginSuction();

        if (_binder)
        {
            float dir = _move ? Mathf.Sign(_move.LastDirection) : +1f;
            _binder.SetSuctionActive(true, dir);
        }

        if (_move) _move.SetInputLocked(true);
    }

    void OnAbsorbCancel(InputAction.CallbackContext ctx)
    {
        if (!suction) return;

        suction.EndSuction();
        suction.suctionStrength = _baseStrength;

        // 애니 Off + 이동락 해제
        if (_binder) _binder.SetSuctionActive(false);
        if (_move) _move.SetInputLocked(false); // ← 여기!
    }

    void OnAbsorbToggle(InputAction.CallbackContext ctx)
    {
        if (!suction) return;

        if (suction.suctionActive)
        {
            // 끄는 부분은 그대로 유지
            suction.EndSuction();
            suction.suctionStrength = _baseStrength;

            if (_binder) _binder.SetSuctionActive(false);
            if (_move) _move.SetInputLocked(false);
        }
        else
        {
            //  토글 ON 시에도 지면 체크 추가
            if (_move && !_move.IsGrounded)
            {
                return; // 공중에서는 흡수 시작 불가
            }

            suction.BeginSuction();

            if (_binder)
            {
                float dir = _move ? Mathf.Sign(_move.LastDirection) : +1f;
                _binder.SetSuctionActive(true, dir);
            }

            if (_move) _move.SetInputLocked(true);
        }
    }

    // 런타임에 액션 교체
    public void SetAbsorbAction(InputActionReference reference)
    {
        OnDisable();
        absorbAction = reference;
        OnEnable();
    }

    // 모드 교체(Hold/Toggle)
    public void SetMode(ActivationMode newMode)
    {
        if (mode == newMode) return;
        OnDisable();
        mode = newMode;
        OnEnable();
    }
}
