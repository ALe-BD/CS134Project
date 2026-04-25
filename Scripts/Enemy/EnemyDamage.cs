using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1;
    public float hitCooldown = 0.5f; // prevents rapid repeat hits

    private float lastHitTime = -999f;
    private Collider2D hitbox;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();

        if (hitbox != null)
            hitbox.isTrigger = true;
    }

    // 🔥 Call this from animation when attack hits
    public void DealDamage()
    {
        Debug.Log("DealDamage called");
        if (Time.time < lastHitTime + hitCooldown)
            return;

        if (hitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            hitbox.bounds.center,
            hitbox.bounds.size,
            0f
        );

        foreach (Collider2D col in hits)
        {
            if (!col.CompareTag("Player")) continue;

            HealthManager health = col.GetComponent<HealthManager>();

            if (health != null)
            {
                Debug.Log("Enemy hit player!");
                health.Damaged(damage);

                lastHitTime = Time.time; // start cooldown
                break; // only hit once
            }
        }
    }
}