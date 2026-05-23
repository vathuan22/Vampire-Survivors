using System.Collections.Generic;
using UnityEngine;

public class EnemyDamager : MonoBehaviour
{
    public float damageAmount;
    public float lifeTime, growSpeed = 0f;
    public bool shouldKnockBack;
    public bool destroyParent;
    public bool damageOverTime;
    public float timeBetweenDamage;
    private float damageCounter;
    private List<EnemyController> enemiesInRange = new List<EnemyController>();
    public bool destroyOnImpact;

    void Start()
    {
        if (lifeTime > 0)
            Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (damageOverTime)
        {
            damageCounter -= Time.deltaTime;
            if (damageCounter <= 0)
            {
                damageCounter = timeBetweenDamage;
                for (int i = 0; i < enemiesInRange.Count; i++)
                {
                    if (enemiesInRange[i] != null)
                    {
                        enemiesInRange[i].TakeDamage(damageAmount, shouldKnockBack);
                    }
                    else
                    {
                        enemiesInRange.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"🔵 OnTriggerEnter2D: {collision.name}, tag={collision.tag}, hasEnemyTag={collision.CompareTag("Enemy")}");
        
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log($"✅ Hit Enemy: {collision.name}");
            
            if (!damageOverTime)
            {
                EnemyController enemy = collision.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damageAmount, shouldKnockBack);
                }

                if (destroyOnImpact)
                    Destroy(gameObject);
            }
            else
            {
                EnemyController enemy = collision.GetComponent<EnemyController>();
                if (enemy != null && !enemiesInRange.Contains(enemy))
                {
                    enemiesInRange.Add(enemy);
                    
                }
                else if (enemy == null)
                {
                    Debug.LogWarning($"⚠️ No EnemyController on {collision.name}");
                }
            }
        }
        else if (collision.CompareTag("Breakable")) 
        {
            BreakableBox box = collision.GetComponent<BreakableBox>();
            if (box != null)
            {
                box.TakeDamage(damageAmount);
                
                // Vũ khí có thuộc tính nổ/hủy khi chạm thì tự hủy luôn
                if (destroyOnImpact && !damageOverTime) 
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (damageOverTime && collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            if (enemy != null)
                enemiesInRange.Remove(enemy);
        }
    }
}