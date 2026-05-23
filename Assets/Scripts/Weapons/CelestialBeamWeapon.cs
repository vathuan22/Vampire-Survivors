using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CelestialBeam weapon - sweeps beam(s) từ trái sang phải tuần tự như mưa.
/// Beam nghiêng 45° từ trên trái xuống dưới phải.
/// Amount = số beam rơi tuần tự, cách nhau theo thời gian delay Between Beams.
/// </summary>
public class CelestialBeamWeapon : Weapon
{
    [Header("Damager")]
    public EnemyDamager beamDamagerPrefab;

    [Header("Animation (8 frames)")]
    public Sprite[] animationFrames;
    public float frameDuration = 0.1f;

    [Header("Beam Settings")]
    public float beamAngle      = 45f;   // dương = nghiêng / từ trái trên → phải dưới
    public float maxBeamScale  = 3f;    // độ dày lớn nhất (trục Y sau khi xoay)
    public float beamLength    = 20f;   // chiều dài beam (trục X sau khi xoay)
    public float beamDuration  = 3f;     // Thời gian tồn tại của MỖI beam độc lập
    public float delayBetweenBeams = 0.25f; // ⚡ KHOẢNG CÁCH THỜI GIAN GIỮA CÁC BEAM (Hiệu ứng mưa)
    public float screenHalfWidth = 15f; // nửa chiều ngang màn hình
    public float beamSpacing   = 4f;    // khoảng cách Y giữa các beam

    public LayerMask whatIsEnemy;

    private float skillCounter;
    private bool firstActivation = true;

    private bool isActive = false;
    private float skillTimeElapsed = 0f;   // ⚡ Đếm thời gian từ lúc kỹ năng bắt đầu
    private float totalSkillDuration = 0f; // ⚡ Tổng thời gian chạy skill (bao gồm cả delay của beam cuối)
    private Vector3 playerStartPosition;

    private class BeamInstance
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public int frameIndex;
        public float frameTimer;
        public Vector3 startPos;
        public float startTime;  // ⚡ Thời điểm (delay) beam này bắt đầu chạy
    }
    private List<BeamInstance> activeBeams = new List<BeamInstance>();

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        SetStats();
    }

    void Update()
    {
        if (statsUpdated)
        {
            statsUpdated = false;
            SetStats();
        }

        if (!isActive)
            playerStartPosition = transform.position;

        if (isActive)
        {
            skillTimeElapsed += Time.deltaTime; // ⚡ Đếm xuôi thời gian

            foreach (BeamInstance beam in activeBeams)
            {
                if (beam.gameObject == null) continue;

                // Tính toán "số tuổi" hiện tại của riêng beam này
                float beamAge = skillTimeElapsed - beam.startTime;

                // TRƯỜNG HỢP 1: Chưa đến giờ xuất hiện -> Ẩn đi
                if (beamAge < 0f)
                {
                    if (beam.gameObject.activeSelf) beam.gameObject.SetActive(false);
                    continue;
                }

                // TRƯỜNG HỢP 2: Đang trong thời gian hoạt động của beam
                if (beamAge <= beamDuration)
                {
                    if (!beam.gameObject.activeSelf) beam.gameObject.SetActive(true);

                    float progress    = beamAge / beamDuration;                   // 0 → 1 cho riêng beam này
                    float scaleFactor = Mathf.Sin(progress * Mathf.PI);           // 0 → 1 → 0
                    float thickness   = Mathf.Max(scaleFactor * maxBeamScale * stats[weaponLevel].range, 0.01f);

                    // Quét độc lập dựa theo progress riêng
                    float sweepX = Mathf.Lerp(
                        beam.startPos.x,
                        beam.startPos.x + screenHalfWidth * 2f,
                        progress
                    );
                    beam.gameObject.transform.position = new Vector3(sweepX, beam.startPos.y, beam.startPos.z);
                    beam.gameObject.transform.localScale = new Vector3(beamLength, thickness, 1f);

                    // Sprite animation độc lập
                    if (animationFrames != null && animationFrames.Length > 0)
                    {
                        beam.frameTimer += Time.deltaTime;
                        if (beam.frameTimer >= frameDuration)
                        {
                            beam.frameTimer -= frameDuration;
                            beam.frameIndex = (beam.frameIndex + 1) % animationFrames.Length;
                            beam.spriteRenderer.sprite = animationFrames[beam.frameIndex];
                        }
                    }
                }
                // TRƯỜNG HỢP 3: Beam này đã chạy xong -> Ẩn tạm thời chờ EndBeam giải phóng
                else
                {
                    if (beam.gameObject.activeSelf) beam.gameObject.SetActive(false);
                }
            }

            // Khi tổng thời gian vượt quá tia beam cuối cùng kết thúc -> Tắt skill
            if (skillTimeElapsed >= totalSkillDuration)
                EndBeam();
        }

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
        if (beamDamagerPrefab == null)
        {
            Debug.LogError("❌ CelestialBeam: chưa assign beamDamagerPrefab!");
            return;
        }

        float amount = Mathf.Max(stats[weaponLevel].amount, 1);
        activeBeams.Clear();

        float totalHeight = (amount - 1) * beamSpacing;
        float topY        = playerStartPosition.y + totalHeight * 0.5f;

        // ⚡ Tính tổng thời gian skill cần chạy: bằng thời gian delay của beam cuối + thời gian beam đó quét
        totalSkillDuration = ((amount - 1) * delayBetweenBeams) + beamDuration;
        skillTimeElapsed = 0f;

        for (int i = 0; i < amount; i++)
        {
            float beamY   = topY - i * beamSpacing;
            Vector3 start = new Vector3(
                playerStartPosition.x - screenHalfWidth,
                beamY,
                playerStartPosition.z
            );

            // ── Beam GameObject ──────────────────────────────────────
            GameObject beamGO = new GameObject($"CelestialBeam_{i}");
            beamGO.transform.position   = start;
            beamGO.transform.rotation   = Quaternion.Euler(0f, 0f, -beamAngle);
            beamGO.transform.localScale = new Vector3(beamLength, 0.01f, 1f);

            SpriteRenderer sr = beamGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            if (animationFrames != null && animationFrames.Length > 0)
                sr.sprite = animationFrames[0];

            // ── EnemyDamager child ───────────────────────────────────
            GameObject damagerGO = Instantiate(beamDamagerPrefab.gameObject);
            damagerGO.transform.SetParent(beamGO.transform, false);
            damagerGO.transform.localPosition = Vector3.zero;
            damagerGO.transform.localRotation = Quaternion.identity;
            damagerGO.transform.localScale    = Vector3.one;

            EnemyDamager damager        = damagerGO.GetComponent<EnemyDamager>();
            damager.damageAmount        = stats[weaponLevel].damage;
            damager.damageOverTime      = true;
            damager.lifeTime            = beamDuration + i * delayBetweenBeams + 1f; // Đảm bảo damager sống đủ lâu
            damager.shouldKnockBack     = false;

            CapsuleCollider2D cap = damagerGO.GetComponent<CapsuleCollider2D>();
            if (cap == null) cap  = damagerGO.AddComponent<CapsuleCollider2D>();
            cap.isTrigger         = true;
            cap.direction         = CapsuleDirection2D.Horizontal;
            cap.size              = new Vector2(1f, 0.5f);

            Rigidbody2D rb = damagerGO.GetComponent<Rigidbody2D>();
            if (rb == null) rb = damagerGO.AddComponent<Rigidbody2D>();
            rb.bodyType    = RigidbodyType2D.Kinematic;
            rb.isKinematic = true;

            // ⚡ KHỞI ĐẦU TẮT ACTIVE: Để tránh tất cả beam hiển thị đồng loạt ở điểm xuất phát ban đầu
            beamGO.SetActive(false);

            activeBeams.Add(new BeamInstance
            {
                gameObject     = beamGO,
                spriteRenderer = sr,
                frameIndex     = 0,
                frameTimer     = 0f,
                startPos       = start,
                startTime      = i * delayBetweenBeams // ⚡ Mỗi beam kế tiếp sẽ có thời gian bắt đầu trễ hơn
            });
        }

        isActive  = true;

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(8);
    }

    void EndBeam()
    {
        foreach (BeamInstance beam in activeBeams)
            if (beam.gameObject != null)
                Destroy(beam.gameObject);

        activeBeams.Clear();

        transform.position = playerStartPosition;
        isActive           = false;
        skillCounter       = stats[weaponLevel].timeBetweenAttacks;

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(7);
    }

    void SetStats()
    {
        if (stats == null || weaponLevel >= stats.Count) return;

        skillCounter    = firstActivation ? 0f : stats[weaponLevel].timeBetweenAttacks;
        firstActivation = false;
        playerStartPosition = transform.position;
    }

    private void OnDestroy()
    {
        // Khắc phục lỗi rò rỉ bộ nhớ nếu weapon bị hủy bất ngờ lúc skill đang chạy
        EndBeam();
    }
}