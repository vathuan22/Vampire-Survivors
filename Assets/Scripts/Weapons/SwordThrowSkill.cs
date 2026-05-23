using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sword Throw Skill - Ném kiếm đến enemy rồi quay về player.
/// Mỗi sword dùng HashSet riêng, dùng MoveTowards để di chuyển chính xác.
/// </summary>
public class SwordThrowSkill : Weapon
{
    [Header("Sword Settings")]
    public float throwRange = 15f;
    public float swordSpeed = 15f;
    public float swordSize = 1f;
    public float knockbackForce = 8f;
    public float delayBetweenSwords = 0.15f;

    [Header("Animation")]
    public Sprite[] animationFrames;
    public float frameDuration = 0.08f;

    [Header("Damage")]
    public EnemyDamager swordDamager;
    public bool damageOnReturn = false;
    public float damageRadius = 0.5f;

    public LayerMask whatIsEnemy;

    private float skillCounter;
    private bool firstActivation = true;
    private bool isAttacking = false;
    private int activeSwordCount = 0;
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

        if (stats == null || weaponLevel >= stats.Count) return;

        if (!isAttacking)
        {
            playerStartPosition = transform.position;

            skillCounter -= Time.deltaTime;
            if (skillCounter <= 0f)
            {
                skillCounter = stats[weaponLevel].timeBetweenAttacks;
                TriggerThrow();
            }
        }
    }

    void TriggerThrow()
    {
        if (stats == null || weaponLevel >= stats.Count) return;
        if (swordDamager == null)
        {
            Debug.LogError("❌ SwordThrow: swordDamager chưa assign!");
            return;
        }

        Vector3 playerPos = transform.parent != null
            ? transform.parent.position
            : transform.position;

        int swordCount = Mathf.Max(1, (int)stats[weaponLevel].amount);
        StartCoroutine(SpawnSwordsWithDelay(playerPos, swordCount));

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    IEnumerator SpawnSwordsWithDelay(Vector3 playerPos, int swordCount)
    {
        isAttacking      = true;
        activeSwordCount = swordCount;

        // Thu thập enemy 1 lần, lọc null, sort theo distance
        float searchRadius   = throwRange * stats[weaponLevel].range;
        Collider2D[] raw     = Physics2D.OverlapCircleAll(playerPos, searchRadius, whatIsEnemy);

        List<Collider2D> enemies = new List<Collider2D>();
        foreach (Collider2D e in raw)
            if (e != null && e.gameObject != null) enemies.Add(e);

        enemies.Sort((a, b) =>
            Vector3.Distance(playerPos, a.transform.position)
            .CompareTo(Vector3.Distance(playerPos, b.transform.position)));

        for (int i = 0; i < swordCount; i++)
        {
            Vector3 targetPos;

            if (enemies.Count == 0)
            {
                float spreadAngle = (360f / swordCount) * i * Mathf.Deg2Rad;
                targetPos = playerPos + new Vector3(
                    Mathf.Cos(spreadAngle), Mathf.Sin(spreadAngle), 0f) * throwRange;
            }
            else
            {
                // Mỗi sword nhắm enemy khác nhau
                int idx = i % enemies.Count;
                // null check lần 2 phòng enemy chết giữa delay
                if (enemies[idx] == null || enemies[idx].gameObject == null)
                    targetPos = playerPos + Vector3.right * throwRange;
                else
                    targetPos = enemies[idx].transform.position;
            }

            Debug.Log($"⚔️ SwordThrow {i + 1}/{swordCount} → target={targetPos}");
            StartCoroutine(PerformThrow(playerPos, targetPos));

            if (i < swordCount - 1)
                yield return new WaitForSeconds(delayBetweenSwords);
        }
    }

    IEnumerator PerformThrow(Vector3 playerPos, Vector3 targetPos)
    {
        // FIX: HashSet cục bộ cho từng sword
        HashSet<EnemyController> hitThisThrow = new HashSet<EnemyController>();

        float swordScale = stats[weaponLevel].range;
        float moveSpeed  = swordSpeed * Mathf.Max(stats[weaponLevel].speed, 1f);
        float hitRadius  = damageRadius * swordScale;

        // Tạo sword visual
        GameObject swordGO = new GameObject("SwordThrow");
        swordGO.transform.position   = playerPos;
        swordGO.transform.localScale = Vector3.one * swordSize * swordScale;

        SpriteRenderer sr = swordGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (animationFrames != null && animationFrames.Length > 0)
            sr.sprite = animationFrames[0];

        Rigidbody2D rb = swordGO.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.isKinematic = true;

        float animTimer = 0f;

        // ── Phase 1: Bay đến target ────────────────────────────────────
        // FIX: Dùng MoveTowards để dừng chính xác, không overshoot
        while (Vector3.Distance(swordGO.transform.position, targetPos) > 0.1f)
        {
            animTimer += Time.deltaTime;

            swordGO.transform.position = Vector3.MoveTowards(
                swordGO.transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            // Xoay theo hướng bay
            Vector2 flyDir = (targetPos - swordGO.transform.position).normalized;
            if (flyDir != Vector2.zero)
                swordGO.transform.rotation = Quaternion.AngleAxis(
                    Mathf.Atan2(flyDir.y, flyDir.x) * Mathf.Rad2Deg,
                    Vector3.forward);

            // Animate
            if (animationFrames != null && animationFrames.Length > 0)
            {
                int fi = Mathf.Clamp(
                    (int)(animTimer / frameDuration), 0, animationFrames.Length - 1);
                sr.sprite = animationFrames[fi];
            }

            // FIX: Damage dọc đường bay, mỗi enemy chỉ 1 lần
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                swordGO.transform.position, hitRadius, whatIsEnemy);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player")) continue;

                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null && !hitThisThrow.Contains(enemy))
                {
                    hitThisThrow.Add(enemy);
                    enemy.TakeDamage(stats[weaponLevel].damage, true);

                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 knockDir = (hit.transform.position - swordGO.transform.position).normalized;
                        enemyRb.velocity = knockDir * knockbackForce;
                    }

                    Debug.Log($"⚔️ Sword hit {enemy.name}!");
                }
            }

            yield return null;
        }

        // ── Phase 2: Quay về player ────────────────────────────────────
        // FIX: Lấy vị trí player hiện tại (player có thể đã di chuyển)
        // Dùng transform.position của weapon object (luôn bám theo player)
        animTimer = 0f;

        while (true)
        {
            animTimer += Time.deltaTime;

            // Lấy vị trí player hiện tại mỗi frame
            Vector3 currentPlayerPos = transform.position;

            swordGO.transform.position = Vector3.MoveTowards(
                swordGO.transform.position,
                currentPlayerPos,
                moveSpeed * Time.deltaTime
            );

            // Xoay theo hướng bay về
            Vector2 returnDir = (currentPlayerPos - swordGO.transform.position).normalized;
            if (returnDir != Vector2.zero)
                swordGO.transform.rotation = Quaternion.AngleAxis(
                    Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg,
                    Vector3.forward);

            // Animate
            if (animationFrames != null && animationFrames.Length > 0)
            {
                int fi = Mathf.Clamp(
                    (int)(animTimer / frameDuration), 0, animationFrames.Length - 1);
                sr.sprite = animationFrames[fi];
            }

            // Damage khi về (optional)
            if (damageOnReturn)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    swordGO.transform.position, hitRadius, whatIsEnemy);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Player")) continue;

                    EnemyController enemy = hit.GetComponent<EnemyController>();
                    if (enemy != null && !hitThisThrow.Contains(enemy))
                    {
                        hitThisThrow.Add(enemy);
                        enemy.TakeDamage(stats[weaponLevel].damage, true);

                        Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                        if (enemyRb != null)
                        {
                            Vector2 knockDir = (hit.transform.position - swordGO.transform.position).normalized;
                            enemyRb.velocity = knockDir * knockbackForce;
                        }
                    }
                }
            }

            // Đến nơi thì dừng
            if (Vector3.Distance(swordGO.transform.position, currentPlayerPos) < 0.15f)
                break;

            yield return null;
        }

        // Cleanup
        Destroy(swordGO);

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);

        activeSwordCount--;
        if (activeSwordCount <= 0)
        {
            activeSwordCount = 0;
            isAttacking      = false;
        }

        Debug.Log($"✅ SwordThrow done. Remaining={activeSwordCount}");
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        // FIX: fire ngay lần đầu, đợi cooldown khi level up
        skillCounter    = firstActivation ? 0f : stats[weaponLevel].timeBetweenAttacks;
        firstActivation = false;
        playerStartPosition = transform.position;

        Debug.Log($"✅ SwordThrow set! Dmg={stats[weaponLevel].damage}, " +
                  $"Range={stats[weaponLevel].range}, Amount={stats[weaponLevel].amount}");
    }
}