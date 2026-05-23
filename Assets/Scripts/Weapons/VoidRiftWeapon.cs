using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VoidRift weapon that pulls enemies to a point then explodes.
/// Phase 1: Select nearest enemy as target point
/// Phase 2: Pull all enemies in range to target for 2 seconds
/// Phase 3: Explosion deals massive damage
/// </summary>
public class VoidRiftWeapon : Weapon
{
    public EnemyDamager explosionDamager;   // AoE damage zone template

    [Header("Pull Settings")]
    public float pullDuration = 2f;         // Duration of pull effect
    public float pullForce = 5f;            // Speed of pulling enemies

    [Header("Explosion Settings")]
    public float explosionRadiusMultiplier = 1.2f;
    public float explosionDamageMultiplier = 2.5f;

    public LayerMask whatIsEnemy;
    public float weaponRange = 15f;

    [Header("Animation")]
    public Sprite[] animationFrames;
    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    public float frameDuration = 0.1f;

    private float skillCounter;

    // State
    private bool isActive = false;
    private float pullTimer = 0f;
    private Vector3 targetPoint;
    private Vector3 playerStartPosition;

    // Lưu trạng thái physics gốc của từng enemy
    private struct EnemyPhysicsState
    {
        public EnemyController enemy;
        public float originalGravityScale;
        public RigidbodyConstraints2D originalConstraints;
        public bool colliderWasEnabled;  // ✅ Track collider state
    }
    private List<EnemyPhysicsState> pullingEnemies = new List<EnemyPhysicsState>();

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.enabled = false;
        SetStats();
    }

    void Update()
    {
        if (statsUpdated == true)
        {
            statsUpdated = false;
            SetStats();
        }

        // Sprite animation
        if (animationFrames != null && animationFrames.Length > 0 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
        }

        // Track player position khi idle
        if (!isActive)
        {
            playerStartPosition = transform.position;
        }

        // ── Active pull phase ───────────────────────────────────────────
        if (isActive)
        {
            transform.position = targetPoint;
            pullTimer -= Time.deltaTime;

            for (int i = pullingEnemies.Count - 1; i >= 0; i--)
            {
                EnemyPhysicsState state = pullingEnemies[i];
                if (state.enemy == null)
                {
                    pullingEnemies.RemoveAt(i);
                    continue;
                }

                Rigidbody2D rb = state.enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector3 dir = (targetPoint - state.enemy.transform.position).normalized;
                    rb.velocity = dir * pullForce;
                }
            }

            if (pullTimer <= 0f)
            {
                Explode();
            }
        }

        // ── Cooldown (chỉ chạy khi idle) ───────────────────────────────
        if (!isActive)
        {
            skillCounter -= Time.deltaTime;
            if (skillCounter <= 0f)
            {
                skillCounter = stats[weaponLevel].timeBetweenAttacks;
                TriggerSkill();
            }
        }
    }

    void TriggerSkill()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        float searchRadius = weaponRange * stats[weaponLevel].range;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, searchRadius, whatIsEnemy);
        

        if (enemies.Length == 0) return;
        
        // Tìm enemy gần nhất làm targetPoint
        Collider2D nearest = enemies[0];
        float nearestDist = Vector3.Distance(transform.position, nearest.transform.position);
        for (int i = 1; i < enemies.Length; i++)
        {
            float d = Vector3.Distance(transform.position, enemies[i].transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = enemies[i]; }
        }
        targetPoint = nearest.transform.position;

        // Thu thập enemies, lưu physics state gốc, disable AI movement
        pullingEnemies.Clear();
        foreach (Collider2D col in enemies)
        {
            EnemyController enemy = col.GetComponent<EnemyController>();
            if (enemy == null) continue;

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            // ✅ Get and disable colliders
            Collider2D collider = enemy.GetComponent<Collider2D>();
            bool colliderWasEnabled = collider != null && collider.enabled;
            if (collider != null)
                collider.enabled = false;  // ✅ Prevent damage during pull

            EnemyPhysicsState state = new EnemyPhysicsState
            {
                enemy                = enemy,
                originalGravityScale = rb.gravityScale,
                originalConstraints  = rb.constraints,
                colliderWasEnabled   = colliderWasEnabled  // ✅ Save state
            };
            pullingEnemies.Add(state);

            // Disable AI script để nó không override velocity mỗi frame
            enemy.enabled = false;

            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.None;
            rb.velocity      = Vector2.zero;
        }

        isActive  = true;
        pullTimer = pullDuration;

        transform.position     = targetPoint;
        transform.localScale = Vector3.one * stats[weaponLevel].range;
        spriteRenderer.enabled = true;

        

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    void Explode()
    {
        Debug.Log($"💥 VoidRift explosion at {targetPoint}!");

        if (explosionDamager != null)
        {
            GameObject zoneGO = Instantiate(explosionDamager.gameObject, targetPoint, Quaternion.identity);
            EnemyDamager zone = zoneGO.GetComponent<EnemyDamager>();

            float radius = stats[weaponLevel].range * explosionRadiusMultiplier;
            float damage = stats[weaponLevel].damage * explosionDamageMultiplier;

            zoneGO.transform.localScale = Vector3.one * radius;

            zone.damageAmount    = damage;
            zone.damageOverTime  = false;
            zone.lifeTime        = 0.5f;
            zone.shouldKnockBack = true;

            // radius = 0.5f vì object đã được scale bằng radius
            // world radius thực tế = scale(radius) * collider(0.5) = radius * 0.5
            // nếu muốn hit zone lớn hơn, tăng số này lên
            CircleCollider2D col = zoneGO.GetComponent<CircleCollider2D>();
            if (col == null) col = zoneGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.5f;

            Rigidbody2D rb = zoneGO.GetComponent<Rigidbody2D>();
            if (rb == null) rb = zoneGO.AddComponent<Rigidbody2D>();
            rb.bodyType    = RigidbodyType2D.Kinematic;
            rb.isKinematic = true;

            zoneGO.SetActive(true);
        }

        // Restore physics + re-enable AI cho tất cả enemy TRƯỚC khi clear list
        foreach (EnemyPhysicsState state in pullingEnemies)
        {
            if (state.enemy == null) continue;

            // ✅ Restore collider
            Collider2D collider = state.enemy.GetComponent<Collider2D>();
            if (collider != null && state.colliderWasEnabled)
                collider.enabled = true;

            // Re-enable AI script
            state.enemy.enabled = true;

            Rigidbody2D rb = state.enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = state.originalGravityScale;
                rb.constraints  = state.originalConstraints;
                rb.velocity      = Vector2.zero;
            }
        }

        pullingEnemies.Clear();

        spriteRenderer.enabled = false;
        transform.position     = playerStartPosition;
        transform.localScale = Vector3.one;

        isActive     = false;
        skillCounter = stats[weaponLevel].timeBetweenAttacks;

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter = stats[weaponLevel].timeBetweenAttacks;
        playerStartPosition = transform.position;

        Debug.Log($"✅ VoidRiftWeapon stats set! Damage={stats[weaponLevel].damage}, Range={stats[weaponLevel].range}");
    }
}