using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ECBMotor2D : MonoBehaviour
{
    [Header("ECB (environment collision shape)")]
    [Tooltip("ECB diamond size in world units (width tip-to-tip, height tip-to-tip)")]
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
    [SerializeField] private float groundStickVelocity = 2.0f;

    [Tooltip("Treat surfaces as ground if normal.y >= this value.")]
    [Range(0f, 1f)]
    public float minGroundNormalY = 0.7f;

    [Header("Drop-through Platforms")]
    [Tooltip("Seconds to ignore platform collisions after pressing down to drop.")]
    public float dropThroughTime = 0.12f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool debugCast = true;
    public Color castColor = Color.red;
    public Color hitColor = Color.green;

    // Public state
    public Vector2 Velocity { get; private set; }
    public bool Grounded { get; private set; }
    public Vector2 GroundNormal { get; private set; }
    public bool DisableGravity { get; set; }

    float dropTimer;

    // Diamond geometry helpers (still valid: tips are at +/- half-height and +/- half-width)
    public Vector2 ECBWorldCenter => (Vector2)transform.position + ecbOffset;
    public float ECBHalfHeight => ecbSize.y * 0.5f;
    public float ECBHalfWidth => ecbSize.x * 0.5f;
    public Vector2 ECBWorldBottom => ECBWorldCenter + Vector2.down * (ECBHalfHeight);

    [Header("Depenetration (eject from solids)")]
    [SerializeField] private PolygonCollider2D ecbQueryCollider;
    [SerializeField] private int depenetrationMaxIters = 10;
    [SerializeField] private float depenetrationExtra = 0.001f;
    [SerializeField] private int overlapBufferSize = 32;

    private Collider2D[] _overlapBuf;

    // Cast buffer (we only need the closest hit)
    private RaycastHit2D[] _castHits = new RaycastHit2D[8];

    void Awake()
    {
        Physics2D.queriesHitTriggers = false;
        if (!ecbQueryCollider)
            ecbQueryCollider = GetComponent<PolygonCollider2D>();

        if (!ecbQueryCollider)
            ecbQueryCollider = gameObject.AddComponent<PolygonCollider2D>();

        ecbQueryCollider.isTrigger = true;
        SyncDiamondCollider();
        Debug.Log($"ECB enabled={ecbQueryCollider.enabled} pathCount={ecbQueryCollider.pathCount} points={ecbQueryCollider.GetPath(0).Length}");

        _overlapBuf = new Collider2D[overlapBufferSize];
    }

    void SyncDiamondCollider()
    {
        if (!ecbQueryCollider) return;

        // Diamond points in local space around collider origin (0,0), using tip-to-tip sizes.
        // Order matters (clockwise or ccw). We'll do ccw:
        // top -> right -> bottom -> left
        Vector2 top    = new Vector2(0f,  ECBHalfHeight);
        Vector2 right  = new Vector2( ECBHalfWidth, 0f);
        Vector2 bottom = new Vector2(0f, -ECBHalfHeight);
        Vector2 left   = new Vector2(-ECBHalfWidth, 0f);

        ecbQueryCollider.pathCount = 1;
        ecbQueryCollider.SetPath(0, new Vector2[] { top, right, bottom, left });

        // Offset positions the diamond center relative to transform.position
        ecbQueryCollider.offset = ecbOffset;
        ecbQueryCollider.isTrigger = true;
    }

    void DepenetrateFromSolids()
    {
        if (!ecbQueryCollider) return;

        // Keep query collider in sync with ECB settings (in case you tweak in inspector)
        SyncDiamondCollider();

        var filter = BuildFilter(solidMask);

        for (int iter = 0; iter < depenetrationMaxIters; iter++)
        {
            int count = ecbQueryCollider.OverlapCollider(filter, _overlapBuf);
            if (count == 0) return;

            bool foundOverlap = false;
            Vector2 bestPush = Vector2.zero;
            float bestAbsDistance = 0f;

            for (int i = 0; i < count; i++)
            {
                Collider2D other = _overlapBuf[i];
                if (!other || other == ecbQueryCollider) continue;

                ColliderDistance2D d = Physics2D.Distance(ecbQueryCollider, other);
                if (!d.isOverlapped) continue;

                foundOverlap = true;

                // d.distance is negative when overlapped
                float absDist = -d.distance;
                if (absDist > bestAbsDistance)
                {
                    bestAbsDistance = absDist;

                    // Push OUT along normal by penetration depth + bias
                    bestPush = d.normal * (d.distance - skin - depenetrationExtra);
                }
            }

            if (!foundOverlap) return;

            transform.position += (Vector3)bestPush;
        }
    }

    void Update()
    {
        int floorLayer = GameObject.Find("Ground").layer;
        Debug.Log($"Floor layer={floorLayer} includedInSolidMask={(solidMask.value & (1 << floorLayer)) != 0}");
        Tick(Time.deltaTime);
    }
    void LateUpdate()
    {
        Vector2 origin = (Vector2)transform.position + ecbOffset;
        float dist = 5f;

        // Hit ANY layer
        RaycastHit2D hitAny = Physics2D.Raycast(origin, Vector2.down, dist, ~0);

        Debug.DrawLine(origin, origin + Vector2.down * dist, Color.magenta);

        if (hitAny.collider)
        {
            Debug.DrawLine(hitAny.point, hitAny.point + hitAny.normal * 0.5f, Color.green);
            Debug.Log($"[ANY] Hit {hitAny.collider.name} layer={hitAny.collider.gameObject.layer} isTrigger={hitAny.collider.isTrigger}");
        }
        else
        {
            Debug.LogWarning("[ANY] Raycast hit NOTHING (even with ~0 mask). Floor collider disabled/inactive or not 2D?");
        }

        // Hit ONLY your solidMask
        RaycastHit2D hitSolid = Physics2D.Raycast(origin, Vector2.down, dist, solidMask);
        Debug.DrawLine(origin, origin + Vector2.down * dist, Color.cyan);

        if (hitSolid.collider)
        {
            Debug.DrawLine(hitSolid.point, hitSolid.point + hitSolid.normal * 0.5f, Color.yellow);
            Debug.Log($"[SOLID] Hit {hitSolid.collider.name} layer={hitSolid.collider.gameObject.layer}");
        }
        else
        {
            Debug.LogWarning($"[SOLID] Raycast hit NOTHING. solidMask.value={solidMask.value}");
        }
    }

    public void SetVerticalVelocity(float vy) => Velocity = new Vector2(Velocity.x, vy);
    public void AddVelocity(Vector2 dv) => Velocity += dv;

    public void RequestDropThrough()
    {
        dropTimer = dropThroughTime;
    }

    void Tick(float dt)
    {
        //DepenetrateFromSolids();

        ApplyMovingPlatformMotion(dt);

        if (dropTimer > 0f) dropTimer -= dt;

        if (!DisableGravity)
        {
            float vy = Velocity.y - gravity * dt;
            if (vy < -maxFallSpeed) vy = -maxFallSpeed;
            Velocity = new Vector2(Velocity.x, vy);
        }

        if (Grounded && Velocity.y <= 0.01f)
        {
            Velocity = new Vector2(Velocity.x, -groundStickVelocity);
        }

        Vector2 delta = Velocity * dt;

        if (useSlopeTangent && Grounded && Velocity.y <= 0.01f)
        {
            Vector2 tangent = GetGroundTangent();
            Vector2 slopeMove = tangent * (delta.x);

            if (slopeMove != Vector2.zero)
                MoveAndCollide(slopeMove, axisIsVertical: false);

            if (Mathf.Abs(delta.y) > 0f)
                MoveAndCollide(new Vector2(0f, delta.y), axisIsVertical: true);
        }
        else
        {
            if (Mathf.Abs(delta.x) > 0f)
                MoveAndCollide(new Vector2(delta.x, 0f), axisIsVertical: false);

            if (Mathf.Abs(delta.y) > 0f)
                MoveAndCollide(new Vector2(0f, delta.y), axisIsVertical: true);
        }

        UpdateGroundedAndSnap();

        if (preservePlatformMomentum)
        {
            bool justLeftGround = (wasGroundedLastFrame && !Grounded);
            if (justLeftGround)
                Velocity += lastPlatformVelocity;
        }

        wasGroundedLastFrame = Grounded;
    }

    void MoveAndCollide(Vector2 move, bool axisIsVertical)
    {
        SyncDiamondCollider();

        Vector2 startPos = transform.position;
        float distance = move.magnitude;
        if (distance <= 0f) return;

        Vector2 dir = move / distance;

        // Build collision mask for this move
        LayerMask mask = solidMask;

        bool movingDown = axisIsVertical && dir.y < 0f;
        if (movingDown)
        {
            mask |= oneWayNoDropMask;

            if (dropTimer <= 0f)
                mask |= platformMask;
        }

        if (dropTimer > 0f)
            mask &= ~platformMask;

        // --- KITE CAST (ignores sibling colliders) ---
        if (!KiteCast(dir, distance + skin, mask, out RaycastHit2D hit))
        {
            // No collision: move freely
            transform.position = (Vector3)startPos + (Vector3)move;
            return;
        }

        // Platform one-way validation (only relevant if we actually hit a platform)
        if (((1 << hit.collider.gameObject.layer) & platformMask) != 0)
        {
            Vector2 ecbCenter = (Vector2)startPos + ecbOffset;
            if (!IsValidPlatformHit(hit, ecbCenter))
            {
                // Ignore this platform hit: move fully
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
            Velocity = new Vector2(Velocity.x, 0f);
        else
            Velocity = new Vector2(0f, Velocity.y);
    }

    bool IsValidPlatformHit(RaycastHit2D hit, Vector2 ecbCenter)
    {
        if (Velocity.y > 0.01f)
            return false;

        if (hit.normal.y < minGroundNormalY)
            return false;

        float surfaceY = hit.point.y;
        float ecbBottomY = ecbCenter.y - ECBHalfHeight;

        const float tolerance = 0.02f;
        if (ecbBottomY < surfaceY - tolerance)
            return false;

        return true;
    }

    private void UpdateGroundedAndSnap()
    {
        SyncDiamondCollider();

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

        LayerMask mask = solidMask;

        if (Velocity.y <= 0.01f)
        {
            mask |= oneWayNoDropMask;
            if (dropTimer <= 0f) mask |= platformMask;
        }

        if (!KiteCast(Vector2.down, groundSnapDistance + skin, mask, out RaycastHit2D best))
        {
            Grounded = false;
            GroundNormal = Vector2.up;
            groundCollider = null;
            groundBody = null;
            PlatformVelocity = Vector2.zero;
            return;
        }

        DebugDrawKiteCast(Vector2.down, groundSnapDistance + skin, 1);

        // Now continue grounding logic using "best"
        bool isGround = best.normal.y >= minGroundNormalY;

        if (isGround)
        {
            Grounded = allowSnap;
            GroundNormal = best.normal;

            if (Grounded)
            {
                groundCollider = best.collider;
                groundBody = best.rigidbody;

                if (groundBody != null)
                    lastGroundBodyPos = groundBody.position;
                else
                    lastGroundBodyPos = groundCollider.transform.position;
            }

            if (allowSnap)
            {
                float snap = Mathf.Max(0f, best.distance - skin);
                if (snap > 0f)
                    transform.position += (Vector3)(Vector2.down * snap);
            }

            if (Grounded && Velocity.y < 0f)
                Velocity = new Vector2(Velocity.x, 0f);

            return;
            }
        

        Grounded = false;
        GroundNormal = Vector2.up;
        groundCollider = null;
        groundBody = null;
        PlatformVelocity = Vector2.zero;
    }

    private Vector2 GetGroundTangent()
    {
        Vector2 t = new Vector2(GroundNormal.y, -GroundNormal.x);
        return t.normalized;
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

        Vector2 currentPos = (groundBody != null)
            ? groundBody.position
            : (Vector2)groundCollider.transform.position;

        Vector2 delta = currentPos - lastGroundBodyPos;

        if (delta != Vector2.zero)
            transform.position += (Vector3)delta;

        PlatformVelocity = (dt > 0f) ? (delta / dt) : Vector2.zero;

        lastPlatformVelocity = PlatformVelocity;
        lastGroundBodyPos = currentPos;
    }

    public void InheritPlatformVelocityOnce()
    {
        Velocity += lastPlatformVelocity * preservedPerc;
    }
    private ContactFilter2D BuildFilter(LayerMask mask)
    {
        ContactFilter2D f = new ContactFilter2D();
        f.useLayerMask = true;
        f.layerMask = mask;

        // Don’t accidentally filter out everything
        f.useDepth = false;
        f.useNormalAngle = false;
    

        // Safe: allows hitting trigger colliders if any exist
        f.useTriggers = true;

        return f;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector2 center = (Vector2)transform.position + ecbOffset;

        // Draw diamond wire
        Vector2 top    = center + new Vector2(0f,  ECBHalfHeight);
        Vector2 right  = center + new Vector2( ECBHalfWidth, 0f);
        Vector2 bottom = center + new Vector2(0f, -ECBHalfHeight);
        Vector2 left   = center + new Vector2(-ECBHalfWidth, 0f);

        Gizmos.color = Grounded ? Color.green : Color.yellow;
        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + Vector2.down * (groundSnapDistance + skin));
    }

    private void DebugDrawKiteCast(Vector2 dir, float distance, int hitCount)
{
    if (!debugCast || !ecbQueryCollider) return;

    Vector2 center = (Vector2)transform.position + ecbOffset;

    float halfW = ECBHalfWidth;
    float halfH = ECBHalfHeight;

    // Kite corners at start
    Vector2 top    = center + new Vector2(0, halfH);
    Vector2 right  = center + new Vector2(halfW, 0);
    Vector2 bottom = center + new Vector2(0, -halfH);
    Vector2 left   = center + new Vector2(-halfW, 0);

    Debug.DrawLine(top, right, castColor);
    Debug.DrawLine(right, bottom, castColor);
    Debug.DrawLine(bottom, left, castColor);
    Debug.DrawLine(left, top, castColor);

    // Draw cast direction
    Debug.DrawLine(center, center + dir * distance, Color.cyan);

    // Draw kite at end position (max cast distance)
    Vector2 endOffset = dir * distance;
    Debug.DrawLine(top + endOffset, right + endOffset, castColor);
    Debug.DrawLine(right + endOffset, bottom + endOffset, castColor);
    Debug.DrawLine(bottom + endOffset, left + endOffset, castColor);
    Debug.DrawLine(left + endOffset, top + endOffset, castColor);

    // If we hit something, draw hit info
    if (hitCount > 0)
    {
        RaycastHit2D hit = _castHits[0];

        Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, hitColor);
        Debug.DrawRay(hit.centroid, hit.normal * 0.5f, hitColor);
    }
}
private bool KiteCast(Vector2 dir, float distance, LayerMask mask, out RaycastHit2D bestHit)
{
    bestHit = default;

    if (!ecbQueryCollider) return false;

    // Keep our collider in sync (important if you change size/offset at runtime)
    SyncDiamondCollider();

    // Build a ContactFilter2D that exactly matches the desired mask and trigger behavior
    ContactFilter2D filter = new ContactFilter2D();
    filter.useLayerMask = true;
    filter.layerMask = mask;
    filter.useTriggers = false; // don't hit triggers for movement/grounding
    filter.useDepth = false;
    filter.useNormalAngle = false;

    // Cast the polygon shape. This populates _castHits and returns number of hits
    int hitCount = ecbQueryCollider.Cast(dir, filter, _castHits, distance);

    if (hitCount <= 0) return false;

    // Choose the closest valid hit (ignore the query collider / siblings)
    float bestDist = float.PositiveInfinity;
    bool found = false;

    for (int i = 0; i < hitCount; i++)
    {
        var h = _castHits[i];
        if (!h.collider) continue;

        // Ignore hits on our own collider or any colliders belonging to the same root (optional)
        if (h.collider == ecbQueryCollider) continue;
        if (h.collider.transform.IsChildOf(transform)) continue;

        if (h.distance < bestDist)
        {
            bestDist = h.distance;
            bestHit = h;
            found = true;
        }
    }

    return found;
}
}
