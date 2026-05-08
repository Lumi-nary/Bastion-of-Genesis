using UnityEngine;

public static class GameplayAudio
{
    private const string LibraryResourcePath = "Data/Audio/GameplayAudioLibrary";
    private const float BaseUnderAttackCooldownSeconds = 8f;

    private static GameplayAudioLibrary cachedLibrary;
    private static float lastBaseUnderAttackTime = -BaseUnderAttackCooldownSeconds;

    public static GameplayAudioLibrary Library
    {
        get
        {
            if (cachedLibrary == null)
            {
                cachedLibrary = Resources.Load<GameplayAudioLibrary>(LibraryResourcePath);
            }

            return cachedLibrary;
        }
    }

    public static void PlayUIDeny(float volume = 1f) => Play2D(Library != null ? Library.uiDeny : null, volume);
    public static void PlayBuildPlacementPreview(float volume = 1f) => Play2D(Library != null ? Library.buildPlacementPreview : null, volume);
    public static void PlayBuildPlaced(Vector3 position, float volume = 1f) => PlayWorld(Library != null ? Library.buildPlaced : null, position, volume);
    public static void PlayBuildCancel(float volume = 1f) => Play2D(Library != null ? Library.buildCancel : null, volume);
    public static void PlayBuildDamaged(Vector3 position, float volume = 0.85f) => PlayWorld(Library != null ? Library.buildDamaged : null, position, volume);
    public static void PlayBuildDestroyed(Vector3 position, float volume = 1f) => PlayPriority(Library != null ? Library.buildDestroyed : null, position, volume);
    public static void PlayTurretFire(Vector3 position, float volume = 0.65f) => PlayWorld(Library != null ? Library.turretFire : null, position, volume);
    public static void PlayTurretHit(Vector3 position, float volume = 0.55f) => PlayWorld(Library != null ? Library.turretHit : null, position, volume);
    public static void PlayEnemyHit(Vector3 position, float volume = 0.45f) => PlayWorld(Library != null ? Library.enemyHit : null, position, volume);
    public static void PlayEnemyDeath(Vector3 position, float volume = 0.7f) => PlayWorld(Library != null ? Library.enemyDeath : null, position, volume);
    public static void PlayWaveIncoming(float volume = 1f) => Play2D(Library != null ? Library.waveIncoming : null, volume);
    public static void PlayPollutionSpread(Vector3 position, float volume = 0.8f) => PlayWorld(Library != null ? Library.pollutionSpread : null, position, volume);

    public static void PlayBaseUnderAttack(Vector3 position, float volume = 1f)
    {
        if (Time.time - lastBaseUnderAttackTime < BaseUnderAttackCooldownSeconds)
        {
            return;
        }

        lastBaseUnderAttackTime = Time.time;
        PlayPriority(Library != null ? Library.baseUnderAttack : null, position, volume);
    }

    private static void Play2D(AudioClip clip, float volume)
    {
        if (clip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySFX2D(clip, volume);
    }

    private static void PlayWorld(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(clip, position, volume);
    }

    private static void PlayPriority(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlayPrioritySFX(clip, position, volume);
    }
}
