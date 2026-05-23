using UnityEngine;

public class AscendStoneManager : MonoBehaviour
{
    public static AscendStoneManager instance;

    public GameObject ascendStonePrefab;
    public int spawnCount = 2;
    public float spawnRadius = 10f;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Spawn ascend stones khi người chơi max hết skills
    /// </summary>
    public void SpawnAscendStones()
    {
        if (ascendStonePrefab == null)
        {
            Debug.LogError("❌ ascendStonePrefab is not assigned!");
            return;
        }

        if (PlayerController.instance == null)
        {
            Debug.LogError("❌ PlayerController not found!");
            return;
        }

        Vector3 playerPos = PlayerController.instance.transform.position;

        for (int i = 0; i < spawnCount; i++)
        {
            // ✅ Spawn ngẫu nhiên xung quanh player
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(spawnRadius * 0.5f, spawnRadius);
            Vector3 spawnPos = playerPos + (Vector3)randomDir * randomDist;

            GameObject stone = Instantiate(ascendStonePrefab, spawnPos, Quaternion.identity);
            stone.name = $"AscendStone_{i}";

            Debug.Log($"✅ Spawned ascend stone #{i + 1} at {spawnPos}");
        }
    }
}
