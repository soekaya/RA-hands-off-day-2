using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;

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
        // Kena tembok kelas: peluru hilang
        if (collision.GetComponent<ClassroomWall>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // Cek apakah objek yang ditabrak memiliki Tag "Enemy"
        if (collision.CompareTag("Enemy"))
        {
            Health health = collision.GetComponent<Health>();

            if (health != null)
            {
                health.TakeDamage(damage);
            }

            // Hancurkan projectile/peluru setelah mengenai musuh
            Destroy(gameObject);
        }
    }
}