using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sword Beam Skill - Bắn beam về phía enemy gần nhất.
/// Mỗi beam dùng HashSet riêng, nhắm enemy khác nhau.
/// </summary>
public class SwordBeamSkill : Weapon
{
    [Header("Damager")]
    public EnemyDamager beamDamager;

    [Header("Beam Settings")]
    public float beamRange = 15f;
    public float beamWidth = 1.5f;
    public float beamHeight = 3f;
    public float beamDuration = 0.8f;
    public float beamSpeed = 20f;
    public float knockbackForce = 8f;
    public float delayBetweenBeams = 0.1f;

    [Header("Animation")]
    public Sprite[] animationFrames;
    public float frameDuration = 0.1f;

    public LayerMask whatIsEnemy;

    private float skillCounter;
    private bool firstActivation = true;
    private bool isAttacking = false;
    private int activeBeamCount = 0;
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

        if (stats == null || stats.Count == 0 || weaponLevel >= stats.Count) return;

        if (!isAttacking)
        {
            playerStartPosition = transform.position;

            skillCounter -= Time.deltaTime;
            if (skillCounter <= 0f)
            {
                skillCounter = stats[weaponLevel].timeBetweenAttacks;
                TriggerBeam();
            }
        }
    }

    void TriggerBeam()
    {
        if (stats == null || weaponLevel >= stats.Count) return;
        if (beamDamager == null)
        {
            Debug.LogError("❌ SwordBeam: beamDamager chưa assign!");
            return;
        }

        Vector3 playerPos = transform.parent != null
            ? transform.parent.position
            : transform.position;

        int beamCount = Mathf.Max(1, (int)stats[weaponLevel].amount);
        StartCoroutine(SpawnBeamsWithDelay(playerPos, beamCount));

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    IEnumerator SpawnBeamsWithDelay(Vector3 playerPos, int beamCount)
    {
        isAttacking     = true;
        activeBeamCount = beamCount;

        // FIX: Thu thập tất cả enemy 1 lần, mỗi beam nhắm enemy khác nhau
        float searchRadius   = beamRange * stats[weaponLevel].range;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPos, searchRadius, whatIsEnemy);

        // FIX: Lọc enemy null/destroyed trước khi sort
        List<Collider2D> validEnemies = new List<Collider2D>();
        foreach (Collider2D e in enemies)
            if (e != null && e.gameObject != null) validEnemies.Add(e);

        // Sort theo distance
        validEnemies.Sort((a, b) =>
            Vector3.Distance(playerPos, a.transform.position)
            .CompareTo(Vector3.Distance(playerPos, b.transform.position)));

        for (int i = 0; i < beamCount; i++)
        {
            Vector2 beamDirection;

            if (validEnemies.Count == 0)
            {
                // Không có enemy → spread đều 360°
                float spreadAngle = (360f / beamCount) * i;
                beamDirection = new Vector2(
                    Mathf.Cos(spreadAngle * Mathf.Deg2Rad),
                    Mathf.Sin(spreadAngle * Mathf.Deg2Rad)).normalized;
            }
            else
            {
                // Mỗi beam nhắm enemy khác nhau, check null lần nữa
                int enemyIndex     = i % validEnemies.Count;
                Collider2D target  = validEnemies[enemyIndex];
                if (target == null || target.gameObject == null)
                {
                    beamDirection = Vector2.right;
                }
                else
                {
                    beamDirection = (target.transform.position - playerPos).normalized;
                }
            }

            float angle          = Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg;
            Vector3 beamSpawnPos = playerPos + (Vector3)beamDirection * 1f;

            Debug.Log($"⚔️ SwordBeam {i + 1}/{beamCount} → dir={beamDirection}, angle={angle:F1}°");

            // FIX: Mỗi beam có HashSet riêng, không dùng chung
            StartCoroutine(PerformBeam(beamSpawnPos, beamDirection, angle));

            if (i < beamCount - 1)
                yield return new WaitForSeconds(delayBetweenBeams);
        }
    }

    IEnumerator PerformBeam(Vector3 beamStartPos, Vector2 beamDirection, float angle)
    {
        // FIX: HashSet cục bộ cho từng beam, không dùng chung
        HashSet<EnemyController> hitThisBeam = new HashSet<EnemyController>();

        float beamScale     = stats[weaponLevel].range;
        float maxDistance   = beamRange * beamScale;
        float beamDistance  = 0f;
        float animTimer     = 0f;
        float beamMoveSpeed = beamSpeed * Mathf.Max(stats[weaponLevel].speed, 1f);

        bool  flipX            = beamDirection.x < 0f;
        float normalizedAngle  = flipX ? (180f - angle) : angle;

        // Beam visual
        GameObject beamGO = new GameObject("BeamVisual");
        beamGO.transform.position   = beamStartPos;
        beamGO.transform.rotation   = Quaternion.AngleAxis(normalizedAngle, Vector3.forward);
        beamGO.transform.localScale = new Vector3(beamWidth * beamScale, beamHeight, 1f);

        SpriteRenderer sr = beamGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;
        sr.flipX        = flipX;
        if (animationFrames != null && animationFrames.Length > 0)
            sr.sprite = animationFrames[0];

        // EnemyDamager child của beam
        GameObject damagerGO = Instantiate(
            beamDamager.gameObject, beamStartPos,
            Quaternion.AngleAxis(normalizedAngle, Vector3.forward));
        damagerGO.transform.SetParent(beamGO.transform, false);
        damagerGO.transform.localPosition = Vector3.zero;
        damagerGO.transform.localScale    = Vector3.one;

        EnemyDamager damager = damagerGO.GetComponent<EnemyDamager>();
        damager.damageAmount    = stats[weaponLevel].damage;
        damager.damageOverTime  = false;
        damager.shouldKnockBack = true;
        damager.lifeTime        = beamDuration + 0.2f;

        BoxCollider2D box = damagerGO.GetComponent<BoxCollider2D>();
        if (box == null) box = damagerGO.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size      = new Vector2(1f, 1f);

        Rigidbody2D rb = damagerGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = damagerGO.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.isKinematic = true;

        damagerGO.SetActive(true);

        // ── Bay và damage dọc đường ────────────────────────────────────
        while (animTimer < beamDuration && beamDistance < maxDistance)
        {
            animTimer    += Time.deltaTime;
            beamDistance  = beamMoveSpeed * beamScale * animTimer;

            Vector3 currentPos = beamStartPos + (Vector3)beamDirection * beamDistance;
            beamGO.transform.position = currentPos;

            // Sprite animation
            if (animationFrames != null && animationFrames.Length > 0)
            {
                int fi = Mathf.Clamp(
                    (int)(animTimer / frameDuration), 0, animationFrames.Length - 1);
                sr.sprite = animationFrames[fi];
            }

            // Damage dọc đường, mỗi enemy chỉ 1 lần
            Vector2 boxSize = new Vector2(beamWidth * beamScale, beamHeight * beamScale);
            Collider2D[] hits = Physics2D.OverlapBoxAll(currentPos, boxSize, normalizedAngle, whatIsEnemy);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player")) continue;

                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null && !hitThisBeam.Contains(enemy))
                {
                    hitThisBeam.Add(enemy);
                    enemy.TakeDamage(stats[weaponLevel].damage, true);

                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                        enemyRb.velocity = beamDirection * knockbackForce;

                    Debug.Log($"⚔️ Beam hit {enemy.name}!");
                }
            }

            yield return null;
        }

        Destroy(beamGO);

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);

        activeBeamCount--;
        if (activeBeamCount <= 0)
        {
            activeBeamCount = 0;
            isAttacking     = false;
        }

        Debug.Log($"✅ SwordBeam done. Remaining={activeBeamCount}");
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter    = firstActivation ? 0f : stats[weaponLevel].timeBetweenAttacks;
        firstActivation = false;
        playerStartPosition = transform.position;

        Debug.Log($"✅ SwordBeam set! Dmg={stats[weaponLevel].damage}, Range={stats[weaponLevel].range}");
    }
}