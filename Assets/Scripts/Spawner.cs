using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Transform target;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnDistance = 3.5f; // never spawn right on top of the player
    [SerializeField] private int maxAlive = 12;             // 0 = no limit

    [Header("Enemy Selection (Normal Distribution)")]
    [SerializeField, Range(0f, 1f)] public float EnemyBias = 0f;
    [SerializeField, Range(0f, 1f)] private float stdDev = 0.15f;
    [SerializeField] private bool limitToOneStdDev = false;

    [Header("Keep Spawns Inside The Room")]
    [SerializeField] private bool limitToRoom = true;
    [SerializeField] private Vector2 roomCenter = Vector2.zero;
    [SerializeField] private Vector2 roomSize = new Vector2(24f, 12f); // floor size of the classroom
    [SerializeField] private float wallMargin = 1.2f;

    private float spawnTimer;
    private readonly List<GameObject> alive = new List<GameObject>();

    private void Update()
    {
        if (enemies == null || enemies.Length == 0 || target == null)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnPrefab();
            spawnTimer = 0f;
        }
    }

    private void SpawnPrefab()
    {
        alive.RemoveAll(x => x == null);
        if (maxAlive > 0 && alive.Count >= maxAlive)
        {
            return;
        }

        Vector3 position;
        if (!TryGetSpawnPosition(out position))
        {
            return;
        }

        GameObject spawned = Instantiate(enemies[PickEnemyIndex()], position, Quaternion.identity);
        alive.Add(spawned);
        WaveManager.Instance.RegisterSpawn();
    }

    private int PickEnemyIndex()
    {
        int lastIndex = enemies.Length - 1;
        if (lastIndex == 0)
        {
            return 0;
        }

        float mean = EnemyBias * lastIndex;
        float sigma = stdDev * lastIndex;

        float value = mean;
        for (int i = 0; i < 10; i++)
        {
            float candidate = mean + RandomGaussian() * sigma;

            bool inArray = candidate > -0.5f && candidate < lastIndex + 0.5f;
            bool inStdDev = !limitToOneStdDev || Mathf.Abs(candidate - mean) <= sigma;

            if (inArray && inStdDev)
            {
                value = candidate;
                break;
            }
        }

        return Mathf.Clamp(Mathf.RoundToInt(value), 0, lastIndex);
    }


    private static float RandomGaussian()
    {
        float u1 = 1f - Random.value;
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        float maxRadius = Mathf.Max(spawnRadius, minSpawnDistance + 0.5f);

        for (int i = 0; i < 12; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minSpawnDistance, maxRadius);

            Vector3 p = new Vector3(
                target.position.x + Mathf.Cos(angle) * radius,
                target.position.y + Mathf.Sin(angle) * radius,
                target.position.z
            );

            if (!limitToRoom || IsInsideRoom(p))
            {
                position = p;
                return true;
            }
        }

        if (limitToRoom)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = new Vector3(
                    Random.Range(roomCenter.x - roomSize.x / 2f + wallMargin, roomCenter.x + roomSize.x / 2f - wallMargin),
                    Random.Range(roomCenter.y - roomSize.y / 2f + wallMargin, roomCenter.y + roomSize.y / 2f - wallMargin),
                    target.position.z
                );

                if (Vector2.Distance(p, target.position) >= minSpawnDistance)
                {
                    position = p;
                    return true;
                }
            }
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsInsideRoom(Vector3 p)
    {
        return p.x >= roomCenter.x - roomSize.x / 2f + wallMargin
            && p.x <= roomCenter.x + roomSize.x / 2f - wallMargin
            && p.y >= roomCenter.y - roomSize.y / 2f + wallMargin
            && p.y <= roomCenter.y + roomSize.y / 2f - wallMargin;
    }
}