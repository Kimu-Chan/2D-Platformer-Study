using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemyController : MonoBehaviour
{
    private enum State
    {
        Moving,
        Knockback,
        Attacking,
        Dead
    }
    
    private State currentState;

    [SerializeField] private float groundCheckDistance, wallCheckDistance;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float maxHealth;
    [SerializeField] private float knockbackDuration;
    [SerializeField] private float lastTouchDamageTime;
    [SerializeField] private float touchDamageCooldown;
    [SerializeField] private float touchDamage;
    [SerializeField] private float touchDamageWidth, touchDamageHeight;
    [SerializeField] private Transform groundCheck, wallCheck;
    [SerializeField] private Transform touchDamageCheck;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector2 knockbackSpeed;
    [SerializeField] private GameObject hitParticle,
                                        deathChunkParticle,
                                        deathBloodParticle;
    
    private float currentHealth;
    private float knockbackStartTime;
    
    // 0: damage, 1: knockback Direction
    private float[] attackDetails = new float[2];
    
    private int facingDirection;
    private int damageDirection;
    
    private Vector2 movement;
    private Vector2 touchDamageBotLeft, touchDamageTopRight;
    
    private bool groundDetected, wallDetected;

    private GameObject alive;
    private Rigidbody2D aliveRb;
    private Animator aliveAnim;
    
    private static readonly int Knockback = Animator.StringToHash("Knockback");

    private void Start()
    {
        alive = transform.Find("Alive").gameObject;
        aliveRb = alive.GetComponent<Rigidbody2D>();
        aliveAnim = alive.GetComponent<Animator>();
        
        facingDirection = 1;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Moving:
                UpdateMovingState();
                break;
            case State.Knockback:
                UpdateKnockbackState();
                break;
            case State.Attacking:
                UpdateAttackingState();
                break;
            case State.Dead:
                UpdateDeadState();
                break;
        }
    }

#region State Functions

    #region MOVING STATE

    private void EnterMovingState()
    {
        
    }
    
    private void UpdateMovingState()
    {
        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        wallDetected = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        
        CheckTouchDamage();
        
        if (!groundDetected || wallDetected)
        {
            Flip();
        }
        else
        {
            movement.Set(movementSpeed * facingDirection, aliveRb.velocity.y);
            aliveRb.velocity = movement;
        }
    }
    
    private void ExitMovingState()
    {
        
    }

    #endregion

    #region KNOCKBACK STATE

    // Knockback STATE
    private void EnterKnockbackState()
    {
        knockbackStartTime = Time.time;
        movement.Set(knockbackSpeed.x * damageDirection, knockbackSpeed.y);
        aliveRb.velocity = movement;
        
        aliveAnim.SetBool(Knockback, true);
    }
    
    private void UpdateKnockbackState()
    {
        if (Time.time >= knockbackStartTime + knockbackDuration)
        {
            SwitchState(State.Moving);
        }
    }
    
    private void ExitKnockbackState()
    {
        aliveAnim.SetBool(Knockback, false);
    }

    #endregion

    #region ATTACKING STATE

    // Attacking STATE
    private void EnterAttackingState()
    {
        
    }
    
    private void UpdateAttackingState()
    {
        
    }
    
    private void ExitAttackingState()
    {
        
    }

    #endregion

    #region DEAD STATE

    // Dead STATE
    private void EnterDeadState()
    {
        // 파괴 효과
        Instantiate(deathChunkParticle, alive.transform.position, deathChunkParticle.transform.rotation);
        Destroy(gameObject);
    }
    
    private void UpdateDeadState()
    {
        
    }
    
    private void ExitDeadState()
    {
        
    }

    #endregion
    

#endregion
    
    
    // Other Functions
    
    // attackDetails = 0: damage, 1: knockback Direction
    private void Damage(float[] attackDetails)
    {
        currentHealth -= attackDetails[0];

        Instantiate(hitParticle, alive.transform.position, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        
        if(attackDetails[1] > alive.transform.position.x)
        {
            damageDirection = -1;
        }
        else
        {
            damageDirection = 1;
        }

        switch (currentHealth)
        {
            //Hit particle
            case > 0.0f:
                SwitchState(State.Knockback);
                break;
            case <= 0.0f:
                SwitchState(State.Dead);
                break;
        }
    }

    private void CheckTouchDamage()
    {
        if (Time.time >= lastTouchDamageTime + touchDamageCooldown)
        {
            var touchPosition = touchDamageCheck.position;
            touchDamageBotLeft.Set(touchPosition.x - touchDamageWidth / 2f, touchPosition.y - touchDamageHeight / 2f);
            touchDamageTopRight.Set(touchPosition.x + touchDamageWidth / 2f, touchPosition.y + touchDamageHeight / 2f);
            
            Collider2D hit = Physics2D.OverlapArea(touchDamageBotLeft, touchDamageTopRight, whatIsPlayer);
            if (hit != null)
            {
                lastTouchDamageTime = Time.time;
                attackDetails[0] = touchDamage;
                attackDetails[1] = alive.transform.position.x;
                hit.SendMessage("Damage", attackDetails);
            }
        }
    }
    
    private void Flip()
    {
        facingDirection *= -1;
        alive.transform.Rotate(0f, 180f, 0f);
    }

    private void SwitchState(State state)
    {
        switch (currentState)
        {
            case State.Moving:
                ExitMovingState();
                break;
            case State.Knockback:
                ExitKnockbackState();
                break;
            case State.Attacking:
                ExitAttackingState();
                break;
            case State.Dead:
                ExitDeadState();
                break;
        }

        switch (state)
        {
            case State.Moving:
                EnterMovingState();
                break;
            case State.Knockback:
                EnterKnockbackState();
                break;
            case State.Attacking:
                EnterAttackingState();
                break;
            case State.Dead:
                EnterDeadState();
                break;
        }
        
        currentState = state;
    }

    private void OnDrawGizmos()
    {
        var groundCheckPosition = groundCheck.position;
        Gizmos.DrawLine(groundCheckPosition, new Vector3(groundCheckPosition.x, groundCheckPosition.y - groundCheckDistance, groundCheckPosition.z));
        var wallCheckPosition = wallCheck.position;
        Gizmos.DrawLine(wallCheckPosition, new Vector3(wallCheckPosition.x + wallCheckDistance, wallCheckPosition.y, wallCheckPosition.z));

        var touchPosition = touchDamageCheck.position;
        Vector2 botLeft = new Vector2(touchPosition.x - touchDamageWidth / 2f, touchPosition.y - touchDamageHeight / 2f);
        Vector2 botRight = new Vector2(touchPosition.x + touchDamageWidth / 2f, touchPosition.y - touchDamageHeight / 2f);
        Vector2 topLeft = new Vector2(touchPosition.x - touchDamageWidth / 2f, touchPosition.y + touchDamageHeight / 2f);
        Vector2 topRight = new Vector2(touchPosition.x + touchDamageWidth / 2f, touchPosition.y + touchDamageHeight / 2f);
        
        Gizmos.DrawLine(botLeft, botRight);
        Gizmos.DrawLine(botRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, botLeft);
    }
}
