using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Swordman Directional Slash Skill
/// Finds nearest enemy → slashes in that direction with AOE damage
/// Animation: 8 frames of slash effect
/// </summary>
public class SwordmanSlashSkill : Weapon
{
    [Header("Damager")]
    public EnemyDamager slashDamager;  // AOE damage zone template

    [Header("Slash Settings")]
    public float slashRange = 12f;           // Range to find nearest enemy
    public float slashRadius = 2.5f;         // Radius of slash AOE
    public float slashDuration = 0.64f;      // Duration (8 frames * 0.08s = 0.64s)
    public float slashDistance = 3f;         // Distance from player to spawn slash effect
    public float knockbackForce = 5f;        // Knockback strength
    public float delayBetweenSlashes = 0.15f; // Delay between multiple slashes

    [Header("Animation")]
    public Sprite[] animationFrames;        // 8 sprite frames
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    public float frameDuration = 0.08f;     // 0.08s per frame = 0.64s total

    public LayerMask whatIsEnemy;
    private float skillCounter;
    private bool isSlashing = false;
    private Vector3 playerStartPosition;

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

        if (!isSlashing)
            playerStartPosition = transform.position;

        skillCounter -= Time.deltaTime;
        if (skillCounter <= 0 && !isSlashing)
        {
            skillCounter = stats[weaponLevel].timeBetweenAttacks;
            TriggerSlash();
        }
    }

    /// <summary>
    /// Find nearest enemy and slash towards them
    /// Spawns multiple slashes with delay in random directions around player
    /// </summary>
    void TriggerSlash()
    {
        if (stats == null || weaponLevel >= stats.Count) return;
        if (slashDamager == null)
        {
            Debug.LogError("❌ SwordmanSlash: slashDamager not assigned!");
            return;
        }

        // Get number of slashes from amount stat (minimum 1)
        int slashCount = Mathf.Max(1, (int)stats[weaponLevel].amount);

        StartCoroutine(SpawnSlashesWithDelay(slashCount));

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    /// <summary>
    /// Spawn multiple slashes with stagger delay in random directions
    /// </summary>
    IEnumerator SpawnSlashesWithDelay(int slashCount)
    {
        for (int i = 0; i < slashCount; i++)
        {
            // Random direction around player (360°)
            float randomAngle = Random.Range(0f, 360f);
            Vector3 slashDir = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad), 0f).normalized;
            Vector3 slashPos = transform.position + slashDir * slashDistance;

            StartCoroutine(PerformSlash(slashPos, slashDir));

            // Delay before next slash
            if (i < slashCount - 1)
                yield return new WaitForSeconds(delayBetweenSlashes);
        }
    }

    /// <summary>
    /// Animate slash and deal damage
    /// </summary>
    IEnumerator PerformSlash(Vector3 slashPos, Vector3 slashDir)
    {
        isSlashing = true;
        currentFrameIndex = 0;
        frameTimer = 0f;

        // ✅ Create SEPARATE visual object for slash (not weapon transform)
        GameObject slashVisualGO = new GameObject("SlashVisual");
        slashVisualGO.transform.position = slashPos;
        
        SpriteRenderer slashVisualRenderer = slashVisualGO.AddComponent<SpriteRenderer>();
        slashVisualRenderer.sortingOrder = 5;
        if (animationFrames != null && animationFrames.Length > 0)
            slashVisualRenderer.sprite = animationFrames[0];

        // ✅ Create damage zone at slash position IMMEDIATELY (before animation)
        if (slashDamager != null)
        {
            GameObject slashZoneGO = Instantiate(slashDamager.gameObject, slashPos, Quaternion.identity);
            EnemyDamager slashZone = slashZoneGO.GetComponent<EnemyDamager>();

            // Configure damage zone
            slashZoneGO.transform.localScale = Vector3.one * slashRadius;
            slashZone.damageAmount = stats[weaponLevel].damage;
            slashZone.damageOverTime = false;  // Instant damage
            slashZone.shouldKnockBack = true;
            slashZone.lifeTime = 0.3f;  // Short visual duration

            // Setup collider
            CircleCollider2D collider = slashZoneGO.GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = slashZoneGO.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = slashRadius * 0.7f;

            // Setup rigidbody
            Rigidbody2D rb = slashZoneGO.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = slashZoneGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.isKinematic = true;

            slashZoneGO.SetActive(true);

            // Apply knockback
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(slashPos, slashRadius, whatIsEnemy);
            for (int i = 0; i < hitEnemies.Length; i++)
            {
                Rigidbody2D enemyRb = hitEnemies[i].GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 knockbackDir = (hitEnemies[i].transform.position - slashPos).normalized;
                    enemyRb.velocity = knockbackDir * knockbackForce;
                }
            }

            if (SFXManager.instance != null)
                SFXManager.instance.PlaySFXPitched(7);
        }

        // Animate slash visual (while damage zone is already active)
        float animTimer = 0f;
        while (animTimer < slashDuration)
        {
            animTimer += Time.deltaTime;
            
            // Update animation frame
            if (animationFrames != null && animationFrames.Length > 0)
            {
                int frameIndex = Mathf.Clamp((int)(animTimer / frameDuration), 0, animationFrames.Length - 1);
                slashVisualRenderer.sprite = animationFrames[frameIndex];
            }
            
            yield return null;
        }

        // ✅ Destroy slash visual (weapon position unaffected)
        Destroy(slashVisualGO);
        isSlashing = false;
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter = 0f;
        playerStartPosition = transform.position;

        Debug.Log($"✅ SwordmanSlash: Dmg={stats[weaponLevel].damage}, Range={stats[weaponLevel].range}");
    }
}
