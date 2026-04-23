using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1;

    private bool canDamage;
    private bool hasHit;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public void SetCanDamage(bool value)
    {
        canDamage = value;

        if (value)
        {
            hasHit = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision);
    }

    private void TryDealDamage(Collider2D collision)
    {
        if (!canDamage || hasHit) return;

        if (!collision.CompareTag("Player")) return;

        HealthManager health = collision.GetComponent<HealthManager>();

        if (health != null)
        {
            health.Damaged(damage);
            hasHit = true;
        }
    }
}