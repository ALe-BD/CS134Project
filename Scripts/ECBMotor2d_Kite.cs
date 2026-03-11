using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ecbmotor2D with a kite-shaped Environment Collision Body (ECB).
/// - Treats the ECB as a convex kite (4 points) for casts against solids/platforms.
/// - Supports one-way platforms + drop-through timer.
/// - Optional moving-platform riding + momentum preservation.
/// 
/// Usage:
/// - Put this on a GameObject (no Rigidbody2D required).
/// - Call AddVelocity / SetVerticalVelocity / SetHorizontalVelocity from your controller.
/// - Move happens in Tick() called by Update().
/// 
/// Notes:
/// - "Kite" here is a symmetric diamond: top point, left/right mid points, bottom point.
/// - Width = ecbSize.x, Height = ecbSize.y, centered at ECBWorldCenter.
/// </summary>
public class ECBMotor2D_Kite : MonoBehaviour
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

    // Internal
    private const int MaxHits = 16;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[MaxHits];

    // A tiny "contact offset" we keep when stopping at a surface.
    private float ContactSkin => Mathf.Max(0.0005f, skin);

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void SetVerticalVelocity(float vy) => Velocity = new Vector2(Velocity.x, vy);
    public void AddVelocity(Vector2 dv) => Velocity += dv;

    public void SetHorizontalVelocity(float vx) => Velocity = new Vector2(vx, Velocity.y);

    /// <summary> Call when the player presses down+jump (or down+drop) to drop through one-way platforms. </summary>
    public void DropThrough()
    {
        // Only drop if we are actually standing on a one-way platform.
        if (!StandingOnPlatform) return;

        // Optional: don't allow dropping through "no drop" platforms.
        if (StandingOnNoDropPlatform) return;

        dropTimer = dropThroughTime;

        // Force leave-ground immediately
        Grounded = false;
        groundCollider = null;
        groundBody = null;

        // Optional: give a tiny downward nudge so you clear it instantly.
        if (Velocity.y > -2f) SetVerticalVelocity(-2f);
    }

    public void Tick(float dt)
    {
        if (dt <= 0f) return;

        // Update platform velocity from last frame groundBody position.
        PlatformVelocity = Vector2.zero;
        if (rideMovingPlatforms && groundBody != null)
        {
            Vector2 curPos = groundBody.position;
            PlatformVelocity = (curPos - lastGroundBodyPos) / Mathf.Max(dt, 0.0001f);
            lastGroundBodyPos = curPos;
        }

        // If we were grounded last frame and we just left ground, optionally preserve some platform momentum.
        if (preservePlatformMomentum && wasGroundedLastFrame && !Grounded)
        {
            Velocity += lastPlatformVelocity * preservedPerc;
        }

        // Gravity
        if (!DisableGravity)
        {
            float vy = Velocity.y - gravity * dt;
            if (vy < -maxFallSpeed) vy = -maxFallSpeed;
            Velocity = new Vector2(Velocity.x, vy);
        }

        if (dropTimer > 0f) dropTimer -= dt;

        // Ride platform by pre-applying platform delta (so collisions account for it).
        Vector2 platformDelta = Vector2.zero;
        if (rideMovingPlatforms && Grounded && groundBody != null)
        {
            platformDelta = PlatformVelocity * dt;
        }

        // Stick to ground a bit when grounded so we don't "hop" on slopes.
        if (Grounded && !DisableGravity && Velocity.y <= 0f)
        {
            Velocity = new Vector2(Velocity.x, -groundStickVelocity);
        }

        // Resolve movement in two phases: platform delta then own velocity.
        Vector2 pos = transform.position;

        if (platformDelta != Vector2.zero)
            MoveAndCollide(ref pos, platformDelta, isPlatformMove: true);

        Vector2 delta = Velocity * dt;
        MoveAndCollide(ref pos, delta, isPlatformMove: false);

        // Ground snap (after move) if falling/sliding very slightly above ground.
        if (Velocity.y <= 0f && dropTimer <= 0f)
        {
            RefreshGroundFromBottom(ref pos);
        }

        transform.position = pos;

        // Cache for next frame
        wasGroundedLastFrame = Grounded;
        lastPlatformVelocity = PlatformVelocity;
    }

    private void MoveAndCollide(ref Vector2 pos, Vector2 delta, bool isPlatformMove)
    {
        // Reset ground each move unless we find it again.
        bool groundedBefore = Grounded;
        if (!isPlatformMove)
        {
            Grounded = false;
            GroundNormal = Vector2.up;
            if (!groundedBefore)
            {
                groundCollider = null;
                groundBody = null;
            }
        }

        // Split into horizontal then vertical for simpler ground logic.
        // (Still using kite casts for each axis move.)
        if (delta.x != 0f)
        {
            Vector2 step = new Vector2(delta.x, 0f);
            ResolveAxis(ref pos, ref delta, step, axis: Axis.Horizontal, isPlatformMove: isPlatformMove);
        }

        if (delta.y != 0f)
        {
            Vector2 step = new Vector2(0f, delta.y);
            ResolveAxis(ref pos, ref delta, step, axis: Axis.Vertical, isPlatformMove: isPlatformMove);
        }
    }

    private enum Axis { Horizontal, Vertical }

    private void ResolveAxis(ref Vector2 pos, ref Vector2 fullDelta, Vector2 step, Axis axis, bool isPlatformMove)
    {
        float dist = step.magnitude;
        if (dist <= 0f) return;

        Vector2 dir = step / dist;
        // Cast a little farther to include skin, then stop short by skin.
        float castDist = dist + ContactSkin;

        int hitCount = KiteCast(pos, dir, castDist, out RaycastHit2D bestHit, axis);
        if (hitCount == 0)
        {
            pos += step;
            return;
        }

        // Move up to surface
        float moveDist = Mathf.Max(0f, bestHit.distance - ContactSkin);
        pos += dir * moveDist;

        // Respond to collision: remove velocity component into surface.
        Vector2 n = bestHit.normal;

        // Ground check: only when moving downward (or very small downward during slope stick),
        // and surface qualifies as ground.
        if (axis == Axis.Vertical && dir.y < 0f && n.y >= minGroundNormalY)
        {
            Grounded = true;
            GroundNormal = n;

            // Moving platform bookkeeping.
            groundCollider = bestHit.collider;
            groundBody = groundCollider != null ? groundCollider.attachedRigidbody : null;
            if (groundBody != null)
                lastGroundBodyPos = groundBody.position;

            // Zero out downward velocity.
            if (!isPlatformMove)
                Velocity = new Vector2(Velocity.x, 0f);

            // If using slope tangent, redirect horizontal velocity along slope.
            if (useSlopeTangent && !isPlatformMove)
            {
                // Tangent pointing "right-ish" along the surface.
                Vector2 tangent = new Vector2(n.y, -n.x).normalized;
                float vx = Velocity.x;
                Velocity = tangent * (vx / Mathf.Max(Mathf.Abs(tangent.x), 0.0001f));
                // Keep y from becoming positive due to numerical noise.
                if (Velocity.y > 0f) Velocity = new Vector2(Velocity.x, 0f);
            }
        }
        else
        {
            // Wall/ceiling: project velocity away from normal.
            if (!isPlatformMove)
            {
                // Remove component into the normal
                float into = Vector2.Dot(Velocity, n);
                if (into < 0f) Velocity -= into * n;

                // If we hit ceiling while moving up, clamp y to 0.
                if (axis == Axis.Vertical && dir.y > 0f)
                    Velocity = new Vector2(Velocity.x, Mathf.Min(Velocity.y, 0f));
            }
        }

        // Consume part of delta.
        if (axis == Axis.Horizontal) fullDelta.x = 0f;
        else fullDelta.y = 0f;
    }

    /// <summary>
    /// Casts the kite ECB from a given position in direction for distance, against solids + (conditionally) platforms.
    /// Returns count, and outputs a "best" hit (closest valid).
    /// </summary>
private int KiteCast(Vector2 pos, Vector2 dir, float distance, out RaycastHit2D bestHit, Axis axis)
{
    bestHit = default;

    bool dropping = dropTimer > 0f;

    // While dropping, we must not collide with regular platforms even if someone
    // accidentally included platform layers inside solidMask.
    LayerMask solids = dropping ? (solidMask & ~platformMask) : solidMask;

    // Platforms are only allowed when not dropping and moving downward.
    bool allowPlatforms = !dropping && (dir.y < 0f);

    LayerMask mask = solids | (allowPlatforms ? platformMask : 0);

    // Optional: if you want “no-drop” platforms to still block even while dropping,
    // ensure they’re always included.
    mask |= oneWayNoDropMask;
    if (mask.value == 0) return 0;

    Vector2 c = pos + ecbOffset;
    GetKiteWorldPoints(c, out Vector2 top, out Vector2 right, out Vector2 bottom, out Vector2 left);

    RaycastHit2D best = default;
    float bestDist = float.PositiveInfinity;
    bool found = false;

    // Helper inline via repeated code blocks (no local funcs)

    // ---- vertex rays ----
    RaycastHit2D hit;

    hit = Physics2D.Raycast(top, dir, distance, mask);
    if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }

    hit = Physics2D.Raycast(right, dir, distance, mask);
    if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }

    hit = Physics2D.Raycast(bottom, dir, distance, mask);
    if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }

    hit = Physics2D.Raycast(left, dir, distance, mask);
    if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }

    // ---- edge sampling (2 subdivisions) ----
    const int steps = 2;

    // top->right
    for (int i = 0; i <= steps; i++)
    {
        Vector2 p = Vector2.Lerp(top, right, i / (float)steps);
        hit = Physics2D.Raycast(p, dir, distance, mask);
        if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }
    }

    // right->bottom
    for (int i = 0; i <= steps; i++)
    {
        Vector2 p = Vector2.Lerp(right, bottom, i / (float)steps);
        hit = Physics2D.Raycast(p, dir, distance, mask);
        if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }
    }

    // bottom->left
    for (int i = 0; i <= steps; i++)
    {
        Vector2 p = Vector2.Lerp(bottom, left, i / (float)steps);
        hit = Physics2D.Raycast(p, dir, distance, mask);
        if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }
    }

    // left->top
    for (int i = 0; i <= steps; i++)
    {
        Vector2 p = Vector2.Lerp(left, top, i / (float)steps);
        hit = Physics2D.Raycast(p, dir, distance, mask);
        if (hit.collider && IsValidHit(hit, dir, axis) && hit.distance < bestDist) { best = hit; bestDist = hit.distance; found = true; }
    }

    if (!found) return 0;

    bestHit = best; // assign out param once, at end
    return 1;
}

    private bool IsValidHit(RaycastHit2D hit, Vector2 dir, Axis axis)
    {
        // One-way behavior:
        // - Platforms should only block when approaching from above (moving down),
        //   and when the hit normal points upward-ish.
        // - oneWayNoDropMask blocks even when dropping.
        int layerBit = 1 << hit.collider.gameObject.layer;

        bool isSolid = (solidMask.value & layerBit) != 0;
        if (isSolid) return true;

        bool isPlatform = (platformMask.value & layerBit) != 0;
        if (!isPlatform) return false;

        bool noDrop = (oneWayNoDropMask.value & layerBit) != 0;
        if (dropTimer > 0f && !noDrop) return false;

        // Must be moving downwards for a platform to block.
        if (dir.y >= 0f) return false;

        // Must be hitting the top face / upward-ish normal.
        if (hit.normal.y < minGroundNormalY) return false;

        // Also: if we're already below platform top, avoid "snag" while moving sideways.
        // We'll approximate by comparing ECB bottom to hit point.
        if (axis == Axis.Horizontal)
        {
            float ecbBottom = (ECBWorldCenter + Vector2.down * ECBHalfHeight).y;
            if (ecbBottom < hit.point.y - 0.01f) return false;
        }

        return true;
    }

    private void TryGroundSnap(ref Vector2 pos)
    {
        // Only snap if we are close to ground.
        Vector2 dir = Vector2.down;
        float dist = groundSnapDistance + ContactSkin;

        int hitCount = KiteCast(pos, dir, dist, out RaycastHit2D hit, Axis.Vertical);
        if (hitCount == 0) return;

        if (hit.normal.y < minGroundNormalY) return;

        // Snap down
        float moveDist = Mathf.Max(0f, hit.distance - ContactSkin);
        if (moveDist <= groundSnapDistance + 0.0001f)
        {
            pos += dir * moveDist;
            Grounded = true;
            GroundNormal = hit.normal;

            groundCollider = hit.collider;
            groundBody = groundCollider != null ? groundCollider.attachedRigidbody : null;
            if (groundBody != null)
                lastGroundBodyPos = groundBody.position;

            // Remove any residual downward velocity
            Velocity = new Vector2(Velocity.x, 0f);
        }
    }

    /// <summary>
    /// Creates a symmetric kite (diamond) around center c using ecbSize:
    /// top, right, bottom, left (clockwise).
    /// </summary>
    private void GetKiteWorldPoints(Vector2 c, out Vector2 top, out Vector2 right, out Vector2 bottom, out Vector2 left)
    {
        float hw = ECBHalfWidth;
        float hh = ECBHalfHeight;

        top = c + new Vector2(0f, +hh);
        bottom = c + new Vector2(0f, -hh);

        // Midpoints on left/right at center height:
        right = c + new Vector2(+hw, 0f);
        left = c + new Vector2(-hw, 0f);
    }
    public bool StandingOnPlatform
    {
        get
        {
            if (!Grounded || groundCollider == null) return false;
            int layerBit = 1 << groundCollider.gameObject.layer;
            return (platformMask.value & layerBit) != 0;
        }
    }

    public bool StandingOnNoDropPlatform
    {
        get
        {
            if (!Grounded || groundCollider == null) return false;
            int layerBit = 1 << groundCollider.gameObject.layer;
            return (oneWayNoDropMask.value & layerBit) != 0;
        }
    }
    private int GroundProbe(Vector2 pos, float distance, out RaycastHit2D bestHit)
    {
        bestHit = default;

        // During drop-through, ignore platforms completely (except no-drop if you want)
        bool dropping = dropTimer > 0f;

        // Same robustness: exclude regular platforms from solids while dropping.
        LayerMask solids = dropping ? (solidMask & ~platformMask) : solidMask;

        // Ground probe should see platforms only when not dropping.
        LayerMask mask = solids | (dropping ? 0 : platformMask);

        // Still include no-drop platforms even during drop.
        mask |= oneWayNoDropMask;
        
        Vector2 c = pos + ecbOffset;
        GetKiteWorldPoints(c, out Vector2 top, out Vector2 right, out Vector2 bottom, out Vector2 left);

        // Sample ONLY the bottom area (bottom tip + small width)
        // This is the important change: we do NOT sample the whole kite.
        float halfW = ECBHalfWidth;
        Vector2 p0 = bottom;                               // bottom tip
        Vector2 p1 = bottom + Vector2.left * (halfW * 0.6f);
        Vector2 p2 = bottom + Vector2.right * (halfW * 0.6f);

        bool found = false;
        float bestDist = float.PositiveInfinity;

        RaycastHit2D hit;

        hit = Physics2D.Raycast(p0, Vector2.down, distance, mask);
        if (IsValidGroundHit(hit) && hit.distance < bestDist) { bestHit = hit; bestDist = hit.distance; found = true; }

        hit = Physics2D.Raycast(p1, Vector2.down, distance, mask);
        if (IsValidGroundHit(hit) && hit.distance < bestDist) { bestHit = hit; bestDist = hit.distance; found = true; }

        hit = Physics2D.Raycast(p2, Vector2.down, distance, mask);
        if (IsValidGroundHit(hit) && hit.distance < bestDist) { bestHit = hit; bestDist = hit.distance; found = true; }

        return found ? 1 : 0;
    }

private bool IsValidGroundHit(RaycastHit2D hit)
{
    if (!hit.collider) return false;
    if (hit.normal.y < minGroundNormalY) return false;

    int layerBit = 1 << hit.collider.gameObject.layer;

    bool isSolid = (solidMask.value & layerBit) != 0;
    if (isSolid) return true;

    bool isPlatform = (platformMask.value & layerBit) != 0;
    if (!isPlatform) return false;

    // While dropping: ignore platforms (mask already handles this, but keep it safe)
    bool isNoDrop = (oneWayNoDropMask.value & layerBit) != 0;
    if (dropTimer > 0f && !isNoDrop) return false;

    return true;
}
private void RefreshGroundFromBottom(ref Vector2 pos)
{
    if (dropTimer > 0f) return;
    if (Velocity.y > 0f) return; // don't glue while rising

    float dist = groundSnapDistance + ContactSkin;

    if (GroundProbe(pos, dist, out RaycastHit2D hit) == 0)
        return;

    float moveDist = Mathf.Max(0f, hit.distance - ContactSkin);
    if (moveDist > groundSnapDistance + 0.0001f)
        return;

    pos += Vector2.down * moveDist;

    Grounded = true;
    GroundNormal = hit.normal;

    groundCollider = hit.collider;
    groundBody = groundCollider ? groundCollider.attachedRigidbody : null;
    if (groundBody != null) lastGroundBodyPos = groundBody.position;

    if (Velocity.y < 0f) Velocity = new Vector2(Velocity.x, 0f);
}
    private void RefreshGround(ref Vector2 pos)
    {
        // Probe a short distance down to keep Grounded stable even when not moving.
        Vector2 dir = Vector2.down;
        float dist = groundSnapDistance + ContactSkin;

        if (KiteCast(pos, dir, dist, out RaycastHit2D hit, Axis.Vertical) == 0)
            return;

        if (hit.normal.y < minGroundNormalY)
            return;

        // Only treat as ground if we're above the surface (prevents snapping up weirdly)
        float moveDist = Mathf.Max(0f, hit.distance - ContactSkin);
        if (moveDist > groundSnapDistance + 0.0001f)
            return;

        pos += dir * moveDist;

        Grounded = true;
        GroundNormal = hit.normal;

        groundCollider = hit.collider;
        groundBody = groundCollider != null ? groundCollider.attachedRigidbody : null;
        if (groundBody != null)
            lastGroundBodyPos = groundBody.position;

        // Kill residual downward velocity
        if (Velocity.y < 0f)
            Velocity = new Vector2(Velocity.x, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector2 c = (Application.isPlaying ? ECBWorldCenter : (Vector2)transform.position + ecbOffset);
        GetKiteWorldPoints(c, out Vector2 top, out Vector2 right, out Vector2 bottom, out Vector2 left);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);

        Gizmos.color = Grounded ? Color.green : Color.red;
        Gizmos.DrawSphere(ECBWorldBottom, 0.03f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(c, (Application.isPlaying ? (Vector3)Velocity : Vector3.zero) * 0.02f);
    }
}
