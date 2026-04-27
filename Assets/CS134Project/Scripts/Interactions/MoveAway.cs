using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAway : InteractionScript
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxMoveSpeed = 10f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private Vector2 moveDirection = Vector2.right;

    private Vector3 targetPosition;
    private bool shouldMove = false;
    private Transform player;

    private void Start()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (shouldMove && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            float closeness = 1f - Mathf.Clamp01(distance / detectionRange);
            float currentSpeed = Mathf.Lerp(moveSpeed, maxMoveSpeed, closeness);

            transform.position += (Vector3)(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            shouldMove = true;
            player = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            shouldMove = false;
            player = null;
        }
    }
}

