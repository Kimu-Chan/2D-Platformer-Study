using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private bool combatEnabled;
    [SerializeField] private float inputTimer;
    [SerializeField] private float attack1Radius;
    [SerializeField] private float attack1Damage;
    [SerializeField] private Transform attack1HitBoxPos;
    [SerializeField] private LayerMask whatIsDamageable;
    
    private bool gotInput;
    private bool isAttacking;
    private bool isFirstAttack;

    private float lastInputTime = Mathf.NegativeInfinity;
    
    // 0: damage, 1: knockback Direction
    private float[] attackDetails = new float[2];

    private Animator anim;

    private PlayerController PC;
    private PlayerStats PS;
    
    private static readonly int CanAttack = Animator.StringToHash("canAttack");
    private static readonly int Attack1 = Animator.StringToHash("attack1");
    private static readonly int FirstAttack = Animator.StringToHash("firstAttack");
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");

    private void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetBool(CanAttack, combatEnabled);
        PC = GetComponent<PlayerController>();
        PS = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        CheckCombatInput();
        CheckAttacks();
    }

    private void CheckCombatInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (combatEnabled)
            {
                // 공격!
                gotInput = true;
                lastInputTime = Time.time;
            }
        }
    }

    private void CheckAttacks()
    {
        if (gotInput)
        {
            // 공격 수행
            if (!isAttacking)
            {
                gotInput = false;
                isAttacking = true;
                isFirstAttack = !isFirstAttack;
                anim.SetBool(Attack1, true);
                anim.SetBool(FirstAttack, isFirstAttack);
                anim.SetBool(IsAttacking, isAttacking);
            }
        }

        if (Time.time >= lastInputTime)
        {
            // 새 입력 대기
            gotInput = false;
        }
    }

    private void CheckAttackHitBox()
    {
        Collider2D[] detectedObjects = Physics2D.OverlapCircleAll(attack1HitBoxPos.position, attack1Radius, whatIsDamageable);
        
        attackDetails[0] = attack1Damage;
        attackDetails[1] = transform.position.x;
        
        foreach (Collider2D collider2D in detectedObjects)
        {
            collider2D.transform.parent.SendMessage("Damage", attackDetails);
            // 피격 효과 재생
        }
    }

    private void FinishAttack1()
    {
        isAttacking = false;
        anim.SetBool(IsAttacking, isAttacking);
        anim.SetBool(Attack1, false);
    }

    private void Damage(float[] attackDetails)
    {
        if (!PC.GetDashStatus())
        {
            int direction;
        
            // 데미지 처리
            PS.DecreaseHealth(attackDetails[0]);

            if (attackDetails[1] > transform.position.x)
            {
                direction = -1;
            }
            else
            {
                direction = 1;
            }
        
            PC.Knockback(direction);
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attack1HitBoxPos.position, attack1Radius);
    }
}
