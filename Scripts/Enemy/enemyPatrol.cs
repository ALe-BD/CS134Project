using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyPatrol : MonoBehaviour
{
    [SerializeField] public Transform pointA;
    [SerializeField] public Transform pointB;
    [SerializeField] public Transform player;

    private Rigidbody2D rb;
    public Animator anim;
    private Transform currentPoint;

    [SerializeField] public float speed = 2f;
    [SerializeField] public float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float lastAttackTime;

    [SerializeField] public float chaseSpeed = 3.5f;
    [SerializeField] public float patrolBuffer = 2f;

    private bool isDead = false;
    private bool isChasing = false;
    private bool isAttacking = false;

    void Start()
    {
        player = GameObject.Find("Player (1)").transform;
        
        rb = GetComponent<Rigidbody2D>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        currentPoint = pointB;

        anim.SetBool("isRunning", true);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        float minX = Mathf.Min(pointA.position.x, pointB.position.x) - patrolBuffer;
        float maxX = Mathf.Max(pointA.position.x, pointB.position.x) + patrolBuffer;

        // Restart patrol
        if (isChasing && (transform.position.x < minX || transform.position.x > maxX))
        {
            isChasing = false;
            currentPoint = GetClosestPoint();
        }
        
        if (isAttacking)
        {
            rb.velocity = Vector2.zero; // ensure no movement
            return;
        }

        // Attack & chase
        if (!isAttacking && distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackThenChase());
            return;
        }

        // Chase
        if (isChasing)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * chaseSpeed, 0);

            transform.localScale = new Vector3(-Mathf.Sign(direction.x), 1, 1);

            anim.SetBool("isRunning", true);
            anim.SetBool("isIdle", false);

            return;
        }

        // Patrol mode
        float patrolDirection = Mathf.Sign(currentPoint.position.x - transform.position.x);
        rb.velocity = new Vector2(patrolDirection * speed, 0);

        transform.localScale = new Vector3(-patrolDirection, 1, 1);

        anim.SetBool("isRunning", true);
        anim.SetBool("isIdle", false);
        
        if (Mathf.Abs(transform.position.x - currentPoint.position.x) < 0.1f)
        {
            currentPoint = currentPoint == pointA ? pointB : pointA;
        }
    }

    Transform GetClosestPoint()
    {
        float distA = Vector2.Distance(transform.position, pointA.position);
        float distB = Vector2.Distance(transform.position, pointB.position);
        return distA < distB ? pointA : pointB;
    }

    IEnumerator AttackThenChase()
    {
        isAttacking = true;

        rb.velocity = Vector2.zero;
        
        FacePlayer();

        anim.SetBool("isRunning", false);
        anim.SetBool("isIdle", false);

        lastAttackTime = Time.time;
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(2f);

        isChasing = true;
        isAttacking = false;
    }

    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;

        rb.velocity = Vector2.zero;
        anim.SetBool("isRunning", false);
        anim.SetBool("isIdle", false);
        anim.SetTrigger("death");

        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 2f);
    }
    
    void FacePlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        transform.localScale = new Vector3(-direction, 1, 1);
    }
}