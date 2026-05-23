using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 1.5f; // Tầm hút
    public float moveSpeed = 5f; // Tốc độ hút
    private bool isMovingToPlayer = false;

    void Update()
    {
        // Cơ chế hút về phía người chơi (giống Exp và Coin)
        if (PlayerHealthController.instance != null && PlayerHealthController.instance.gameObject.activeSelf)
        {
            float distance = Vector3.Distance(transform.position, PlayerHealthController.instance.transform.position);

            if (distance < pickupRange)
            {
                isMovingToPlayer = true;
            }

            if (isMovingToPlayer)
            {
                transform.position = Vector3.MoveTowards(transform.position, PlayerHealthController.instance.transform.position, moveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            float maxHP = PlayerHealthController.instance.maxHealth;
            float currentHP = PlayerHealthController.instance.currentHealth;

            // Nếu máu đầy thì không nhặt (tùy chọn, bạn có thể bỏ check này nếu muốn ăn luôn dù đầy máu)
            if (currentHP >= maxHP) return;

            // Tính lượng máu đã mất
            float missingHP = maxHP - currentHP;

            // Tính lượng máu sẽ hồi (10% của lượng máu đã mất)
            float calculatedHeal = missingHP * 0.10f;

            // Chốt chặn: hồi tối thiểu 1 HP
            if (calculatedHeal < 1f && missingHP > 0)
            {
                calculatedHeal = 1f;
            }

            // Cộng máu
            PlayerHealthController.instance.currentHealth += calculatedHeal;

            // Đảm bảo không vượt quá Max HP
            if (PlayerHealthController.instance.currentHealth > maxHP)
            {
                PlayerHealthController.instance.currentHealth = maxHP;
            }

            // Cập nhật UI
            if (PlayerHealthController.instance.healthSlider != null)
            {
                PlayerHealthController.instance.healthSlider.value = PlayerHealthController.instance.currentHealth;
            }

            // Phát âm thanh hồi máu (Giả sử âm thanh số 2 là nhặt đồ)
            if (SFXManager.instance != null)
            {
                SFXManager.instance.PlaySFXPitched(2);
            }

            Destroy(gameObject);
        }
    }
}