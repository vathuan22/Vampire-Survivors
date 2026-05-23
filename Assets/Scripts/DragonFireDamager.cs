using UnityEngine;

public class DragonFireDamager : MonoBehaviour
{
    public float damageAmount = 15f;
    public float speed = 8f;
    public float lifeTime = 3f;
    public Rigidbody2D theRB;

    void Start()
    {
        // Tự hủy sau 3 giây để không làm nặng máy
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Bay thẳng về phía trước (trục X) theo hướng đã xoay
        if (theRB != null)
        {
            theRB.velocity = transform.right * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealthController.instance.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }
}