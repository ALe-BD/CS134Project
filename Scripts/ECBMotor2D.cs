using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ECBMotor2D : MonoBehaviour
{
    [Header("ECB (environment collision shape)")]
    [Tooltip("ECB box size in world units (width, height)")]
    public Vector2 ecbSize = new Vector2(0.6f, 1.4f);

    [Tooltip("ECB center offset from transform.position (world units).")]
    public Vector2 ecbOffset = new Vector2(0f, 0.7f);

    [Header("Collision Masks")]
    public LayerMask solidMask;
    public LayerMask platformMask;
    public LayerMask oneWayNoDropMask;

    [Header("Moving Platforms")]
    [SerializeField] private bool rideMovingPlatforms = true;
    [SerializeField] private bool preservePlatformMomentum = true;
    [SerializeField] private float preservedPerc = 1f;
    private bool wasGroundedLastFrame;
    private Vector2 lastPlatformVelocity;
    public Vector2 PlatformVelocity { get; private set; }
    private Collider2D groundCollider;
    private Rigidbody2D groundBody;
    private Vector2 lastGroundBodyPos;

    [Header("Movement")]
    public float gravity = 40f;
    public float maxFallSpeed = 30f;
    public float groundSnapDistance = 0.08f;
    public float skin = 0.02f;

    [Header("Slopes")]
    [SerializeField] private bool useSlopeTangent = true;
    [SerializeField] private float groundStickVelocity = 2.0f; // small downward to stay glued on slopes

    [Tooltip("Treat surfaces as ground if normal.y >= this value.")]
    [Range(0f, 1f)]
    public float minGroundNormalY = 0.7f;

    [Header("Drop-through Platforms")]
    [Tooltip("Seconds to ignore platform collisions after pressing down to drop.")]
    public float dropThroughTime = 0.12f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // Public state
    public Vector2 Velocity { get; private set; }
    public bool Grounded { get; private set; }
    public Vector2 GroundNormal { get; private set; }
    public bool DisableGravity { get; set; }

    float dropTimer;

    // Optional: expose ECB bottom for other systems (ledge checks, etc.)
    public Vector2 ECBWorldCenter => (Vector2)transform.position + ecbOffset;
    public float ECBHalfHeight => ecbSize.y * 0.5f;
    public float ECBHalfWidth => ecbSize.x * 0.5f;
    public Vector2 ECBWorldBottom => ECBWorldCenter + Vector2.down * (ECBHalfHeight);

    [Header("Depenetration (eject from solids)")]
    [SerializeField] private BoxCollider2D ecbQueryCollider;
    [SerializeField] private int depenetrationMaxIters = 10;
    [SerializeField] private float depenetrationExtra = 0.001f; // tiny bias to fully clear seams
    [SerializeField] private int overlapBufferSize = 32;

    private Collider2D[] _overlapBuf;

    void Awake()
    {
        if (!ecbQueryCollider)
            ecbQueryCollider = GetComponent<BoxCollider2D>();

        if (ecbQueryCollider)
        {
            ecbQueryCollider.isTrigger = true;
            ecbQueryCollider.size = ecbSize;
            ecbQueryCollider.offset = ecbOffset;
        }

        _overlapBuf = new Collider2D[overlapBufferSize];
    }

    void DepenetrateFromSolids()
    {
        if (!ecbQueryCollider) return;

        // Keep query collider in sync with ECB settings (in case you tweak in inspector)
        ecbQueryCollider.size = ecbSize;
        ecbQueryCollider.offset = ecbOffset;
        ecbQueryCollider.isTrigger = true;

        for (int iter = 0; iter < depenetrationMaxIters; iter++)
        {
            int count = Physics2D.OverlapBoxNonAlloc(
                ECBWorldCenter,
                ecbSize,
                0f,
                _overlapBuf,
                solidMask
            );

            if (count == 0) return; // clean

            // Choose the single strongest push this iteration (stable at corners)
            bool foundOverlap = false;
            Vector2 bestPush = Vector2.zero;
            float bestAbsDistance = 0f;

            for (int i = 0; i < count; i++)
            {
                Collider2D other = _overlapBuf[i];
                if (!other || other == ecbQueryCollider) continue;

                // Distance gives us normal + signed distance
                ColliderDistance2D d = Physics2D.Distance(ecbQueryCollider, other);

                if (!d.isOverlapped) continue;

                foundOverlap = true;

                // d.distance is negative when overlapped
                float absDist = -d.distance;

                if (absDist > bestAbsDistance)
                {
                    bestAbsDistance = absDist;

                    // Push OUT along d.normal by the penetration depth + skin bias
                    bestPush = d.normal * (d.distance - skin - depenetrationExtra);
                    // since d.distance is negative, this becomes a push along +normal
                }
            }

            if (!foundOverlap) return;

            // Apply the best push
            transform.position += (Vector3)bestPush;

            // Optional: if you want, kill velocity when forcibly ejected
            // Velocity = Vector2.zero;
        }
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void SetVerticalVelocity(float vy) => Velocity = new Vector2(Velocity.x, vy);
    public void AddVelocity(Vector2 dv) => Velocity += dv;

    public void RequestDropThrough()
    {
        dropTimer = dropThroughTime;
    }

    void Tick(float dt)
    {
        DepenetrateFromSolids();

        ApplyMovingPlatformMotion(dt);
        // Timers
        if (dropTimer > 0f) dropTimer -= dt;

        // Gravity
        if(!DisableGravity)
        {
            float vy = Velocity.y - gravity * dt;
            if (vy < -maxFallSpeed) vy = -maxFallSpeed;
            Velocity = new Vector2(Velocity.x, vy);
        }
        
        if (Grounded && Velocity.y <= 0.01f)
        {
            Velocity = new Vector2(Velocity.x, -groundStickVelocity);
        }
        // Move: resolve axis separately for predictability (platform-fighter style)
        Vector2 delta = Velocity * dt;
        if (useSlopeTangent && Grounded && Velocity.y <= 0.01f)
        {
            Vector2 tangent = GetGroundTangent();

            // take intended horizontal displacement and move it along the slope
            Vector2 slopeMove = tangent * (delta.x);

            if (Mathf.Abs(slopeMove.x) > 0f || Mathf.Abs(slopeMove.y) > 0f)
                MoveAndCollide(slopeMove, axisIsVertical: false);

            // then apply remaining vertical (usually just the small stick velocity)
            if (Mathf.Abs(delta.y) > 0f)
                MoveAndCollide(new Vector2(0f, delta.y), axisIsVertical: true);
        }
        else
        {
        // Horizontal
        if (Mathf.Abs(delta.x) > 0f)
            MoveAndCollide(new Vector2(delta.x, 0f), axisIsVertical: false);

        // Vertical (handles landing / head bonk)
        if (Mathf.Abs(delta.y) > 0f)
            MoveAndCollide(new Vector2(0f, delta.y), axisIsVertical: true);
        }
        
        // Ground check + snap
        UpdateGroundedAndSnap();
        // After grounding is updated, preserve platform momentum if we just left the ground
        if (preservePlatformMomentum)
        {
            bool justLeftGround = (wasGroundedLastFrame && !Grounded);

            if (justLeftGround)
            {
                // Add platform velocity once so it carries into the air
                Velocity += lastPlatformVelocity;
            }
        }

        // Store for next frame
        wasGroundedLastFrame = Grounded;
    }

    void MoveAndCollide(Vector2 move, bool axisIsVertical)
    {
        Vector2 startPos = transform.position;
        Vector2 center = (Vector2)startPos + ecbOffset;

        float distance = move.magnitude;
        Vector2 dir = move.normalized;

        // Decide which layers we collide with for this move
        LayerMask mask = solidMask;

        // Only consider platforms when moving DOWN and not dropping through
        bool movingDown = axisIsVertical && dir.y < 0f;
        if (movingDown)
        {
            // Always land on "one-way but NOT droppable" platforms while falling
            mask |= oneWayNoDropMask;

            // Only land on droppable platforms if not currently dropping through
            if (dropTimer <= 0f)
                mask |= platformMask;
        }

        // Then only gate platformMask by dropTimer:
        if (dropTimer > 0f)
            mask &= ~platformMask;

        // Cast ECB box
        RaycastHit2D hit = Physics2D.BoxCast(
            center,
            ecbSize,
            0f,
            dir,
            distance + skin,
            mask
        );

        if (!hit)
        {
            transform.position = (Vector3)startPos + (Vector3)move;
            return;
        }

        // If the hit was a platform, validate one-way rules
        if (((1 << hit.collider.gameObject.layer) & platformMask) != 0)
        {
            if (!IsValidPlatformHit(hit, center))
            {
                // Ignore this platform hit: move fully.
                transform.position = (Vector3)startPos + (Vector3)move;
                return;
            }
        }

        // Move up to contact (minus skin)
        float allowed = Mathf.Max(0f, hit.distance - skin);
        Vector2 actualMove = dir * Mathf.Min(distance, allowed);
        transform.position = (Vector3)startPos + (Vector3)actualMove;

        // Resolve velocity (stop into surface)
        if (axisIsVertical)
        {
            // If we hit something while moving up/down, zero Y velocity
            Velocity = new Vector2(Velocity.x, 0f);
        }
        else
        {
            Velocity = new Vector2(0f, Velocity.y);
        }
    }

    bool IsValidPlatformHit(RaycastHit2D hit, Vector2 ecbCenter)
{
    // Only treat as one-way ground if we're moving downward or basically not going up
    if (Velocity.y > 0.01f)
        return false;

    // Must be mostly upward-facing (works for slopes too)
    if (hit.normal.y < minGroundNormalY)
        return false;

    // Use the contact point (local surface height), NOT bounds.max.y (which breaks on slopes)
    float surfaceY = hit.point.y;

    // ECB bottom BEFORE moving
    float ecbBottomY = ecbCenter.y - ECBHalfHeight;

    // Only land if bottom is above (or very slightly above) the surface at the contact point
    const float tolerance = 0.02f; // tweak if needed
    if (ecbBottomY < surfaceY - tolerance)
        return false;

    return true;
}

    private static bool LayerInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    private void UpdateGroundedAndSnap()
    {
        if (Velocity.y > 0.01f)
        {
            Grounded = false;
            GroundNormal = Vector2.up;

            groundCollider = null;
            groundBody = null;
            PlatformVelocity = Vector2.zero;
            return;
        }
        bool allowSnap = Velocity.y <= 0.01f;
        Vector2 pos = transform.position; 
        Vector2 center = pos + ecbOffset; 
        // Ground probe: short cast downward 
        LayerMask mask = solidMask; 
        // Only consider one-way surfaces when not moving upward \
        if (Velocity.y <= 0.01f) { 
            // Always consider non-droppable one-ways for grounding 
            mask |= oneWayNoDropMask; 
            // Consider droppable platforms only if not dropping through 
            if (dropTimer <= 0f) mask |= platformMask; 
        } 
        RaycastHit2D hit = Physics2D.BoxCast( center, ecbSize, 0f, Vector2.down, groundSnapDistance + skin, mask ); 
        
        // if (hit) {
        //     Debug.Log($"Ground probe hit {hit.collider.name} dist={hit.distance:F3} normal={hit.normal} layer={hit.collider.gameObject.layer}");
        // } else {
        //     Debug.Log("Ground probe: no hit");
        // }

        if (hit) { 
            // Platform filtering 
            if (((1 << hit.collider.gameObject.layer) & platformMask) != 0) { 
                if (!IsValidPlatformHit(hit, center)) { 
                    Grounded = false; 
                    GroundNormal = Vector2.up; 
                    
                    groundCollider = null;
                    groundBody = null;
                    PlatformVelocity = Vector2.zero;
                    return; 
                } } 
                bool isGround = hit.normal.y >= minGroundNormalY; 
                if (isGround) { 
                     // Key change: don't "re-ground" and snap while rising
                    Grounded = allowSnap;
                    GroundNormal = hit.normal;

                    if (Grounded)
                        {
                            groundCollider = hit.collider;
                            groundBody = hit.rigidbody;

                            // Initialize last position when we land (prevents 1-frame pop)
                            if (groundBody != null)
                                lastGroundBodyPos = groundBody.position;
                            else
                                lastGroundBodyPos = groundCollider.transform.position;
                        }
                    // Snap down (prevents hovering) ONLY when not moving upward
                    if (allowSnap)
                    {
                        float snap = Mathf.Max(0f, hit.distance - skin);
                        if (snap > 0f)
                            transform.position = (Vector3)(pos + Vector2.down * snap);
                    }
                    // If grounded, don’t keep accumulating downward velocity
                    if (Grounded && Velocity.y < 0f)
                        Velocity = new Vector2(Velocity.x, 0f);
                    return; 
                } 
            } 
            Grounded = false; 
            GroundNormal = Vector2.up;

            groundCollider = null;
            groundBody = null;
            PlatformVelocity = Vector2.zero;
            
    }
    private Vector2 GetGroundTangent()
    {
        // Perpendicular to normal (points "along" the surface)
        Vector2 t = new Vector2(GroundNormal.y, -GroundNormal.x);
        return t.normalized;
    }

    private static Vector2 ProjectOn(Vector2 v, Vector2 dirNormalized)
    {
        return dirNormalized * Vector2.Dot(v, dirNormalized);
    }

    private void ApplyMovingPlatformMotion(float dt)
    {
        if (!rideMovingPlatforms)
        {
            PlatformVelocity = Vector2.zero;
            return;
        }

        if (!Grounded || groundCollider == null)
        {
            PlatformVelocity = Vector2.zero;
            groundBody = null;
            return;
        }

        // Where is the platform now?
        Vector2 currentPos;

        if (groundBody != null)
            currentPos = groundBody.position;
        else
            currentPos = (Vector2)groundCollider.transform.position;

        // How far did it move since last frame?
        Vector2 delta = currentPos - lastGroundBodyPos;

        // Move the player by the platform's delta
        if (delta != Vector2.zero)
            transform.position += (Vector3)delta;

        // Expose velocity for other systems (optional)
        PlatformVelocity = (dt > 0f) ? (delta / dt) : Vector2.zero;

        // Store for next frame
        lastPlatformVelocity = PlatformVelocity;
        lastGroundBodyPos = currentPos;
    }
    public void InheritPlatformVelocityOnce()
    {
        Velocity += lastPlatformVelocity * preservedPerc;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector2 center = (Vector2)transform.position + ecbOffset;

        Gizmos.color = Grounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, ecbSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + Vector2.down * (groundSnapDistance + skin));
    }
}
