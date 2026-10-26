using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform target;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnDistance = 3.5f; // never spawn right on top of the player
    [SerializeField] private int maxAlive = 12;             // 0 = no limit

    [Header("Keep Spawns Inside The Room")]
    [SerializeField] private bool limitToRoom = true;
    [SerializeField] private Vector2 roomCenter = Vector2.zero;
    [SerializeField] private Vector2 roomSize = new Vector2(24f, 12f); // floor size of the classroom
    [SerializeField] private float wallMargin = 1.2f;

    private float spawnTimer;
    private readonly List<GameObject> alive = new List<GameObject>();

    private void Update()
    {
        if (prefab == null || target == null)
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

        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        alive.Add(spawned);
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        float maxRadius = Mathf.Max(spawnRadius, minSpawnDistance + 0.5f);

        // 1) a point on a ring around the player
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

        // 2) the player is near a wall: pick any point in the room that is far enough from the player
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