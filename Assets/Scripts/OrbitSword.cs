using System.Collections.Generic;
using UnityEngine;

public class OrbitSword : MonoBehaviour
{
    public float spinSpeed = 220f;
    public int damage = 1;
    public float knockback = 5f;
    public float hitRadius = 0.7f;
    public float hitCooldown = 0.5f;
    public Vector2 centerOffset = new Vector2(0f, 0.2f);

    private Transform player;
    private Health playerHealth;
    private Transform blade;
    private float angle;
    private readonly Dictionary<Health, float> nextHitTime = new Dictionary<Health, float>();

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }

        blade = transform.Find("Blade");
    }

    // used by UpgradeManager to spread several swords around the player
    public void SetAngle(float degrees)
    {
        angle = degrees;
    }

    private void Update()
    {
        if (player == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        angle += spinSpeed * Time.deltaTime;
        transform.position = player.position + (Vector3)centerOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        HitEnemies();
    }

    private void HitEnemies()
    {
        Vector2 center = blade != null ? (Vector2)blade.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Health target = hits[i].GetComponentInParent<Health>();
            if (target == null || target.IsDead || !target.CompareTag("Enemy")) continue;

            float next;
            if (nextHitTime.TryGetValue(target, out next) && Time.time < next) continue;
            nextHitTime[target] = Time.time + hitCooldown;

            Vector2 away = (Vector2)target.transform.position - (Vector2)player.position;
            target.TakeDamage(damage, away, knockback);
        }

        CleanUp();
    }

    // removes enemies that are already destroyed from the list
    private void CleanUp()
    {
        if (nextHitTime.Count < 40) return;

        List<Health> removeList = new List<Health>();
        foreach (KeyValuePair<Health, float> pair in nextHitTime)
        {
            if (pair.Key == null) removeList.Add(pair.Key);
        }

        for (int i = 0; i < removeList.Count; i++)
        {
            nextHitTime.Remove(removeList[i]);
        }
    }
}
