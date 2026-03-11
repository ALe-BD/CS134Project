using UnityEngine;
using UnityEngine.InputSystem;

public class InputAdapter_Kite : MonoBehaviour
{
    [SerializeField] private ECBMotor2D_Kite motor;

    [Header("Jump")]
    [SerializeField] private int jumpAmount = 2;     // total jumps (1 = no double jump, 2 = double jump)
    [SerializeField] private float jumpSpeed = 14f;
    [SerializeField] private ParticleSystem jumpVFX;

    [Header("Timing")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.10f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 6.5f;
    private Vector2 move;

    [Header("Movement Acceleration")]
    [SerializeField] private float groundAcceleration = 80f;
    [SerializeField] private float airAcceleration = 40f;
    [SerializeField] private float groundDeceleration = 100f;
    [SerializeField] private float airDeceleration = 50f;
    [SerializeField] private float maxAirSpeedMultiplier = 1.0f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.25f;
    [SerializeField] private int maxAirDashes = 1;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private int dashCount;

    // Jump state
    private int airJumpUsed;                 // counts air jumps used (0..jumpAmount-1)
    private float lastGroundedTime = -999f;  // last time grounded
    private float lastJumpPressedTime = -999f;

    private int facingDirection = 1; // 1 = right, -1 = left

    public void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        lastJumpPressedTime = Time.time; // buffer
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (isDashing) return;
        if (dashCooldownTimer > 0f) return;

        // Limit air dashes
        if (!motor.Grounded && dashCount >= maxAirDashes)
            return;

        StartDash();
    }

    public void OnDrop(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        motor.DropThrough();
    }

    void Update()
    {
        if (motor == null) return;

        float dt = Time.deltaTime;

        // timers
        if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;

        // Track last grounded time (use current motor state from previous tick)
        var isGrounded = motor.Grounded;
        Debug.Log($"Grounded:{isGrounded}, Dashes:{maxAirDashes - dashCount}, Jumps:{jumpAmount - airJumpUsed}");
        if (motor.Grounded)
        {
            lastGroundedTime = Time.time;
            airJumpUsed = 0;
            dashCount = 0;

            // optional: cancel dash on landing
            if (isDashing) isDashing = false;
        }

        // Try to consume buffered jump BEFORE we possibly dash or change horizontal.
        TryConsumeBufferedJump();

        if (isDashing)
        {
            TickDash(dt);
            return; // skip normal movement while dashing
        }

        TickHorizontal(dt);
        UpdateFacing();
    }

    private void TickHorizontal(float dt)
    {
        bool grounded = motor.Grounded;

        float accel = grounded ? groundAcceleration : airAcceleration;
        float decel = grounded ? groundDeceleration : airDeceleration;

        float maxSpeed = grounded ? walkSpeed : (walkSpeed * maxAirSpeedMultiplier);

        float targetVelocityX = move.x * maxSpeed;
        float currentVelocityX = motor.Velocity.x;

        float newVelocityX;
        if (Mathf.Abs(move.x) > 0.01f)
        {
            newVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, accel * dt);
        }
        else
        {
            newVelocityX = Mathf.MoveTowards(currentVelocityX, 0f, decel * dt);
        }

        // Apply only the delta on X
        motor.AddVelocity(new Vector2(newVelocityX - currentVelocityX, 0f));
    }

    private void UpdateFacing()
    {
        if (move.x > 0.1f) facingDirection = 1;
        else if (move.x < -0.1f) facingDirection = -1;
    }

    private void TryConsumeBufferedJump()
    {
        // still within buffer?
        if ((Time.time - lastJumpPressedTime) > jumpBufferTime)
            return;

        bool groundedNow = motor.Grounded;
        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;

        // Ground/coyote jump
        if (groundedNow || canCoyote)
        {
            motor.SetVerticalVelocity(jumpSpeed);
            

            // consume buffer and coyote so it can't be reused
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;

            return;
        }

        // Air jump (jumpAmount includes the ground jump; so air jumps allowed = jumpAmount - 1)
        int maxAirJumps = Mathf.Max(0, jumpAmount - 1);
        if (airJumpUsed < maxAirJumps)
        {
            if (jumpVFX != null)
            {
                jumpVFX.transform.position = transform.position;
                jumpVFX.Play();
            }

            motor.SetVerticalVelocity(jumpSpeed);
            airJumpUsed++;

            lastJumpPressedTime = -999f;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (!motor.Grounded)
            dashCount++;

        if (move.magnitude > 0.1f) dashDirection = move.normalized;
        else dashDirection = new Vector2(facingDirection, 0f);
    }

    private void TickDash(float dt)
    {
        dashTimer -= dt;

        // Set velocity directly for consistent dash feel.
        SetVelocity(dashDirection * dashSpeed);

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }

    // Local helper so we don't need to modify your motor API.
    private void SetVelocity(Vector2 v)
    {
        motor.AddVelocity(v - motor.Velocity);
    }
}