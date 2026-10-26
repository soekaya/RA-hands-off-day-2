using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;
    public float knockback = 4f;

    private static readonly Color SparkColor = new Color(1f, 0.80f, 0.30f, 1f);

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        // Menggerakkan objek ke arah atas lokal objek
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kena tembok kelas: peluru hilang dengan percikan kecil
        if (collision.GetComponent<ClassroomWall>() != null)
        {
            GameFX.Burst(transform.position, SparkColor, 5, 4f, 0.12f, 0.25f, -(Vector2)transform.up);
            Destroy(gameObject);
            return;
        }

        // Cek apakah objek yang ditabrak memiliki Tag "Enemy"
        if (collision.CompareTag("Enemy"))
        {
            Health health = collision.GetComponent<Health>();

            if (health != null)
            {
                // arah peluru dipakai untuk knockback dan efek percikan
                health.TakeDamage(damage, transform.up, knockback);
            }

            // Hancurkan projectile/peluru setelah mengenai musuh
            Destroy(gameObject);
        }
    }
}