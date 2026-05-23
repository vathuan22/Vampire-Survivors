using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ZoneWeapon : Weapon
{
    public EnemyDamager damager; // The enemy damager. GK
    private float spawnTime, spawnCounter; // The time between spawning and the counter for the spawn time. GK
    
    [Header("Animation")]
    public Sprite[] animationFrames; // 8 sprite frames for animation
    private SpriteRenderer spriteRenderer; // SpriteRenderer for animation
    private int currentFrameIndex = 0; // Current animation frame
    private float frameTimer = 0f; // Timer for sprite animation
    private float frameDuration = 0.2f; // Duration per frame (0.2s)
    
    [Header("Fade In")]
    private float fadeInDuration = 0.3f; // Fade in time
    private float fadeInTimer = 0f; // Timer for fade in
    private bool isFadingIn = true; // Is fading in?
    
    [Header("Damage Settings")]
    public float damageRadiusMultiplier = 0.5f; // Giảm damage range so với visual (0-1)
    
    // ✅ Track active damager instances
    private List<GameObject> activeDamagers = new List<GameObject>();

    void Start()
    {
        SetStats();
        
        // ✅ Setup sprite animation
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        
        // ✅ Start fade in
        isFadingIn = true;
        fadeInTimer = 0f;
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
        
        Debug.Log($"✅ ZoneWeapon initialized with {animationFrames.Length} frames");
    }

    void Update()
    {
        if (statsUpdated == true) // If the stats are updated. GK
        {
            statsUpdated = false; // Set the stats updated to false. GK
            SetStats(); // Set the stats. GK
        }
        
        // ✅ Fade in animation
        if (isFadingIn)
        {
            fadeInTimer += Time.deltaTime;
            if (fadeInTimer >= fadeInDuration)
            {
                isFadingIn = false;
                fadeInTimer = fadeInDuration;
            }
            
            float alpha = Mathf.Lerp(0f, 1f, fadeInTimer / fadeInDuration);
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        
        // ✅ Sprite animation (8 frames, 0.2s per frame)
        if (animationFrames.Length > 0 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
        }
        
        spawnCounter -= Time.deltaTime; // Decrease the spawn counter. GK
        if (spawnCounter <= 0) // If the spawn counter is less than or equal to 0. GK
        {
            spawnCounter = spawnTime; // Reset the spawn counter. GK
            
            // ✅ Check if damager is null before instantiating
            if (damager != null)
            {
                GameObject damagerInstance = Instantiate(damager.gameObject, transform.position, Quaternion.identity);
                damagerInstance.SetActive(true);
                
                // ✅ Track this damager instance
                activeDamagers.Add(damagerInstance);
                
                // ✅ Get EnemyDamager component and set stats directly on instance
                EnemyDamager damagerComponent = damagerInstance.GetComponent<EnemyDamager>();
                if (damagerComponent != null)
                {
                    damagerComponent.damageAmount = stats[weaponLevel].damage;
                    // ✅ Animation duration = frameDuration * number of frames
                    float animationDuration = frameDuration * animationFrames.Length;
                    damagerComponent.lifeTime = Mathf.Max(animationDuration, stats[weaponLevel].duration);
                    damagerComponent.timeBetweenDamage = 0.1f;  // Damage every 0.1s during animation
                    damagerComponent.damageOverTime = true;
                    damagerComponent.shouldKnockBack = true;
                    
                    Debug.Log($"✅ Damager setup: lifeTime={damagerComponent.lifeTime}, animDuration={animationDuration}, damagePerTick={damagerComponent.damageAmount}");
                }
                
                // ✅ Ensure CircleCollider2D exists and is trigger
                CircleCollider2D collider = damagerInstance.GetComponent<CircleCollider2D>();
                if (collider == null)
                {
                    collider = damagerInstance.AddComponent<CircleCollider2D>();
                }
                collider.isTrigger = true; // Make sure it's a trigger
                collider.radius = stats[weaponLevel].range * damageRadiusMultiplier; // Apply damage radius multiplier
                
                // ✅ Ensure Rigidbody2D exists for trigger detection
                Rigidbody2D rb = damagerInstance.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = damagerInstance.AddComponent<Rigidbody2D>();
                }
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.isKinematic = true;
                
                // ✅ Scale visual
                damagerInstance.transform.localScale = Vector3.one * stats[weaponLevel].range;
                
                Debug.Log($"✅ Spawned damager, visual_scale={stats[weaponLevel].range}, damage_radius={collider.radius}");
                
                if (SFXManager.instance != null)
                    SFXManager.instance.PlaySFXPitched(10); // Play the sound effect. GK
            }
            else
            {
                Debug.LogWarning("⚠️ Damager prefab is NULL! Check inspector assignment.");
            }
        }
    }

    void SetStats() // Function to set the stats of the weapon. GK
    {
        // ✅ Destroy tất cả damager instances cũ khi lên cấp
        for (int i = activeDamagers.Count - 1; i >= 0; i--)
        {
            if (activeDamagers[i] != null)
                Destroy(activeDamagers[i]);
            activeDamagers.RemoveAt(i);
        }
        
        // ✅ Update spawn settings
        spawnTime = stats[weaponLevel].timeBetweenAttacks;
        spawnCounter = 0f; // Force spawn damager ngay lập tức
        
        // ✅ Scale weapon sprite theo range
        transform.localScale = Vector3.one * stats[weaponLevel].range;
        
        Debug.Log($"✅ ZoneWeapon leveled up! Range: {stats[weaponLevel].range}, Damage: {stats[weaponLevel].damage}");
    }
}
