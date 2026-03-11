using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class InputAdapter : MonoBehaviour
{
    [SerializeField] private ECBMotor2D1 motor;

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

    private int jumpCount = 1;                 // 1 means "used ground jump already?" (see resets below)
    private float lastGroundedTime = -999f;    // time we were last grounded
    private float lastJumpPressedTime = -999f; // time jump was last pressed (buffer)
    private int facingDirection = 1; // 1 = right, -1 = left

    public void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Buffer the jump press; we'll attempt to consume it in Update after motor ticks.
        lastJumpPressedTime = Time.time;
    }
    public void OnDash(InputAction.CallbackContext ctx)
    {

        if (!ctx.performed) return;
            Debug.Log("Dashed");

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
        motor.RequestDropThrough();
    }

    void Update()
    {

        // Update timers
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            Dash();
            return; // skip normal movement while dashing
        }

        // Horizontal drive

        // Determine if grounded
        bool isGrounded = motor.Grounded;

        // Choose acceleration and deceleration
        float acceleration;
        float deceleration;

        if (isGrounded)
        {
            acceleration = groundAcceleration;
            deceleration = groundDeceleration;
        }
        else
        {
            acceleration = airAcceleration;
            deceleration = airDeceleration;
        }

        // Determine max allowed speed
        float maxSpeed = walkSpeed;

        if (!isGrounded)
        {
            maxSpeed = walkSpeed * maxAirSpeedMultiplier;
        }

        // Desired velocity based on input
        float targetVelocityX = move.x * maxSpeed;

        // Current velocity
        float currentVelocityX = motor.Velocity.x;

        float newVelocityX;

        // If there is input, accelerate toward target
        if (Mathf.Abs(move.x) > 0.01f)
        {
            newVelocityX = Mathf.MoveTowards(
                currentVelocityX,
                targetVelocityX,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // No input → slow down toward 0
            newVelocityX = Mathf.MoveTowards(
                currentVelocityX,
                0f,
                deceleration * Time.deltaTime
            );
        }

        // Apply velocity change
        float velocityChange = newVelocityX - currentVelocityX;
        motor.AddVelocity(new Vector2(velocityChange, 0f));

        if (move.x > 0.1f) facingDirection = 1;
        else if (move.x < -0.1f) facingDirection = -1;
    }

    void LateUpdate()
    {
        Jump();
        // Update grounded timestamp AFTER motor.Update() has run this frame.
        if (motor.Grounded)
        {
            lastGroundedTime = Time.time;
            jumpCount = 1; // reset air-jump state when on ground
            dashCount = 0;
        }
    }

    private void Jump()
    {
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        if (!hasBufferedJump) return;

        bool groundedNow = motor.Grounded;
        bool canCoyoteJump = (Time.time - lastGroundedTime) <= coyoteTime;

        // Ground jump (or coyote)
        if (groundedNow || canCoyoteJump)
        {
            motor.SetVerticalVelocity(jumpSpeed);
            motor.InheritPlatformVelocityOnce();
            motor.SetVerticalVelocity(jumpSpeed);

            // Consume buffer + consume coyote so you can’t use it twice
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;

            jumpCount = 1; // after a ground jump, you still have air jumps remaining if jumpAmount > 1
            return;
        }

        // Air jump(s)
        if (jumpCount < jumpAmount)
        {
            jumpVFX.Play();
            jumpVFX.transform.localPosition = transform.localPosition;
            motor.SetVerticalVelocity(jumpSpeed);
            jumpCount++;

            // Consume buffer
            lastJumpPressedTime = -999f;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        if (!motor.Grounded)
        {
            dashCount++;  
        }
        
        // Use stick direction if present
        if (move.magnitude > 0.1f)
            dashDirection = move.normalized;
        else
            dashDirection = new Vector2(facingDirection, 0f);
    }

    private void Dash()
    {
        dashTimer -= Time.deltaTime;

        // Override velocity completely during dash
        //motor.SetVerticalVelocity(0f);
        motor.AddVelocity(dashDirection * dashSpeed - motor.Velocity);

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }
}

