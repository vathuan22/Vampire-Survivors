using UnityEngine;

public class ProjectileWeapon : Weapon
{
    public EnemyDamager damager;
    public Projectile projectile;
    private float shotCounter;
    public float weaponRange;
    public LayerMask whatIsEnemy;
    public int sfxIndex = 4;  // Sound effect index (default: 4 - projectile launch)

    void Start()
    {
        SetStats();
    }

    void Update()
    {
        if (projectile == null)
        {
            Debug.LogWarning("ProjectileWeapon: projectile is NULL");
            return;
        }

        if (stats == null || stats.Count == 0)
        {
            Debug.LogWarning("ProjectileWeapon: stats is NULL or empty");
            return;
        }

        shotCounter -= Time.deltaTime;
        if (shotCounter > 0)
        {
            // Debug.Log($"Waiting: {shotCounter}");
            return;
        }

        shotCounter = stats[weaponLevel].timeBetweenAttacks;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            weaponRange * stats[weaponLevel].range,
            whatIsEnemy
        );

        

        if (enemies.Length <= 0) return;

        // ✅ Play sound effect when firing
        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(sfxIndex);

        for (int i = 0; i < stats[weaponLevel].amount; i++)
        {
            Transform target = enemies[Random.Range(0, enemies.Length)].transform;
            Vector3 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            Projectile newProjectile = Instantiate(
                projectile,
                transform.position,
                Quaternion.Euler(0f, 0f, angle)
            );

            if (newProjectile != null)
            {
                newProjectile.gameObject.SetActive(true);
                
            }
        }
    }

    void SetStats()
{
    if (stats == null || stats.Count == 0) return;

    if (weaponLevel < 0) weaponLevel = 0;
    if (weaponLevel >= stats.Count) weaponLevel = stats.Count - 1;

    if (damager != null)
    {
        damager.damageAmount = stats[weaponLevel].damage;
        damager.lifeTime = stats[weaponLevel].duration;
        damager.transform.localScale = Vector3.one;
    }

    if (projectile != null)
        projectile.moveSpeed = stats[weaponLevel].speed;

    shotCounter = 0f;
}
}