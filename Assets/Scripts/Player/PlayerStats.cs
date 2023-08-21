using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    
    [SerializeField] private GameObject deathChunkParticle;

    private float currentHealth;

    private GameManager GM;

    private void Start()
    {
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
        
        currentHealth = maxHealth;
    }

    public void DecreaseHealth(float amount)
    {
        currentHealth -= amount;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();

        }
    }

    private void Die()
    {
        Instantiate(deathChunkParticle, transform.position, deathChunkParticle.transform.rotation);
        GM.Respawn();
        
        Destroy(gameObject);
    }
}
