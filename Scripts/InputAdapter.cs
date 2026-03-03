using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class InputAdapter : MonoBehaviour
{
    [SerializeField] private ECBMotor2D motor;

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

        if (jumpCount < jumpAmount)
        {
            jumpVFX.Play();
            jumpVFX.transform.localPosition = transform.localPosition;
        }
        

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
        motor.AddVelocity(new Vector2(move.x * walkSpeed - motor.Velocity.x, 0f));

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

            // Consume buffer + consume coyote so you can’t use it twice
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;

            jumpCount = 1; // after a ground jump, you still have air jumps remaining if jumpAmount > 1
            return;
        }

        // Air jump(s)
        if (jumpCount < jumpAmount)
        {
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
        motor.SetVerticalVelocity(0f);
        motor.AddVelocity(dashDirection * dashSpeed - motor.Velocity);

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }
}

