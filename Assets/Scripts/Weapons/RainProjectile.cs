using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile that falls from sky and explodes on landing.
/// Instant damage (not over time) to keep balance.
/// </summary>
public class RainProjectile : Projectile
{
    public EnemyDamager damager;
    public float explosionRadius = 1.5f;
    public float fallSpeed = 8f;
    public float damage = 10f;
    public float explosionDuration = 0.4f;  // FIX: ngắn hơn, chỉ để visual
    public LayerMask whatIsEnemy;
    public float colliderRadiusMultiplier = 0.6f;

    [HideInInspector]
    public Vector3 targetPosition;

    private bool hasExploded = false;
    private float fallCounter;
    private GameObject aoeIndicator;

    // Static cache
    private static Sprite cachedCircleSprite;

    [Header("Animation")]
    public Sprite[] animationFrames;
    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    public float frameDuration = 0.1f;

    private Rigidbody2D rb;
    private Collider2D projectileCollider;

    void Start()
    {
        transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();

        if (targetPosition == Vector3.zero)
            targetPosition = transform.position + Vector3.down * 5f;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * fallSpeed;
        }

        fallCounter = 0f;
        frameTimer = 0f;
        currentFrameIndex = 0;

        CreateAoEIndicator();
    }

    void CreateAoEIndicator()
    {
        aoeIndicator = new GameObject("AoE_Indicator");
        aoeIndicator.transform.position = targetPosition;

        SpriteRenderer indicatorRenderer = aoeIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = GetOrCreateCircleSprite(explosionRadius);
        indicatorRenderer.color = new Color(1f, 0.3f, 0f, 0.35f);
        indicatorRenderer.sortingOrder = -1;
    }

    Sprite GetOrCreateCircleSprite(float radius)
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int textureSize = Mathf.CeilToInt(radius * 100f);
        Texture2D circleTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        float center = textureSize * 0.5f;
        float r = textureSize * 0.45f;

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > r - 4f && dist < r + 4f)
                    circleTex.SetPixel(x, y, new Color(1f, 0.3f, 0f, 0.8f));
                else if (dist <= r - 4f)
                    circleTex.SetPixel(x, y, new Color(1f, 0.3f, 0f, 0.15f));
                else
                    circleTex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        circleTex.Apply();
        cachedCircleSprite = Sprite.Create(
            circleTex,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    void Update()
    {
        if (!hasExploded && animationFrames != null && animationFrames.Length > 0 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
        }

        if (!hasExploded)
        {
            fallCounter += Time.deltaTime;

            if (transform.position.y <= targetPosition.y || fallCounter > 3f)
                Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (rb != null) rb.velocity = Vector2.zero;
        if (projectileCollider != null) projectileCollider.enabled = false;

        if (aoeIndicator != null) Destroy(aoeIndicator);

        if (damager != null)
        {
            EnemyDamager explosionZone = Instantiate(damager, targetPosition, Quaternion.identity);
            explosionZone.transform.localScale = Vector3.one * explosionRadius * 0.4f;

            // FIX: Instant damage, không over time
            // damage = 1 lần duy nhất khi chạm đất
            explosionZone.damageAmount    = damage;
            explosionZone.damageOverTime  = false;  // FIX: false thay vì true
            explosionZone.shouldKnockBack = true;
            explosionZone.lifeTime        = explosionDuration; // 0.4f - chỉ để visual

            CircleCollider2D cd = explosionZone.GetComponent<CircleCollider2D>();
            if (cd == null) cd = explosionZone.gameObject.AddComponent<CircleCollider2D>();
            cd.isTrigger = true;
            cd.radius    = explosionRadius * colliderRadiusMultiplier;

            explosionZone.gameObject.SetActive(true);

            CreateExplosionEffect(explosionZone);

            if (SFXManager.instance != null)
                SFXManager.instance.PlaySFXPitched(7);
        }

        Invoke(nameof(DestroyProjectile), 0.5f);
    }

    void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    void CreateExplosionEffect(EnemyDamager explosionZone)
    {
        GameObject explosionVFX = new GameObject("Explosion_Flash");
        explosionVFX.transform.position = explosionZone.transform.position;
        explosionVFX.transform.SetParent(explosionZone.transform);

        SpriteRenderer flashRenderer = explosionVFX.AddComponent<SpriteRenderer>();
        Sprite explosionSprite = explosionZone.GetComponent<SpriteRenderer>()?.sprite;
        if (explosionSprite == null && spriteRenderer != null)
            explosionSprite = spriteRenderer.sprite;

        flashRenderer.sprite = explosionSprite;
        flashRenderer.color = new Color(1f, 0.7f, 0.1f, 0.6f);
        flashRenderer.sortingOrder = 1;
        explosionVFX.transform.localScale = Vector3.one * 1.4f;

        StartCoroutine(FadeOutFlash(flashRenderer, 0.3f));
    }

    IEnumerator FadeOutFlash(SpriteRenderer renderer, float duration)
    {
        float elapsed = 0f;
        Color startColor = renderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (renderer == null) yield break;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (renderer != null && renderer.gameObject != null)
            Destroy(renderer.gameObject);
    }
}