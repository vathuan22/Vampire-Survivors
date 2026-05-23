using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using UnityEngine;

public class SpinWeapon : Weapon
{
    public float rotateSpeed;
    public Transform holder, fireballToSpawn;
    public float timeBetweenSpawn;
    private float spawnCounter;
    public EnemyDamager damager;
    
    public Transform fireballPrefab;  // ✅ Cache prefab để dùng lại

    void Start()
    {
        Debug.Log($"SpinWeapon Start: holder={holder}, fireballToSpawn={fireballToSpawn}");
        
        if (fireballToSpawn != null)
        {
            fireballPrefab = fireballToSpawn;  
            Debug.Log($"✅ SpinWeapon: fireballPrefab assigned from fireballToSpawn");
        }
        
        else
        {
            // ✅ Nếu vẫn không tìm được, tìm trong transform hiện tại
            if (transform.Find("Bullet") != null)
            {
                fireballPrefab = transform.Find("Bullet");
                Debug.LogWarning($"⚠️ SpinWeapon: auto-found fireballPrefab at transform.Find('Bullet')");
            }
            else
            {
                Debug.LogError($"❌ SpinWeapon: Không tìm được fireballPrefab! holder={holder}, childCount={holder?.childCount ?? -1}");
            }
        }
        SetStats();
    }

    private void OnEnable()
    {
        // ✅ Luôn re-setup khi enable, không rely vào old reference
        if (fireballToSpawn != null)
        {
            fireballPrefab = fireballToSpawn;
            Debug.Log($"✅ OnEnable: fireballPrefab re-cached from fireballToSpawn");
        }
        else if (holder != null && holder.childCount > 0)
        {
            fireballPrefab = holder.GetChild(0);
            Debug.Log($"⚠️ OnEnable: auto-found fireballPrefab from Holder");
        }
        else
        {
            Debug.LogError("❌ OnEnable: Không tìm được fireballPrefab!");
        }
        SetStats();
        spawnCounter = 0f;
    }
    private void ClearExistingBullets()
{
    if (holder == null) return;

    for (int i = holder.childCount - 1; i >= 0; i--)
    {
        Destroy(holder.GetChild(i).gameObject);
    }
}

    void Update()
    {
        Debug.Log("SpinWeapon Update running");

        if (holder == null)
        {
            Debug.LogError("❌ SpinWeapon: holder is NULL!");
            return;
        }

        if (fireballPrefab == null)  // ✅ Dùng fireballPrefab
        {
            Debug.LogError("❌ SpinWeapon: fireballPrefab is NULL!");
            return;
        }

        if (stats == null || stats.Count == 0)
        {
            Debug.LogError("❌ SpinWeapon: stats is NULL or empty!");
            return;
        }

        holder.rotation = Quaternion.Euler(
            0f, 0f, holder.rotation.eulerAngles.z + (rotateSpeed * Time.deltaTime * stats[weaponLevel].speed)
        );

        spawnCounter -= Time.deltaTime;
        if (spawnCounter <= 0)
        {
            spawnCounter = timeBetweenSpawn;

            float radius = stats[weaponLevel].range;

            for (int i = 0; i < stats[weaponLevel].amount; i++)
            {
                float rot = (360f / stats[weaponLevel].amount) * i;

                Transform bullet = Instantiate(fireballPrefab, holder);  // ✅ Dùng fireballPrefab
                Vector3 offset = Quaternion.Euler(0, 0, rot) * Vector3.up * radius;

                bullet.localPosition = offset;
                bullet.localRotation = Quaternion.identity;
                bullet.gameObject.SetActive(true);
            }

            if (SFXManager.instance != null)
                SFXManager.instance.PlaySFX(8);
        }

        if (statsUpdated)
        {
            SetStats();
            statsUpdated = false;
        }
    }

    public void SetStats()
    {
        if (stats == null || stats.Count == 0) return;
         ClearExistingBullets();
        damager.damageAmount = stats[weaponLevel].damage;
        timeBetweenSpawn = stats[weaponLevel].timeBetweenAttacks;
        damager.lifeTime = stats[weaponLevel].duration;
        spawnCounter = 0f;
    }
}