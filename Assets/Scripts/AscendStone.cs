using UnityEngine;

public class AscendStone : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 1.5f;
    public float moveSpeed = 5f;
    public Vector3 targetOffset = Vector3.zero;

    private bool isMovingToPlayer = false;
    private PlayerController playerController;
    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // ✅ Check xem player có trong range không
        if (!isMovingToPlayer && PlayerController.instance != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, PlayerController.instance.transform.position);
            if (distToPlayer < pickupRange)
            {
                isMovingToPlayer = true;
            }
        }

        // ✅ Di chuyển tới player nếu trong range
        if (isMovingToPlayer && PlayerController.instance != null)
        {
            Vector3 targetPos = PlayerController.instance.transform.position + targetOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // ✅ Nhặt được khi gần player
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                OnPickup();
            }
        }
    }

    private void OnPickup()
    {
        // ✅ Trigger promotion menu
        if (ExperienceLevelController.instance != null)
        {
            ExperienceLevelController.instance.ShowAscendStonePromotionMenu();
        }
        else
        {
            Debug.LogWarning("⚠️ ExperienceLevelController not found!");
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Được gọi từ trigger hoặc khi player gần
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            OnPickup();
        }
    }
}
