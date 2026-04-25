using UnityEngine;

public class HitboxFollower2D : MonoBehaviour
{
    public Transform player;
    public float distanceInFront = 1.5f;
    public Vector2 offset;

    private float facingDirection = 1f; // 1 = right, -1 = left
    private Vector3 lastPosition;

    void Start()
    {
        if (player != null)
        {
            lastPosition = player.position;
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateFacingDirection();
        UpdateHitboxPosition();
    }

    void UpdateFacingDirection()
    {
        float deltaX = player.position.x - lastPosition.x;

        if (Mathf.Abs(deltaX) > 0.01f)
        {
            facingDirection = Mathf.Sign(deltaX);
        }

        lastPosition = player.position;
    }

    void UpdateHitboxPosition()
    {
        Vector3 newPosition = player.position +
                              new Vector3(facingDirection * distanceInFront, 0f, 0f) +
                              (Vector3)offset;

        transform.position = newPosition;
    }
}