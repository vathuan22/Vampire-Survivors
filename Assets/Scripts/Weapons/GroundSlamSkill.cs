using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ground Slam Skill - Vòng tròn kiếm khí phát ra từ player, lan rộng từ từ
/// - Vòng tròn bắt đầu tại player
/// - Phát triển radius dần dần
/// - Gây damage enemy chạm vào vòng tròn
/// - Có thể tạo multiple slams dựa trên amount stat
/// </summary>
public class GroundSlamSkill : Weapon
{
    [Header("Slam Settings")]
    public float maxSlamRadius = 8f;          // Max expansion radius
    public float expandSpeed = 15f;           // Speed of expansion
    public float slamDuration = 0.8f;         // Total duration of slam
    public float knockbackForce = 10f;        // Knockback strength

    [Header("Animation")]
    public Sprite[] animationFrames;          // Ring/circle animation frames
    public float frameDuration = 0.08f;       // Duration per frame

    [Header("Damage")]
    public EnemyDamager slamDamager;          // Damage component template
    public float damageTickInterval = 0.1f;   // Interval between damage ticks
    public float damageRadiusMultiplier = 0.7f;  // ✅ Scale down damage zone to match visual

    public LayerMask whatIsEnemy;

    private float skillCounter;
    private bool isAttacking = false;
    private Vector3 playerStartPosition;
    private int activeSlamCount = 0;

    private HashSet<EnemyController> hitEnemiesThisSlam = new HashSet<EnemyController>();

    void Start()
    {
        SetStats();
    }

    void Update()
    {
        if (statsUpdated)
        {
            statsUpdated = false;
            SetStats();
        }

        if (!isAttacking)
        {
            playerStartPosition = transform.position;

            skillCounter -= Time.deltaTime;
            if (skillCounter <= 0f)
            {
                skillCounter = stats[weaponLevel].timeBetweenAttacks;
                TriggerSlam();
            }
        }
    }

    void TriggerSlam()
    {
        if (stats == null || weaponLevel >= stats.Count) return;
        if (slamDamager == null)
        {
            
            return;
        }

        Vector3 playerPos = transform.parent != null
            ? transform.parent.position
            : transform.position;

        // ✅ Get slam count from amount stat
        int slamCount = Mathf.Max(1, (int)stats[weaponLevel].amount);

        StartCoroutine(SpawnSlamsWithDelay(playerPos, slamCount));

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    /// <summary>
    /// Spawn multiple slams with stagger delay
    /// </summary>
    IEnumerator SpawnSlamsWithDelay(Vector3 playerPos, int slamCount)
    {
        isAttacking = true;
        activeSlamCount = slamCount;
        float delayBetweenSlams = 0.2f;

        for (int i = 0; i < slamCount; i++)
        {
            // ✅ Each slam at player position (can offset if desired)
            Vector3 slamCenter = playerPos;
            
            

            StartCoroutine(PerformSlam(slamCenter));

            // Delay before next slam
            if (i < slamCount - 1)
                yield return new WaitForSeconds(delayBetweenSlams);
        }
    }

    IEnumerator PerformSlam(Vector3 slamCenter)
    {
        hitEnemiesThisSlam.Clear();

        float slamScale = stats[weaponLevel].range;
        float maxRadius = maxSlamRadius * slamScale;
        float expandRate = expandSpeed * slamScale;
        
        float currentRadius = 0.1f;  // Start with small radius
        float slamTimer = 0f;
        int frameIndex = 0;

        // Create slam visual GameObject
        GameObject slamGO = new GameObject("GroundSlam");
        slamGO.transform.position = slamCenter;

        SpriteRenderer sr = slamGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        if (animationFrames != null && animationFrames.Length > 0)
            sr.sprite = animationFrames[0];
        
        slamGO.transform.localScale = Vector3.one * 0.1f;

        // Create collider for damage detection
        CircleCollider2D collider = slamGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 1f;  // Normalized to localScale

        Rigidbody2D rb = slamGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.isKinematic = true;

        // Track enemies that have taken damage this frame
        HashSet<EnemyController> damagedThisFrame = new HashSet<EnemyController>();

        // ─── SLAM EXPANSION ────────────
        while (slamTimer < slamDuration && currentRadius < maxRadius)
        {
            slamTimer += Time.deltaTime;

            // ✅ Expand radius over time
            currentRadius = Mathf.Lerp(0.1f, maxRadius, slamTimer / slamDuration);
            slamGO.transform.localScale = Vector3.one * currentRadius;

            // Animate sprite
            if (animationFrames != null && animationFrames.Length > 0)
            {
                frameIndex = Mathf.Clamp(
                    (int)(slamTimer / frameDuration),
                    0, animationFrames.Length - 1);
                sr.sprite = animationFrames[frameIndex];
            }

            // ✅ Damage enemies in expanding circle (scaled down by multiplier)
            Collider2D[] hits = Physics2D.OverlapCircleAll(slamCenter, currentRadius * damageRadiusMultiplier, whatIsEnemy);
            damagedThisFrame.Clear();

            foreach (Collider2D hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                
                // Damage only if not hit yet in this slam
                if (enemy != null && !hitEnemiesThisSlam.Contains(enemy) && !damagedThisFrame.Contains(enemy))
                {
                    hitEnemiesThisSlam.Add(enemy);
                    damagedThisFrame.Add(enemy);
                    
                    enemy.TakeDamage(stats[weaponLevel].damage, true);

                    // Knockback away from slam center
                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - slamCenter).normalized;
                        enemyRb.velocity = knockbackDir * knockbackForce;
                    }

                    Debug.Log($"💥 Slam hit {enemy.name}!");
                }
            }

            yield return null;
        }

        // Finish expanding - hold at max size briefly
        float holdTimer = 0.2f;
        while (holdTimer > 0)
        {
            holdTimer -= Time.deltaTime;
            
            // Continue checking for enemies during hold
            Collider2D[] hitsHold = Physics2D.OverlapCircleAll(slamCenter, currentRadius * damageRadiusMultiplier, whatIsEnemy);
            damagedThisFrame.Clear();

            foreach (Collider2D hit in hitsHold)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                
                if (enemy != null && !hitEnemiesThisSlam.Contains(enemy) && !damagedThisFrame.Contains(enemy))
                {
                    hitEnemiesThisSlam.Add(enemy);
                    damagedThisFrame.Add(enemy);
                    
                    enemy.TakeDamage(stats[weaponLevel].damage, true);

                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - slamCenter).normalized;
                        enemyRb.velocity = knockbackDir * knockbackForce;
                    }

                    Debug.Log($"💥 Slam hold hit {enemy.name}!");
                }
            }

            yield return null;
        }

        // Cleanup
        Destroy(slamGO);

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);

        // ✅ Decrease active slam count
        activeSlamCount--;
        if (activeSlamCount <= 0)
        {
            isAttacking = false;
            hitEnemiesThisSlam.Clear();
        }

        Debug.Log($"✅ GroundSlam finished. Active slams: {activeSlamCount}");
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter = 0f;
        playerStartPosition = transform.position;

        Debug.Log($"✅ GroundSlam set! Dmg={stats[weaponLevel].damage}, Range={stats[weaponLevel].range}, Amount={stats[weaponLevel].amount}");
    }
}
