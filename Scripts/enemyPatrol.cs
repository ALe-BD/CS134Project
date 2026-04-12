using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyPatrol : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    private Rigidbody2D rb;
    private Animator anim;
    private Transform currentPoint;
    public float speed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentPoint = pointB;
        anim.SetBool("isRunning", true);
    }

    void FixedUpdate()
    {
        float direction = Mathf.Sign(currentPoint.position.x - transform.position.x);
        rb.velocity = new Vector2(direction * speed, rb.velocity.y);

        transform.localScale = new Vector3(-direction, 1, 1);
        
        if (Mathf.Abs(transform.position.x - currentPoint.position.x) < 0.1f)
        {
            currentPoint = currentPoint == pointA ? pointB : pointA;
        }
    }
}   