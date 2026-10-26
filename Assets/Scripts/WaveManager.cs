using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private int maxWaveEvolution = 10;
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private int enemiesAddedPerWave = 5;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float timeBetweenWaves = 5f;

    [SerializeField] private UnityEvent<int> waveStarted;
    [SerializeField] private UnityEvent<int> waveCompleted;

    public int CurrentWave { get; private set; }
    public int TargetEnemyCount { get; private set; }
    public int SpawnedEnemyCount { get; private set; }
    public int EnemyKilledCount { get; private set; }


    public bool CanSpawn => SpawnedEnemyCount < TargetEnemyCount;

    private Spawner[] spawners;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        SetSpawners(false, 0f);

        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            CurrentWave++;
            SpawnedEnemyCount = 0;
            EnemyKilledCount = 0;
            TargetEnemyCount = baseEnemiesPerWave + (CurrentWave - 1) * enemiesAddedPerWave;

            float bias = Mathf.Clamp01(CurrentWave / (float)maxWaveEvolution);

            Debug.Log($"Wave {CurrentWave} starting! Target: {TargetEnemyCount}");
            waveStarted?.Invoke(CurrentWave);
            SetSpawners(true, bias);

            yield return new WaitUntil(() => SpawnedEnemyCount >= TargetEnemyCount);

            SetSpawners(false, bias);

            yield return new WaitUntil(() => EnemyKilledCount >= SpawnedEnemyCount);
            Debug.Log($"Wave {CurrentWave} completed! {EnemyKilledCount} enemies killed.");
            waveCompleted?.Invoke(CurrentWave);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    public void RegisterSpawn() => SpawnedEnemyCount++;

    public void RegisterKill() => EnemyKilledCount++;

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