using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using TMPro;

public class InputAdapter : MonoBehaviour
{
    [SerializeField] private ECBMotor2D1 motor;

    [Header("Jump")]
    [SerializeField] private int jumpAmount = 2;     // total jumps (1 = no double jump, 2 = double jump)
    [SerializeField] private float jumpSpeed = 14f;
    [SerializeField] private ParticleSystem jumpVFX;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private AudioSource source;

    private bool wasGrounded;

    [Header("Timing")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.10f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 6.5f;
    [SerializeField] private float runSpeed = 9.5f;
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

    [Header("Sprite")]
    [SerializeField] private GameObject mainSprite;

    [Header("UI")]
    [SerializeField] private HealthManager Health;
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject ScreenOut;

    private SceneFader screenFade;
    private TextMeshProUGUI scoreUI;
    private Coroutine healthRoutine;
    private bool isHoldingUIBtn = false;
    public bool isInteracting = false;
    public int score = 0;

    [Header("Spawn Point")]
    [SerializeField] private Transform SpawnPoint;
    private bool hasRespawned = false;


    private Animator _animator;

    // animation IDs
	private int _animIDSpeed;
	private int _animIDGrounded;
	private int _animIDJump;
	private int _animIDFreeFall;
    private int _animIDDashing;
    private bool _hasAnimator;

    // dash variables
    private bool isDashing;
    private bool dashHeld;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private int dashCount;

    // jump variables
    private int jumpCount = 1;                 // 1 means "used ground jump already?" (see resets below)
    private float lastGroundedTime = -999f;    // time we were last grounded
    private float lastJumpPressedTime = -999f; // time jump was last pressed (buffer)
    private int facingDirection = 1; // 1 = right, -1 = left
    private SpriteRenderer spriteRenderer;
    private SpriteSpawner ghost;

    public void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        
        if (jumpCount < jumpAmount)
        {
            _animator.Play("Base Layer.PlayerJumpStart");
        }
        
        // Buffer the jump press; we'll attempt to consume it in Update after motor ticks.
        lastJumpPressedTime = Time.time;
    }
    public void OnDash(InputAction.CallbackContext ctx)
    {
        // Hold-to-run state
        if (ctx.started)
            dashHeld = true;
        else if (ctx.canceled)
            dashHeld = false;

        if (!ctx.performed) return;

        if (dashCount < maxAirDashes)
        {
            _animator.Play("Base Layer.PlayerDash");
        }
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

    //Check on Health without having it on screen the whole time
    public void OnUIVisibility(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isHoldingUIBtn = true;
            Health.StartFadeIn();

            if (healthRoutine != null)
            {
                StopCoroutine(healthRoutine);
                healthRoutine = null;
            }
        }
        else if (ctx.canceled)
        {
            isHoldingUIBtn = false;

            if (healthRoutine != null)
                StopCoroutine(healthRoutine);

            healthRoutine = StartCoroutine(FadeOutAfterDelay(2f));
        }
    }

    public void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isInteracting = true;
        }
        else if (ctx.canceled)
        {
            isInteracting = false;
        }

    }

    void Start()
    {
        if(mainSprite == null)
        {
            mainSprite = transform.Find("Body").gameObject;
        }

        SpawnPoint = GameObject.Find("SpawnPoint").transform; //Please don't use the term "SpawnPoint" in any gameObject
        source = GetComponent<AudioSource>();
        spriteRenderer = mainSprite.GetComponent<SpriteRenderer>();  
        _animator = mainSprite.GetComponent<Animator>();
        ghost = mainSprite.GetComponent<SpriteSpawner>();
        Health = GetComponent<HealthManager>();
        UI = transform.parent.Find("UI").gameObject;
        scoreUI = UI.transform.Find("Score").GetComponent<TextMeshProUGUI>();
        ScreenOut = GameObject.Find("SceneFader");
        screenFade = ScreenOut.GetComponent<SceneFader>();

        ghost.enabled = false;
        AssignAnimationIDs();
    }

    void Update()
    {
        //Death Cheak
        if(!hasRespawned && Health.RemovedSegments >= Health.SegmentCount)
        {
            hasRespawned = true;
            StartCoroutine(Respawn());
        }
        //Update Score
        scoreUI.text = score.ToString("D4");

        _hasAnimator = _animator != null;
        // Update timers
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        _animator.SetBool(_animIDDashing, isDashing);
        if (isDashing)
        {
            Dash();
            return; // skip normal movement while dashing
        }

        // Determine if grounded
        bool isGrounded = motor.Grounded;

        //Disables dash effect if player becomes grounded
        if (isGrounded && !wasGrounded)
        {
            ghost.enabled = false;
            source.PlayOneShot(landingSound);
        }

        wasGrounded = isGrounded;

        HoriDriver(isGrounded);
        UpdateFacing();

        _animator.SetBool(_animIDGrounded, isGrounded);
        _animator.SetBool(_animIDFreeFall, !isGrounded);

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

    private void AssignAnimationIDs()
	{
		_animIDSpeed = Animator.StringToHash("Speed");
		_animIDGrounded = Animator.StringToHash("Grounded");
		_animIDJump = Animator.StringToHash("Jump");
		_animIDFreeFall = Animator.StringToHash("Freefall");
        _animIDDashing = Animator.StringToHash("Dashing");
	}

    private void HoriDriver(bool isGrounded)
    {
        // Horizontal drive

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
        // Hold dash to run, but ONLY on ground.
        float maxSpeed = (isGrounded && dashHeld) ? runSpeed : walkSpeed;

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

        if (_hasAnimator)
		{
            _animator.SetFloat(_animIDSpeed, Mathf.Abs(motor.Velocity.x));
		}

    }
    private void UpdateFacing()
    {
        if (move.x > 0.1f)
        {
            facingDirection = 1;
            spriteRenderer.flipX = !enabled;
        }
        else if (move.x < -0.1f)
        {
            facingDirection = -1;
            spriteRenderer.flipX = enabled;
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
            jumpVFX.transform.position = transform.position;
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
        ghost.enabled = true;

        if (move != Vector2.zero)
        {
            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            Debug.Log("angle: " + angle);

            if (angle > 90f || angle < -90f)
            {
                mainSprite.transform.eulerAngles = new Vector3(0f, 0f, angle + 180f);
                Debug.Log("ping1");
            }
            else
            {
                if(!Mathf.Approximately(angle, 90f) || !Mathf.Approximately(angle, -90f))
                {
                    mainSprite.transform.eulerAngles = new Vector3(0f, 0f, angle);
                    Debug.Log("ping2_1");
                }
                if((Mathf.Approximately(angle, 90f) || Mathf.Approximately(angle, -90f)) && facingDirection == -1)
                {
                    mainSprite.transform.eulerAngles = new Vector3(0f, 0f, angle + 180f);
                    Debug.Log("ping2_2");
                }
                
                
            }
        }

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
            mainSprite.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            isDashing = false;
        }
    }

    private IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isHoldingUIBtn)
            Health.StartFadeOut();

        healthRoutine = null;
    }

    private IEnumerator Respawn()
    {
        // Fade to black
        yield return StartCoroutine(screenFade.Fade(0f, 1f));

        // Respawn logic
        transform.position = SpawnPoint.position;
        motor.Velocity = Vector3.zero;
        Health.Healed(Mathf.RoundToInt(Health.SegmentCount));

        // Optional: wait one frame so position/UI fully update before fading back in
        yield return null;

        // Fade back from black
        yield return StartCoroutine(screenFade.Fade(1f, 0f));

        hasRespawned = false;
    }
}

