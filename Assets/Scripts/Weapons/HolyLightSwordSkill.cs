using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holy Light Sword Skill - Kiếm sáng cắm từ trên trời xuống enemy.
/// Dùng MoveTowards để kiếm dừng chính xác tại targetPos, không overshoot.
/// </summary>
public class HolyLightSwordSkill : Weapon
{
    [Header("Sword Settings")]
    public float targetRange = 15f;
    public float fallSpeed = 20f;
    public float swordHeight = 10f;
    public float explodeRadius = 4f;
    public float knockbackForce = 12f;
    public float delayBetweenSwords = 0.3f;

    [Header("Animation")]
    public Sprite[] fallingFrames;
    public Sprite[] explosionFrames;
    public float frameDuration = 0.08f;

    [Header("Damage")]
    public EnemyDamager swordDamager;
    public float damageRadiusMultiplier = 0.8f;

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
        if (swordDamager == null)
        {
            Debug.LogError("❌ HolyLightSword: swordDamager chưa assign!");
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

        // Thu thập enemy 1 lần, sort theo distance
        float searchRadius   = targetRange * stats[weaponLevel].range;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPos, searchRadius, whatIsEnemy);

        System.Array.Sort(enemies, (a, b) =>
            Vector3.Distance(playerPos, a.transform.position)
            .CompareTo(Vector3.Distance(playerPos, b.transform.position)));

        for (int i = 0; i < swordCount; i++)
        {
            Vector3 targetPos;

            if (enemies.Length == 0)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                targetPos   = playerPos + new Vector3(
                    Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 3f, 0f);
            }
            else
            {
                // Mỗi sword nhắm enemy khác nhau
                // ✅ Check null trước khi access position
                Collider2D selectedEnemy = enemies[i % enemies.Length];
                if (selectedEnemy != null && selectedEnemy.gameObject != null)
                {
                    targetPos = selectedEnemy.transform.position;
                }
                else
                {
                    // Enemy đã bị destroy → random position
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    targetPos = playerPos + new Vector3(
                        Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 3f, 0f);
                }
            }

            Debug.Log($"⚔️ HolySword {i + 1}/{swordCount} → {targetPos}");
            StartCoroutine(PerformSlam(targetPos));

            if (i < swordCount - 1)
                yield return new WaitForSeconds(delayBetweenSwords);
        }
    }

    IEnumerator PerformSlam(Vector3 targetPos)
    {
        // ✅ Safety check
        if (stats == null || weaponLevel >= stats.Count) yield break;

        float swordScale   = stats[weaponLevel].range;
        float explosionRad = explodeRadius * swordScale;

        // FIX: Clamp targetPos trong camera bounds để kiếm không bay ra ngoài
        Camera cam = Camera.main;
        if (cam != null)
        {
            float camH = cam.orthographicSize;
            float camW = camH * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // ✅ More aggressive clamping to keep swords visible
            targetPos.x = Mathf.Clamp(targetPos.x, camPos.x - camW * 0.4f, camPos.x + camW * 0.4f);
            targetPos.y = Mathf.Clamp(targetPos.y, camPos.y - camH * 0.3f, camPos.y + camH * 0.3f);
        }

        // Spawn phía trên targetPos theo trục Y
        Vector3 swordStartPos   = new Vector3(targetPos.x, targetPos.y + swordHeight, targetPos.z);
        Vector3 currentSwordPos = swordStartPos;
        float   fallTimer       = 0f;

        // Tạo sword visual
        GameObject swordGO = new GameObject("HolyLightSword");
        swordGO.transform.position   = swordStartPos;
        swordGO.transform.localScale = Vector3.one * swordScale;

        SpriteRenderer sr = swordGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        if (fallingFrames != null && fallingFrames.Length > 0)
            sr.sprite = fallingFrames[0];

        Rigidbody2D rb = swordGO.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.isKinematic = true;

        // ── Phase 1: Rơi xuống + gây damage ────────────────────
        // ✅ Damage trực tiếp khi kiếm bay xuống, không explosion
        while (Vector3.Distance(currentSwordPos, targetPos) > 0.05f)
        {
            // ✅ Re-check stats mỗi frame
            if (stats == null || weaponLevel >= stats.Count) break;
            
            // ✅ Check swordGO không bị destroy
            if (swordGO == null) yield break;

            fallTimer      += Time.deltaTime;
            currentSwordPos = Vector3.MoveTowards(
                currentSwordPos,
                targetPos,
                fallSpeed * Time.deltaTime
            );
            swordGO.transform.position = currentSwordPos;

            // Animate falling frames
            if (fallingFrames != null && fallingFrames.Length > 0 && sr != null)
            {
                int fi = Mathf.Clamp(
                    (int)(fallTimer / frameDuration), 0, fallingFrames.Length - 1);
                sr.sprite = fallingFrames[fi];
            }

            // ✅ Gây damage khi kiếm bay xuống
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                currentSwordPos, explosionRad * damageRadiusMultiplier, whatIsEnemy);
            
            foreach (Collider2D hit in hits)
            {
                // ✅ Defensive check: object có bị destroy không
                if (hit == null || hit.gameObject == null) continue;
                
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(stats[weaponLevel].damage, true);

                    // Knockback
                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        Vector2 dir = (hit.transform.position - currentSwordPos).normalized;
                        if (dir.magnitude < 0.1f) dir = Vector2.up;
                        enemyRb.velocity = dir * knockbackForce;
                    }

                    Debug.Log($"⚔️ HolyLight hit {enemy.name}!");
                }
            }

            yield return null;
        }

        // Cleanup
        if (swordGO != null)
            Destroy(swordGO);

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);

        // ✅ Decrease active sword count
        activeSwordCount--;
        if (activeSwordCount <= 0)
        {
            activeSwordCount = 0;
            isAttacking      = false;
        }

        Debug.Log($"✅ HolySword done. Remaining={activeSwordCount}");
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter    = firstActivation ? 0f : stats[weaponLevel].timeBetweenAttacks;
        firstActivation = false;
        playerStartPosition = transform.position;

        Debug.Log($"✅ HolyLightSword set! Dmg={stats[weaponLevel].damage}, " +
                  $"Range={stats[weaponLevel].range}, Amount={stats[weaponLevel].amount}");
    }
}