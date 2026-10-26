
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.2f;

    private float nextFireTime;

    private void Update()
    {
        RotateToMouse();
        Shoot();
    }

    private void RotateToMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePosition - transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 scale = transform.localScale;

        if (angle > 90f || angle < -90f)
            scale.y = -Mathf.Abs(scale.y);
        else
            scale.y = Mathf.Abs(scale.y);

        transform.localScale = scale;
    }

    private void Shoot()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePosition - firePoint.position).normalized;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle - 90f));
            GetComponent<GunVisual>().Kick();

            nextFireTime = Time.time + fireRate;
        }
    }
}
