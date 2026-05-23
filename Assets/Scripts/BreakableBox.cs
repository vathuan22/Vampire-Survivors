using UnityEngine;

public class BreakableBox : MonoBehaviour
{
    [Header("Box Stats")]
    public float health = 150f; // Máu của hộp đã được tăng

    [Header("Drops")]
    public GameObject healthPotionPrefab; // Kéo Prefab Bình máu
    public GameObject expOrbPrefab;       // Kéo Prefab Cục kinh nghiệm
    public GameObject coinPrefab;         // Kéo Prefab Tiền (Coin) vào đây

    [Header("Drop Settings")]
    [Range(0f, 1f)] public float dropChance = 0.8f; // 80% rớt đồ

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (DamageNumberController.instance != null)
        {
            // Gọi hệ thống có sẵn sinh ra con số sát thương ngay tại vị trí cái hộp
            DamageNumberController.instance.SpawnDamage(damageAmount, transform.position);
        }

        if (health <= 0)
        {
            BreakBox();
        }
    }

    private void BreakBox()
    {
        if (Random.value <= dropChance)
        {
            DropLoot();
        }

        Destroy(gameObject);
    }

    private void DropLoot()
    {
        // Rớt ngẫu nhiên 1 hoặc 2 món
        int dropCount = Random.Range(1, 3); 

        // Gom 3 loại vật phẩm vào một danh sách để quay thưởng
        GameObject[] possibleDrops = new GameObject[] { healthPotionPrefab, expOrbPrefab, coinPrefab };

        for (int i = 0; i < dropCount; i++)
        {
            // Quay ngẫu nhiên 0, 1 hoặc 2 (tương ứng với Máu, Exp, Coin)
            int randomIndex = Random.Range(0, possibleDrops.Length);
            GameObject itemToDrop = possibleDrops[randomIndex];

            if (itemToDrop != null)
            {
                // Cho vật phẩm văng ra vị trí ngẫu nhiên quanh hộp để khỏi bị đè lên nhau
                Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
                Instantiate(itemToDrop, transform.position + randomOffset, Quaternion.identity);
            }
        }
    }
}