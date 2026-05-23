using UnityEngine;

public class BossEventSpawner : MonoBehaviour
{
    [Header("Boss Event")]
    public GameObject dragonBossPrefab;
    public float spawnTimeInSeconds = 300f; // 300 giây = Phút thứ 5
    
    private bool hasSpawned = false;

    void Update()
    {
        if (!hasSpawned && LevelManager.instance != null)
        {
            // Kiểm tra đồng hồ của Level Manager
            if (LevelManager.instance.timer >= spawnTimeInSeconds)
            {
                SpawnBoss();
            }
        }
    }

    void SpawnBoss()
    {
        hasSpawned = true;
        
        // Cho Boss xuất hiện ở ngoài rìa màn hình (cách player 15 đơn vị)
        if (PlayerHealthController.instance != null)
        {
            Vector3 playerPos = PlayerHealthController.instance.transform.position;
            Vector3 spawnPos = playerPos + new Vector3(15f, 0f, 0f); 
            Instantiate(dragonBossPrefab, spawnPos, Quaternion.identity);
            
            Debug.Log("🐉 Cảnh báo: Boss Rồng đã xuất hiện ở phút thứ 5!");
        }
    }
}