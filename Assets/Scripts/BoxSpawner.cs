using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject boxPrefab;         // Kéo Prefab Cái Hộp (Crate) vào đây
    public float spawnInterval = 7f;     // Cứ 7 giây xuất hiện 1 cái
    
    [Header("Distance From Player")]
    public float minSpawnRadius = 6f;    // Khoảng cách tối thiểu (để không đẻ ngay đỉnh đầu player)
    public float maxSpawnRadius = 12f;   // Khoảng cách tối đa (để không đẻ quá xa tầm nhìn)

    private float spawnTimer;

    void Start()
    {
        // Đặt thời gian đếm ngược ban đầu là 7 giây
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        // Chỉ đẻ hộp khi người chơi còn sống
        if (PlayerHealthController.instance != null && PlayerHealthController.instance.gameObject.activeSelf)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0)
            {
                SpawnBoxAroundPlayer();
                spawnTimer = spawnInterval; // Đặt lại đồng hồ 7 giây cho lần tiếp theo
            }
        }
    }

    void SpawnBoxAroundPlayer()
    {
        if (boxPrefab == null)
        {
            Debug.LogWarning("⚠️ BoxSpawner chưa được kéo thả Box Prefab kìa bạn ơi!");
            return;
        }

        // 1. Lấy vị trí hiện tại của nhân vật
        Vector3 playerPos = PlayerHealthController.instance.transform.position;

        // 2. Tính toán một tọa độ ngẫu nhiên dạng vòng tròn xung quanh nhân vật
        Vector2 randomDirection = Random.insideUnitCircle.normalized; // Chọn một hướng ngẫu nhiên (360 độ)
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius); // Chọn khoảng cách ngẫu nhiên từ 6 đến 12 mét
        
        Vector3 spawnPosition = playerPos + new Vector3(randomDirection.x * randomDistance, randomDirection.y * randomDistance, 0f);

        // 3. Tiến hành tạo ra cái hộp tại tọa độ đó
        Instantiate(boxPrefab, spawnPosition, Quaternion.identity);
    }
}