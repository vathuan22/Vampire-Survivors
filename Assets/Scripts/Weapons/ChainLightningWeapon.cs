using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chain Lightning weapon - spawns lightning from sky that chains between enemies
/// </summary>
public class ChainLightningWeapon : Weapon
{
    public GameObject lightningBoltPrefab;     // LightningBolt prefab với LineRenderer
    public EnemyDamager damager;               // Damage component
    
    private float shotCounter;
    public float weaponRange = 15f;            // Range to find enemies
    public LayerMask whatIsEnemy;
    
    public float spawnHeightAbove = 5f;        // Height above enemy
    public int maxChainCount = 3;              // Max enemies to chain to
    public float chainRange = 5f;              // Distance to chain (từ enemy này tới enemy khác)

    void Start()
    {
        // ✅ Clamp weaponLevel nếu vượt range
        if (stats != null && stats.Count > 0)
        {
            if (weaponLevel >= stats.Count)
                weaponLevel = stats.Count - 1;
            if (weaponLevel < 0)
                weaponLevel = 0;
        }
        SetStats();
    }

    void Update()
    {
        if (statsUpdated)
        {
            statsUpdated = false;
            SetStats();
        }

        // ✅ Check stats valid
        if (stats == null || stats.Count == 0) return;
        if (weaponLevel < 0 || weaponLevel >= stats.Count)
        {
            weaponLevel = Mathf.Clamp(weaponLevel, 0, stats.Count - 1);
            return;
        }

        shotCounter -= Time.deltaTime;
        if (shotCounter <= 0)
        {
            shotCounter = stats[weaponLevel].timeBetweenAttacks;
            
            // Find enemies in range
            Collider2D[] enemies = Physics2D.OverlapCircleAll(
                transform.position, 
                weaponRange * stats[weaponLevel].range, 
                whatIsEnemy
            );
            
            if (enemies.Length > 0)
            {
                // Spawn lightning bolts để chain
                for (int i = 0; i < (int)stats[weaponLevel].amount; i++)
                {
                    // Chọn enemy ngẫu nhiên làm điểm bắt đầu
                    Collider2D startEnemy = enemies[Random.Range(0, enemies.Length)];
                    
                    // Bắt đầu chain từ enemy này
                    SpawnChainLightning(startEnemy.transform, new List<Transform>());
                }
                
                if (SFXManager.instance != null)
                    SFXManager.instance.PlaySFXPitched(8);
            }
        }
    }

    void SpawnChainLightning(Transform targetEnemy, List<Transform> hitEnemies)
    {
        if (targetEnemy == null) return;
        
        Vector3 damagePos = targetEnemy.position;
        
        // ✅ Spawn AoE damager tại vị trí enemy
        if (damager != null)
        {
            GameObject damagerObj = Instantiate(damager.gameObject, damagePos, Quaternion.identity);
            damagerObj.SetActive(true);
            
            EnemyDamager dmg = damagerObj.GetComponent<EnemyDamager>();
            if (dmg != null)
            {
                dmg.damageAmount = stats[weaponLevel].damage;
                dmg.lifeTime = stats[weaponLevel].duration;
                dmg.transform.localScale = Vector3.one * stats[weaponLevel].range;
            }
        }
        
        // ✅ Spawn Lightning bolt với LineRenderer (vẽ từ trên trời xuống)
        if (lightningBoltPrefab != null)
        {
            Vector3 startPos = damagePos + Vector3.up * spawnHeightAbove;
            Vector3 endPos = damagePos;
            
            GameObject boltObj = Instantiate(lightningBoltPrefab, startPos, Quaternion.identity);
            boltObj.SetActive(true);
            
            LightningEffectRenderer lightning = boltObj.GetComponent<LightningEffectRenderer>();
            if (lightning != null)
            {
                lightning.DrawLightning(startPos, endPos, 5);  // 5 segments
            }
        }

        hitEnemies.Add(targetEnemy);
        
        // ✅ Chain tới enemy gần nhất
        if (hitEnemies.Count < maxChainCount)
        {
            Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
                targetEnemy.position, 
                chainRange * stats[weaponLevel].range, 
                whatIsEnemy
            );
            
            Transform nextEnemy = null;
            float closestDist = float.MaxValue;
            
            foreach (var col in nearbyEnemies)
            {
                if (hitEnemies.Contains(col.transform)) continue;
                
                float dist = Vector3.Distance(targetEnemy.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nextEnemy = col.transform;
                }
            }
            
            if (nextEnemy != null)
            {
                SpawnChainLightning(nextEnemy, hitEnemies);
            }
        }
    }

    void SetStats()
    {
        if (stats == null || stats.Count == 0) 
        {
            Debug.LogError("❌ ChainLightningWeapon: stats is NULL or empty!");
            return;
        }
        
        // ✅ Clamp weaponLevel
        if (weaponLevel < 0 || weaponLevel >= stats.Count)
        {
            weaponLevel = Mathf.Clamp(weaponLevel, 0, stats.Count - 1);
            Debug.LogWarning($"⚠️ ChainLightningWeapon: clamped weaponLevel to {weaponLevel}");
        }

        if (damager != null)
        {
            damager.damageAmount = stats[weaponLevel].damage;
            damager.lifeTime = stats[weaponLevel].duration;
        }

        shotCounter = 0f;
    }
}
