using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming

public class CombatDummyController : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float knockbackSpeedX, knockbackSpeedY;
    [SerializeField] private float knockbackDuration;
    [SerializeField] private float knockbackDeathSpeedX, knockbackDeathSpeedY;
    [SerializeField] private float deathTorque;
    [SerializeField] private bool applyKnockback;
    [SerializeField] private GameObject hitParticle;

    private float currentHealth;
    private float knockbackStart;
    
    private int playerFacingDirection;

    private bool playerOnLeft;
    private bool knockback;

    private PlayerController pc;
    private GameObject aliveGO, brokenTopGO, brokenBotGO;
    private Rigidbody2D rbAlive, rbBrokenTop, rbBrokenBot;
    private Animator aliveAnim;
    
    private static readonly int PlayerOnLeft = Animator.StringToHash("PlayerOnLeft");
    private static readonly int Damage1 = Animator.StringToHash("Damage");

    private void Start()
    {
        currentHealth = maxHealth;

        pc = GameObject.Find("Player").GetComponent<PlayerController>();

        aliveGO = transform.Find("Alive").gameObject;
        brokenTopGO = transform.Find("Broken Top").gameObject;
        brokenBotGO = transform.Find("Broken Bottom").gameObject;

        aliveAnim = aliveGO.GetComponent<Animator>();
        
        rbAlive = aliveGO.GetComponent<Rigidbody2D>();
        rbBrokenTop = brokenTopGO.GetComponent<Rigidbody2D>();
        rbBrokenBot = brokenBotGO.GetComponent<Rigidbody2D>();
        
        aliveGO.SetActive(true);
        brokenTopGO.SetActive(false);
        brokenBotGO.SetActive(false);
    }

    private void Update()
    {
        CheckKnockback();
    }

    private void Damage(float[] attackDetails)
    {
        currentHealth -= attackDetails[0];

        if (attackDetails[1] > aliveGO.transform.position.x)
        {
            playerFacingDirection = -1;
        }
        else
        {
            playerFacingDirection = 1;
        }

        Instantiate(hitParticle, aliveGO.transform.position,
            Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));

        // 1 = Right -> On Left!
        playerOnLeft = playerFacingDirection == 1;

        aliveAnim.SetBool(PlayerOnLeft, playerOnLeft);
        aliveAnim.SetTrigger(Damage1);

        if (applyKnockback && currentHealth > 0f)
        {
            // 넉백
            Knockback();
        }

        if (currentHealth <= 0f)
        {
            // 사망
            Die();
        }
    }

    private void Knockback()
    {
        knockback = true;
        knockbackStart = Time.time;
        rbAlive.velocity = new Vector2(knockbackSpeedX * playerFacingDirection, knockbackSpeedY);
    }

    private void CheckKnockback()
    {
        if (Time.time >= knockbackStart + knockbackDuration && knockback)
        {
            knockback = false;
            rbAlive.velocity = new Vector2(0f, rbAlive.velocity.y);
        }
    }

    private void Die()
    {
        aliveGO.SetActive(false);
        brokenTopGO.SetActive(true);
        brokenBotGO.SetActive(true);
        
        brokenTopGO.transform.position = aliveGO.transform.position;
        brokenBotGO.transform.position = aliveGO.transform.position;

        rbBrokenBot.velocity = new Vector2(knockbackSpeedX * playerFacingDirection, knockbackSpeedY);
        rbBrokenTop.velocity = new Vector2(knockbackDeathSpeedX * playerFacingDirection, knockbackDeathSpeedY);
        rbBrokenTop.AddTorque(deathTorque * -playerFacingDirection, ForceMode2D.Impulse);
    }
}
