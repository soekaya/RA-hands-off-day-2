using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform target;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnInterval = 2f;

    [Header("Keep Spawns Inside The Room")]
    [SerializeField] private bool limitToRoom = true;
    [SerializeField] private Vector2 roomCenter = Vector2.zero;
    [SerializeField] private Vector2 roomSize = new Vector2(24f, 12f); // floor size of the classroom
    [SerializeField] private float wallMargin = 1.2f;

    private float spawnTimer;

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
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition = new Vector3(
            target.position.x + randomOffset.x,
            target.position.y + randomOffset.y,
            target.position.z
        );

        if (limitToRoom)
        {
            spawnPosition.x = Mathf.Clamp(
                spawnPosition.x,
                roomCenter.x - roomSize.x / 2f + wallMargin,
                roomCenter.x + roomSize.x / 2f - wallMargin);

            spawnPosition.y = Mathf.Clamp(
                spawnPosition.y,
                roomCenter.y - roomSize.y / 2f + wallMargin,
                roomCenter.y + roomSize.y / 2f - wallMargin);
        }

        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}