using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    // code events: the UI listens to these, so it works even if the Inspector events are empty
    public static event System.Action<int> WaveStartedEvent;
    public static event System.Action<int> WaveCompletedEvent;

    [SerializeField] private bool endless = true;
    [SerializeField] private int totalWaves = 10;   // only used when 'Endless' is off
    [SerializeField] private int maxWaveEvolution = 10;
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private int enemiesAddedPerWave = 2;
    [SerializeField] private int maxAliveAtOnce = 10;
    [SerializeField] private int maxAliveLimit = 25;       // the alive limit grows +1 every 2 waves, up to this
    [SerializeField] private float healthPerWave = 0.08f;  // enemies get +8% HP every wave
    [SerializeField] private int healPerWave = 1;

    [Header("Boss")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private int bossEveryNWaves = 5;
    [SerializeField] private Vector2 bossSpawnPoint = new Vector2(6f, 1f);
    [SerializeField] private int bossRewardCoins = 15;
    [Header("Timing")]
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float upgradeDelay = 2f;
    [SerializeField] private int upgradeEveryNWaves = 3;   // the shop opens after wave 3, 6, 9... (set 1 = every wave)

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
    public bool IsEndless => endless;
    public bool IsBossWave { get; private set; }
    public bool IsFinished { get; private set; }

    // best wave ever reached (saved on this computer)
    public int BestWave => PlayerPrefs.GetInt("BestWave", 0);
    public bool IsNewBest { get; private set; }

    // used by Health: enemies get more HP in later waves
    public float EnemyHealthMultiplier => 1f + healthPerWave * Mathf.Max(0, CurrentWave - 1);

    private int AliveLimit => Mathf.Min(maxAliveAtOnce + Mathf.Max(0, CurrentWave - 1) / 2, maxAliveLimit);

    // the Spawner asks this before spawning
    public bool CanSpawn => SpawnedEnemyCount < TargetEnemyCount
                            && SpawnedEnemyCount - EnemyKilledCount < AliveLimit;

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

        while (endless || CurrentWave < totalWaves)
        {
            CurrentWave++;
            SpawnedEnemyCount = 0;
            EnemyKilledCount = 0;
            TargetEnemyCount = baseEnemiesPerWave + (CurrentWave - 1) * enemiesAddedPerWave;

            // every Nth wave is a boss wave: the boss + half of the normal enemies
            IsBossWave = bossPrefab != null && bossEveryNWaves > 0 && CurrentWave % bossEveryNWaves == 0;
            if (IsBossWave)
            {
                TargetEnemyCount = 1 + TargetEnemyCount / 2;
            }

            float bias = Mathf.Clamp01(CurrentWave / (float)maxWaveEvolution);

            SaveBestWave();

            Debug.Log($"Wave {CurrentWave} starting! Target: {TargetEnemyCount}");

            waveStarted?.Invoke(CurrentWave);

            if (waveStartSfx != null && myAudioSource != null)
            {
                myAudioSource.PlayOneShot(waveStartSfx, waveStartVolume);
            }

            WaveStartedEvent?.Invoke(CurrentWave);
            SetSpawners(true, bias);

            if (IsBossWave)
            {
                SpawnBoss();
            }

            yield return new WaitUntil(() => SpawnedEnemyCount >= TargetEnemyCount || PlayerIsDead());

            SetSpawners(false, bias);

            yield return new WaitUntil(() => EnemyKilledCount >= SpawnedEnemyCount || PlayerIsDead());

            if (PlayerIsDead()) yield break;

            Debug.Log($"Wave {CurrentWave} completed! {EnemyKilledCount} enemies killed.");
            waveCompleted?.Invoke(CurrentWave);
            WaveCompletedEvent?.Invoke(CurrentWave);

            if (waveClearSfx != null && myAudioSource != null)
            {
                myAudioSource.PlayOneShot(waveClearSfx, waveClearVolume);
            }

            if (playerHealth != null)
            {
                playerHealth.Heal(healPerWave);
            }

            if (IsBossWave)
            {
                GiveBossReward();
            }

            if (endless || CurrentWave < totalWaves)
            {
                // the upgrade cards (the next wave waits until you chose or skipped)
                bool shopWave = upgradeEveryNWaves <= 1 || CurrentWave % upgradeEveryNWaves == 0;
                if (shopWave && UpgradeManager.Instance != null)
                {
                    yield return new WaitForSeconds(upgradeDelay);
                    UpgradeManager.Instance.Open();
                    yield return new WaitUntil(() => UpgradeManager.Instance == null || !UpgradeManager.Instance.IsOpen || PlayerIsDead());
                }
                else if (shopWave)
                {
                    Debug.LogWarning("WaveManager: UpgradeManager.Instance is null, so no upgrade cards. Is the UpgradeManager object in the scene and active?");
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

    private void SpawnBoss()
    {
        Vector3 position = new Vector3(bossSpawnPoint.x, bossSpawnPoint.y, 0f);

        // do not appear right next to the player: use the other side of the room
        if (playerHealth != null && Vector2.Distance(position, playerHealth.transform.position) < 4f)
        {
            position.x = -position.x;
        }

        Instantiate(bossPrefab, position, Quaternion.identity);
        RegisterSpawn();
    }

    private void GiveBossReward()
    {
        if (LevelManager.instance != null && bossRewardCoins > 0)
        {
            LevelManager.instance.AddCoin(bossRewardCoins);
        }

        if (playerHealth != null)
        {
            playerHealth.Heal(1);
            GameFX.Popup(playerHealth.transform.position + Vector3.up * 2.8f,
                         "BOSS DEFEATED!  +" + bossRewardCoins + " coins",
                         new Color(0.95f, 0.78f, 0.30f, 1f), 5f);
        }
    }

    private void SaveBestWave()
    {
        if (CurrentWave > BestWave)
        {
            PlayerPrefs.SetInt("BestWave", CurrentWave);
            PlayerPrefs.Save();
            IsNewBest = true;
        }
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
