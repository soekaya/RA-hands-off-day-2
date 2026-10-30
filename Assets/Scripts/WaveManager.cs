using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private int totalWaves = 10;
    [SerializeField] private int maxWaveEvolution = 10;
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private int enemiesAddedPerWave = 2;
    [SerializeField] private int maxAliveAtOnce = 10;
    [SerializeField] private int healPerWave = 1;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float upgradeDelay = 2f;

    [Header("Sound")]
    [SerializeField] private AudioClip waveStartSfx;
    [SerializeField] private float waveStartVolume = 1f;
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private float winVolume = 1f;
    [SerializeField] private AudioClip waveClearSfx;
    [SerializeField] private float waveClearVolume = 1f;

    [SerializeField] private UnityEvent<int> waveStarted;
    [SerializeField] private UnityEvent<int> waveCompleted;
    [SerializeField] private UnityEvent allWavesCompleted;

    public int CurrentWave { get; private set; }
    public int TargetEnemyCount { get; private set; }
    public int SpawnedEnemyCount { get; private set; }
    public int EnemyKilledCount { get; private set; }
    public int TotalWaves => totalWaves;
    public bool IsFinished { get; private set; }

    public bool CanSpawn => SpawnedEnemyCount < TargetEnemyCount && SpawnedEnemyCount - EnemyKilledCount < maxAliveAtOnce;

    private Spawner[] spawners;
    private Health playerHealth;
    private AudioSource myAudioSource;

    private void Awake()
    {
        Instance = this;

        myAudioSource = GetComponent<AudioSource>();
        myAudioSource.playOnAwake = false;
        myAudioSource.spatialBlend = 0f;
        myAudioSource.volume = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        SetSpawners(false, 0f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }

        yield return new WaitForSeconds(initialDelay);

        while (CurrentWave < totalWaves)
        {
            CurrentWave++;
            SpawnedEnemyCount = 0;
            EnemyKilledCount = 0;
            TargetEnemyCount = baseEnemiesPerWave + (CurrentWave - 1) * enemiesAddedPerWave;

            float bias = Mathf.Clamp01(CurrentWave / (float)maxWaveEvolution);
            Debug.Log($"Wave {CurrentWave} starting! Target: {TargetEnemyCount}");

            waveStarted?.Invoke(CurrentWave);

            if (waveStartSfx != null && myAudioSource != null)
            {
                myAudioSource.PlayOneShot(waveStartSfx, waveStartVolume);
            }

            SetSpawners(true, bias);

            yield return new WaitUntil(() => SpawnedEnemyCount >= TargetEnemyCount || PlayerIsDead());

            SetSpawners(false, bias);

            yield return new WaitUntil(() => EnemyKilledCount >= SpawnedEnemyCount || PlayerIsDead());

            if (PlayerIsDead()) yield break;

            Debug.Log($"Wave {CurrentWave} completed! {EnemyKilledCount} enemies killed.");
            waveCompleted?.Invoke(CurrentWave);

            if (waveClearSfx != null && myAudioSource != null)
            {
                myAudioSource.PlayOneShot(waveClearSfx, waveClearVolume);
            }

            if (playerHealth != null)
            {
                playerHealth.Heal(healPerWave);
            }

            if (CurrentWave < totalWaves)
            {
                if (UpgradeManager.Instance != null)
                {
                    yield return new WaitForSeconds(upgradeDelay);
                    UpgradeManager.Instance.Open();
                    yield return new WaitUntil(() => !UpgradeManager.Instance.IsOpen || PlayerIsDead());
                }

                if (PlayerIsDead()) yield break;

                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        IsFinished = true;
        allWavesCompleted?.Invoke();

        if (winSfx != null && myAudioSource != null)
        {
            myAudioSource.PlayOneShot(winSfx, winVolume);
        }

        GameFX.CenterText("CLASS DISMISSED!", "You survived all " + totalWaves + " waves!");
    }

    public void RegisterSpawn() => SpawnedEnemyCount++;
    public void RegisterKill() => EnemyKilledCount++;

    private bool PlayerIsDead()
    {
        return playerHealth != null && playerHealth.IsDead;
    }

    private void SetSpawners(bool active, float bias)
    {
        foreach (Spawner spawner in spawners)
        {
            if (spawner == null) continue;
            spawner.EnemyBias = bias;
            spawner.enabled = active;
        }
    }
}
