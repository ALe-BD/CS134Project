using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    public int attackDamage = 10;
    public float attackRange = 2f;

    [Header("References")]
    public Animator animator;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    private float lastAttackTime;

    // Check attack cooldown
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    // Attack on input
    void Attack()
    {
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Hit " + enemy.name);

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Debug method
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}