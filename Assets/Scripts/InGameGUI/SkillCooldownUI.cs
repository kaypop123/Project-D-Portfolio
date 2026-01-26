using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("스킬 GUI")]
    public Image cooldownMask;
    public TextMeshProUGUI cooldownText;
    public GameObject lockIcon;

    [Header("이펙트 설정")]
    public Animator fireAnimator;
    public Image flashOverlay;
    public float flashDuration = 0.5f;

    [Header("데이터")]
    private float cooldownDuration;
    private float lastUseTime = -999f;

    // 해금 관련 변수
    private CharacterTransform playerStatus;
    private int unlockCost = 0;
    private bool isUnlocked = false;

    public void Init(CharacterTransform _playerStatus, int _cost)
    {
        playerStatus = _playerStatus;
        unlockCost = _cost;

        bool conditionMet = playerStatus != null
                            && playerStatus.IsAwakened
                            && playerStatus.soul >= unlockCost;

        if (!conditionMet)
        {
            isUnlocked = false;
            SetLockState(true);
        }
        else
        {
            isUnlocked = true;
            SetLockState(false);
        }

        // 이펙트 초기화
        if (flashOverlay) flashOverlay.gameObject.SetActive(false);
        if (fireAnimator) fireAnimator.gameObject.SetActive(false);
    }

    public void TriggerCoolDown(float cooldown)
    {
        if (!isUnlocked) return;   

        cooldownDuration = cooldown;
        lastUseTime = Time.time;

        cooldownMask.gameObject.SetActive(true);
        cooldownMask.fillAmount = 1f;
        cooldownText.gameObject.SetActive(true);
    }

    void Update()
    {
        CheckUnlockStatus(); 

        if (!isUnlocked) return;

        float remain = GetRemainTime();

        if (remain <= 0f)
        {
            if (cooldownMask.gameObject.activeSelf)
            {
                cooldownMask.fillAmount = 0f;
                cooldownMask.gameObject.SetActive(false);
                cooldownText.gameObject.SetActive(false);

                // 쿨타임 끝났을 때 번쩍
                PlayFlashEffect();
            }
            return;
        }

        if (!cooldownMask.gameObject.activeSelf)
        {
            cooldownMask.gameObject.SetActive(true);
            cooldownText.gameObject.SetActive(true);
        }

        float ratio = remain / cooldownDuration;
        cooldownMask.fillAmount = ratio;
        cooldownText.text = Mathf.CeilToInt(remain).ToString();
    }

    void CheckUnlockStatus()
    {
        if (playerStatus == null) return;

        bool conditionMet = playerStatus.IsAwakened
                            && playerStatus.soul >= unlockCost;


        if (!isUnlocked && conditionMet)
        {
            isUnlocked = true;
            SetLockState(false);
            PlayFlashEffect();
            PlayFireAnimation();
        }

        else if (isUnlocked && !conditionMet)
        {
            isUnlocked = false;
            SetLockState(true);
        }
    }

    void SetLockState(bool locked)
    {
        if (locked)
        {
            if (lockIcon) lockIcon.SetActive(true);
            if (cooldownMask)
            {
                cooldownMask.gameObject.SetActive(true);
                cooldownMask.fillAmount = 1f; 
            }
            if (cooldownText) cooldownText.gameObject.SetActive(false);
        }
        else
        {
            if (lockIcon) lockIcon.SetActive(false);
            if (cooldownMask)
            {
                if (GetRemainTime() <= 0f)
                {
                    cooldownMask.gameObject.SetActive(false);
                    cooldownMask.fillAmount = 0f;
                }
            }
        }
    }

    float GetRemainTime()
    {
        return Mathf.Max(0f, (lastUseTime + cooldownDuration) - Time.time);
    }

    public void PlayFlashEffect()
    {
        if (flashOverlay != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }
    }

    public void PlayFireAnimation()
    {
        if (fireAnimator != null)
        {
            fireAnimator.gameObject.SetActive(true);
            fireAnimator.SetTrigger("Burn");
        }
    }

    IEnumerator FlashRoutine()
    {
        flashOverlay.gameObject.SetActive(true);
        float t = 0f;
        while (t < flashDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
            flashOverlay.color = new Color(1f, 1f, 1f, alpha);
            t += Time.deltaTime;
            yield return null;
        }
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        flashOverlay.gameObject.SetActive(false);
    }
}
