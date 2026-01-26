using UnityEngine;

public class EnemySoundEffect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AudioClip EnemyDogBarkSfx;
    public AudioClip EnemyDogDieSfx;
    public AudioClip EnemyWarmDieSfx;
    public AudioClip EnemyWarmAttackSfx;
    public AudioClip EnemyGorillaDieSfx;
    public AudioClip EnemyGorillaAttackSfx;
    public AudioClip EnemyGorillaChestPoundingSfx;
    public AudioClip EnemyGorillaWalkSfx;
    public AudioClip EnemyGorillaBreathSfx;
    public AudioClip EnemyBossCoreHitSfx;
    public AudioClip EnemyBossCoreBrokSfx;
    public AudioClip EnemyBossIdleSfx;
    public AudioClip EnemyBossAppearSfx;
    public AudioClip EnemyBossAttackSfx;
    public AudioClip EnemyBossGassAttackSfx;
    public AudioClip EnemyBossAcidAttackSfx;
    public AudioClip EnemyBossSumonAttackSfx;
    public AudioClip EnemyBossDieSfx;
    public void PlayEnemyDogBark()
    {
        SoundManager.Instance.PlaySFX(EnemyDogBarkSfx);
    }

    public void PlayEnemyDogDie()
    {
        SoundManager.Instance.PlaySFX(EnemyDogDieSfx);
    }

    public void PlayEnemyWarmDie()
    {
        SoundManager.Instance.PlaySFX(EnemyWarmDieSfx);
    }
    public void PlayEnemyWarmAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyWarmAttackSfx);
    }
    public void PlayEnemyGorillaAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyGorillaAttackSfx);
    }
    public void PlayEnemyGorillaDie()
    {
        SoundManager.Instance.PlaySFX(EnemyGorillaDieSfx);
    }
    public void PlayEnemyGorillaChestPounding()
    {
        SoundManager.Instance.PlaySFX(EnemyGorillaChestPoundingSfx);
    }
    public void PlayEnemyGorillaWalk()
    {
        SoundManager.Instance.PlaySFX(EnemyGorillaWalkSfx);
    }
    public void PlayEnemyGorillaBreath()
    {
        SoundManager.Instance.PlaySFX(EnemyGorillaBreathSfx);
    }

    public void PlayEnemyBossIdle()
    {
        SoundManager.Instance.PlaySFX(EnemyBossIdleSfx);
    }
    public void PlayEnemyBossAppear()
    {
        SoundManager.Instance.PlaySFX(EnemyBossAppearSfx);
    }

    public void PlayEnemyBossAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyBossAttackSfx);
    }

    public void PlayEnemyBossGassAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyBossGassAttackSfx);
    }

    public void PlayEnemyBossAcidAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyBossAcidAttackSfx);
    }

    public void PlayEnemyBossSumonAttack()
    {
        SoundManager.Instance.PlaySFX(EnemyBossSumonAttackSfx);
    }

    public void PlayEnemyBossDie()
    {
        SoundManager.Instance.PlaySFX(EnemyBossDieSfx);
    }

    public void PlayEnemyBossCoreBrok()
    {
        SoundManager.Instance.PlaySFX(EnemyBossCoreBrokSfx);
    }

    public void PlayEnemyBossCoreHit()
    {
        SoundManager.Instance.PlaySFX(EnemyBossCoreHitSfx);
    }
}
