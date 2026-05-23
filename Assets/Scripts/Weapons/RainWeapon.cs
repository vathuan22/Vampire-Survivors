using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rain/Meteor weapon that spawns projectiles above enemies INSIDE the camera view.
/// </summary>
public class RainWeapon : Weapon
{
    [Header("Prefabs")]
    public RainProjectile projectileToSpawn;
    public EnemyDamager explosionDamager;

    [Header("Settings")]
    public float weaponRange = 15f;
    public LayerMask whatIsEnemy;
    public float spawnHeightAbove = 8f;     // Cao hơn để thấy meteor rơi rõ hơn

    private float shotCounter;
    private Collider2D[] localOverlapResults = new Collider2D[150];

    void Start()
    {
        SetStats();
    }

    void Update()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        if (statsUpdated)
        {
            statsUpdated = false;
            SetStats();
        }

        shotCounter -= Time.deltaTime;
        if (shotCounter <= 0)
        {
            shotCounter = stats[weaponLevel].timeBetweenAttacks;

            float searchRadius = weaponRange * stats[weaponLevel].range;
            int enemyCount = Physics2D.OverlapCircleNonAlloc(
                transform.position, searchRadius, localOverlapResults, whatIsEnemy);

            if (enemyCount == 0)
            {
                Debug.LogWarning("⚠️ No enemies found!");
                return;
            }

            if (projectileToSpawn == null)
            {
                Debug.LogWarning("⚠️ projectileToSpawn is NULL!");
                return;
            }

            // Lấy camera bounds để chỉ target enemy trong màn hình
            Camera cam = Camera.main;

            float spawnAmount = Mathf.Max(stats[weaponLevel].amount, 1);
            int spawned = 0;
            int maxAttempts = enemyCount * 2; // tránh infinite loop
            int attempts = 0;

            while (spawned < spawnAmount && attempts < maxAttempts)
            {
                attempts++;

                int randomIndex = Random.Range(0, enemyCount);
                Collider2D targetEnemy = localOverlapResults[randomIndex];
                if (targetEnemy == null) continue;

                Vector3 enemyPosition = targetEnemy.transform.position;

                // FIX: Chỉ spawn nếu enemy nằm trong camera
                if (cam != null)
                {
                    Vector3 viewportPos = cam.WorldToViewportPoint(enemyPosition);
                    bool inCamera = viewportPos.x > 0.05f && viewportPos.x < 0.95f
                                 && viewportPos.y > 0.05f && viewportPos.y < 0.95f;
                    if (!inCamera)
                    {
                        Debug.Log("⚠️ Enemy outside camera, skipping");
                        continue;
                    }
                }

                Vector3 spawnPosition = enemyPosition + Vector3.up * spawnHeightAbove;

                RainProjectile newProjectile = Instantiate(projectileToSpawn, spawnPosition, Quaternion.identity);
                newProjectile.gameObject.SetActive(true);

                newProjectile.damager           = explosionDamager;
                newProjectile.explosionRadius   = stats[weaponLevel].range;
                newProjectile.damage            = stats[weaponLevel].damage;
                newProjectile.whatIsEnemy       = whatIsEnemy;
                newProjectile.targetPosition    = enemyPosition;
                newProjectile.explosionDuration = stats[weaponLevel].duration;

                spawned++;
            }

            if (spawned > 0 && SFXManager.instance != null)
                SFXManager.instance.PlaySFXPitched(8);
        }
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        if (explosionDamager != null)
        {
            explosionDamager.damageAmount = stats[weaponLevel].damage;
            explosionDamager.lifeTime     = stats[weaponLevel].duration;
        }

        shotCounter = 0f;
    }
}