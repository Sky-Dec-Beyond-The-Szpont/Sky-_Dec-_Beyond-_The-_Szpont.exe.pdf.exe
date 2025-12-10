using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float detectionRange = 5f;    // Zasiêg wykrywania gracza
    public float moveSpeed = 3f;         // Prêdkoœæ poruszania
    public int damage = 10;              // Obra¿enia zadawane graczowi przy kontakcie

    private Transform player;
    private Rigidbody2D rb;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Jeœli gracz w zasiêgu, pod¹¿aj za nim
        if (distance <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // zatrzymaj siê jeœli gracz poza zasiêgiem
        }
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Atak jeœli dotkniemy gracza
        if (collision.gameObject.CompareTag("Player"))
        {
            // Tu mo¿esz np. wywo³aæ metodê w skrypcie gracza do zadawania obra¿eñ
            collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}
