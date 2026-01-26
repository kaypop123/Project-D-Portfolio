using UnityEngine;

public class PlayerSfx : MonoBehaviour
{
    public AudioClip WlakSfx;
    public AudioClip hitSfx;
    public AudioClip DieSfx;
    public AudioClip RollSfx;
    public AudioClip Attack1;
    public AudioClip AwakeningAttack1;
    public AudioClip JumpSfx;
    public AudioClip DashSkillSFX;
    public AudioClip SlashSkillSfx;
    public void PlayWalk()
    {
        SoundManager.Instance.PlaySFX(WlakSfx);
    }

    public void PlayHit()
    {
        SoundManager.Instance.PlaySFX(hitSfx);
    }

    public void PlayRoll1()
    {
        SoundManager.Instance.PlaySFX(RollSfx);
    }

    public void PlayDie()
    {
        SoundManager.Instance.PlaySFX(DieSfx);
    }

    public void PlayAttack1()
    {
        SoundManager.Instance.PlaySFX(Attack1);
    }
    public void PlayAwakeningAttack1()
    {
        SoundManager.Instance.PlaySFX(AwakeningAttack1);
    }
    public void PlayJump()
    {
        SoundManager.Instance.PlaySFX(JumpSfx);
    }

    public void PlayDashSkill()
    {
        SoundManager.Instance.PlaySFX(DashSkillSFX);
    }

    public void PlaySlashSkill()
    {
        SoundManager.Instance.PlaySFX(SlashSkillSfx);
    }
}
