using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public int health = 50;

    // Decrement enemy hitpoints
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(name + " took " + damage + " damage");

        if (health <= 0)
        {
            Die();
        }
    }

    // Destroy enemy object when health reaches 0
    void Die()
    {
        Debug.Log(name + " died");
        Destroy(gameObject);
    }
}