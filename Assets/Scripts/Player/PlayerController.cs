using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf
// ReSharper disable CompareOfFloatsByEqualityOperator

public class PlayerController : MonoBehaviour
{

    private float movementInputDirection;
    // 조작감 개선. Jump 인풋 들어올 시 Set. 점프 액션 수행 시, 0으로 초기화
    private float jumpTimer;
    private float turnTimer;
    private float wallJumpTimer;
    private float dashTimeLeft;
    private float lastImageXpos;
    private float lastDash = -100f;
    private float knockbackStartTime;
    [SerializeField] private float knockbackDuration;

    private int amountOfJumpsLeft;
    private int facingDirection = 1;
    private int lastWallJumpDirection;

    private bool isFacingRight = true;
    private bool isWalking;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool canNormalJump;
    private bool canWallJump;
    private bool isAttemptingToJump;
    private bool checkJumpMultiplier;
    private bool canMove;
    private bool canFlip;
    private bool hasWallJumped;
    private bool isTouchingLedge;
    private bool canClimbLedge = false;
    private bool ledgeDetected;
    private bool isDashing;
    private bool knockback;
    
    [SerializeField] private Vector2 knockbackSpeed;

    private Vector2 ledgePosBot;
    private Vector2 ledgePos1;
    private Vector2 ledgePos2;

    private Rigidbody2D rb;
    private Animator anim;

    public int amountOfJumps = 1;

    public float movementSpeed = 10.0f;
    public float jumpForce = 16.0f;
    public float groundCheckRadius;
    public float wallCheckDistance;
    public float wallSlideSpeed;
    public float movementForceInAir;
    public float airDragMultiplier = 0.95f;
    public float variableJumpHeightMultiplier = 0.5f;
    public float wallHopForce;
    public float wallJumpForce;
    public float jumpTimerSet = 0.15f;
    public float turnTimerSet = 0.1f;
    public float wallJumpTimerSet = 0.5f;

    public float ledgeClimbXOffset1 = 0f;
    public float ledgeClimbYOffset1 = 0f;
    public float ledgeClimbXOffset2 = 0f;
    public float ledgeClimbYOffset2 = 0f;
    public float dashTime;
    public float dashSpeed;
    public float distanceBetweenImages;
    public float dashCoolDown;

    public Vector2 wallHopDirection;
    public Vector2 wallJumpDirection;

    public Transform groundCheck;
    public Transform wallCheck;
    public Transform ledgeCheck;

    public LayerMask whatIsGround;
    
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");
    private static readonly int IsWallSliding = Animator.StringToHash("isWallSliding");
    private static readonly int CanClimbLedge = Animator.StringToHash("canClimbLedge");
    private static readonly int XVelocity = Animator.StringToHash("xVelocity");

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        amountOfJumpsLeft = amountOfJumps;
        wallHopDirection.Normalize();
        wallJumpDirection.Normalize();
    }

    // Update is called once per frame
    private void Update()
    {
        // 플레이어 입력 확인, 해당되는 Action 호출
        CheckInput();
        // 이동하려는 방향과 바라보는 방향이 다르다면, Flip(). 이동중 조건 체크 (isWalking)
        CheckMovementDirection();
        UpdateAnimations();
        CheckIfCanJump();
        CheckIfWallSliding();
        CheckJump();
        CheckLedgeClimbing();
        CheckDash();
        CheckKnockback();
    }

    private void FixedUpdate()
    {
        // 이동 적용
        ApplyMovement();
        // 접촉 조건 체크 (isGrounded, isTouchingWall)
        CheckSurroundings();
    }

    private void CheckIfWallSliding()
    {
        if (isTouchingWall && movementInputDirection == facingDirection && rb.velocity.y < 0 && !canClimbLedge)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }
    
    public bool GetDashStatus()
    {
        return isDashing;
    }

    public void Knockback(int direction)
    {
        knockback = true;
        knockbackStartTime = Time.time;
        rb.velocity = new Vector2(knockbackSpeed.x * direction, knockbackSpeed.y);
    }

    private void CheckKnockback()
    {
        if(Time.time >= knockbackStartTime + knockbackDuration && knockback)
        {
            knockback = false;
            rb.velocity = new Vector2(0.0f, rb.velocity.y);
        }
    }

    private void CheckLedgeClimbing()
    {
        if (ledgeDetected && !canClimbLedge)
        {
            canClimbLedge = true;
            
            if(isFacingRight)
            {
                ledgePos1 = new Vector2(Mathf.Floor(ledgePosBot.x + wallCheckDistance) - ledgeClimbXOffset1,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset1);
                ledgePos2 = new Vector2(Mathf.Floor(ledgePosBot.x + wallCheckDistance) + ledgeClimbXOffset2,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset2);
            }
            else
            {
                ledgePos1 = new Vector2(Mathf.Ceil(ledgePosBot.x - wallCheckDistance) + ledgeClimbXOffset1,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset1);
                ledgePos2 = new Vector2(Mathf.Ceil(ledgePosBot.x - wallCheckDistance) - ledgeClimbXOffset2,
                    Mathf.Floor(ledgePosBot.y) + ledgeClimbYOffset2);
            }

            canMove = false;
            canFlip = false;
            
            anim.SetBool(CanClimbLedge, canClimbLedge);
        }

        if (canClimbLedge)
        {
            transform.position = ledgePos1;
        }
    }

    public void FinishLedgeCllimb()
    {
        canClimbLedge = false;
        transform.position = ledgePos2;
        canMove = true;
        canFlip = true;
        ledgeDetected = false;
        anim.SetBool(CanClimbLedge, canClimbLedge);
    }

    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        isTouchingLedge = Physics2D.Raycast(ledgeCheck.position, transform.right, wallCheckDistance, whatIsGround);

        if (isTouchingWall && !isTouchingLedge && !ledgeDetected)
        {
            ledgeDetected = true;
            ledgePosBot = wallCheck.position;
        }
    }

    private void CheckIfCanJump()
    {
        if(isGrounded && rb.velocity.y <= 0.01f)
        {
            amountOfJumpsLeft = amountOfJumps;
        }

        if (isTouchingWall)
        {
            checkJumpMultiplier = false;
            canWallJump = true;
        }

        if(amountOfJumpsLeft <= 0)
        {
            canNormalJump = false;
        }
        else
        {
            canNormalJump = true;
        }
      
    }

    private void CheckMovementDirection()
    {
        if(isFacingRight && movementInputDirection < 0)
        {
            Flip();
        }
        else if(!isFacingRight && movementInputDirection > 0)
        {
            Flip();
        }
        
        // 이동중 조건 체크 (isWalking)
        isWalking = Mathf.Abs(rb.velocity.x) > 0.01f;
    }

    private void UpdateAnimations()
    {
        anim.SetBool(IsWalking, isWalking);
        anim.SetBool(IsGrounded, isGrounded);
        anim.SetFloat(YVelocity, rb.velocity.y);
        anim.SetFloat(XVelocity, rb.velocity.x);
        anim.SetBool(IsWallSliding, isWallSliding);
    }

    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            if(isGrounded || (amountOfJumpsLeft > 0 && isTouchingWall))
            {
                NormalJump();
            }
            else
            {
                // 조작감 개선 (약간의 딜레이 허용)
                jumpTimer = jumpTimerSet;
                isAttemptingToJump = true;
            }
        }

        if(Input.GetButtonDown("Horizontal") && isTouchingWall)
        {
            if(!isGrounded && movementInputDirection != facingDirection)
            {
                canMove = false;
                canFlip = false;

                turnTimer = turnTimerSet;
            }
        }

        if (turnTimer >= 0)
        {
            turnTimer -= Time.deltaTime;

            if(turnTimer <= 0)
            {
                canMove = true;
                canFlip = true;
            }
        }

        if (checkJumpMultiplier && !Input.GetButton("Jump"))
        {
            checkJumpMultiplier = false;
            var velocity = rb.velocity;
            velocity = new Vector2(velocity.x, velocity.y * variableJumpHeightMultiplier);
            rb.velocity = velocity;
        }

        if (Input.GetButtonDown("Dash"))
        {
            if (Time.time >= (lastDash + dashCoolDown))
            {
                AttemptToDash();
            }
        }
    }

    private void AttemptToDash()
    {
        isDashing = true;
        dashTimeLeft = dashTime;
        lastDash = Time.time;

        PlayerAfterImagePool.Instance.GetFromPool();
        lastImageXpos = transform.position.x;
    }

    public int GetFacingDirection()
    {
        return facingDirection;
    }

    private void CheckDash()
    {
        if (isDashing)
        {
            if (dashTimeLeft > 0)
            {
                canMove = false;
                canFlip = false;
                // 대쉬 중 떨어지길 원하지 않으면 vel.y를 0으로 설정
                rb.velocity = new Vector2(dashSpeed * facingDirection, 0);//rb.velocity.y);
                dashTimeLeft -= Time.deltaTime;

                if (Mathf.Abs(transform.position.x - lastImageXpos) > distanceBetweenImages)
                {
                    PlayerAfterImagePool.Instance.GetFromPool();
                    lastImageXpos = transform.position.x;
                }
            }

            if (dashTimeLeft <= 0 || isTouchingWall)
            {
                isDashing = false;
                canMove = true;
                canFlip = true;
            }
        }
    }

    private void CheckJump()
    {
        if(jumpTimer > 0)
        {
            //WallJump
            if(!isGrounded && isTouchingWall && movementInputDirection != 0 && movementInputDirection != facingDirection)
            {
                WallJump();
            }
            /* 입력 방지 function 필요
            else if (isWallSliding && movementInputDirection == facingDirection)
            {
                WallHop();
            }*/
            else if (isGrounded)
            {
                NormalJump();
            }
        }
       
        if(isAttemptingToJump)
        {
            jumpTimer -= Time.deltaTime;
        }

        if(wallJumpTimer > 0)
        {
            if(hasWallJumped && movementInputDirection == -lastWallJumpDirection)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0.0f);
                hasWallJumped = false;
            }else if(wallJumpTimer <= 0)
            {
                hasWallJumped = false;
            }
            else
            {
                wallJumpTimer -= Time.deltaTime;
            }
        }
    }

    private void NormalJump()
    {
        if (canNormalJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            amountOfJumpsLeft--;
            jumpTimer = 0;
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
        }
    }

    private void WallJump()
    {
        if (canWallJump)
        {
            // 점프 높이 일정하게 뛰기 위해 vel.y 초기화
            rb.velocity = new Vector2(rb.velocity.x, 0.0f);
            isWallSliding = false;
            amountOfJumpsLeft = amountOfJumps;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * movementInputDirection, wallJumpForce * wallJumpDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
            jumpTimer = 0;
            isAttemptingToJump = false;
            
            checkJumpMultiplier = true;
            turnTimer = 0;
            canMove = true;
            canFlip = true;
            hasWallJumped = true;
            wallJumpTimer = wallJumpTimerSet;
            lastWallJumpDirection = -facingDirection;
        }
    }

    private void WallHop()
    {
        if (canWallJump)    //canJump로 수정해야함
        {
            isWallSliding = false;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallHopForce * wallHopDirection.x * -facingDirection,
                wallHopForce * wallHopDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
            jumpTimer = 0;
            isAttemptingToJump = false;
            
            turnTimer = 0;
            canMove = true;
            canFlip = true;
            hasWallJumped = true;
            wallJumpTimer = wallJumpTimerSet;
            lastWallJumpDirection = -facingDirection;
        }
    }

    private void ApplyMovement()
    {

        if (!isGrounded && !isWallSliding && movementInputDirection == 0 && !knockback)
        {
            var velocity = rb.velocity;
            velocity = new Vector2(velocity.x * airDragMultiplier, velocity.y);
            rb.velocity = velocity;
        }
        else if(canMove && !knockback)
        {
            rb.velocity = new Vector2(movementSpeed * movementInputDirection, rb.velocity.y);
        }
        

        if (isWallSliding)
        {
            if(rb.velocity.y < -wallSlideSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }
    }

    public void DisableFlip()
    {
        canFlip = false;
    }

    public void EnableFlip()
    {
        canFlip = true;
    }
    
    private void Flip()
    {
        if (!isWallSliding && canFlip && !knockback)
        {
            facingDirection *= -1;
            isFacingRight = !isFacingRight;
            transform.Rotate(0.0f, 180.0f, 0.0f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        var wallCheckPosition = wallCheck.position;
        Gizmos.DrawLine(wallCheckPosition, new Vector3(wallCheckPosition.x + wallCheckDistance, wallCheckPosition.y, wallCheckPosition.z));
        var ledgeCheckPosition = ledgeCheck.position;
        Gizmos.DrawLine(ledgeCheckPosition, new Vector3(ledgeCheckPosition.x + wallCheckDistance, ledgeCheckPosition.y, ledgeCheckPosition.z));
    }
}
