﻿/*
 * PlayerCollisions.cs
 *
 * Handles actions that need to be performed when the player collides with
 * some dynamic object in the world (like an enemy or an ammo box), such as playing
 * an audio clip or decrementing the player's health.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCollisions : MonoBehaviour
{
    public int coolOffSeconds = 1;
    public float coolOffFlashDelay = 0.1f;
    public AudioClip ammoPickupSound;
    public AudioClip healthPickupSound;
    public AudioClip playerHitSound;

    AudioSource audioSource;
    GameObject player;
    PlayerHUD playerHUD;
    int maxHealth = 5;
    SpriteRenderer rend;
    int health;
    bool coolingOff = false;

    /* Tracks the number of zombies touching us-- we increment in OnCollisionEnter
     * and decrement in OnCollisionExit */
    int touching = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player");
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();

        health = maxHealth;
        rend = gameObject.GetComponent<SpriteRenderer>();
    }

    void EnterCoolOff()
    {
        coolingOff = true;
        InvokeRepeating("toggleRenderer", 0, coolOffFlashDelay);
    }

    void ExitCoolOff()
    {
        CancelInvoke("toggleRenderer");
        coolingOff = false;

        // make sure player rendering is enabled
        rend.enabled = true;

        /* Finally, check if there are currently zombies touching us, in which
         * case we'll trigger another zombie hit immediately */
         if (touching > 0)
         {
            PlayerHitByZombie();
         }
    }

    void toggleRenderer()
    {
        rend.enabled = !rend.enabled;
    }

    void Death()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    void PlayerHitByZombie()
    {
        // Decrement player's health
        health -= 1;
        playerHUD.healthBar.SetHealth((float) health / (float) maxHealth);

        // Play sound
        audioSource.PlayOneShot(playerHitSound, 0.5f);

        if (health == 0)
        {
            // Player is dead
            Death();
        }
        else
        {
            EnterCoolOff();
            Invoke("ExitCoolOff", coolOffSeconds);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        GameObject collided = collision.gameObject;
        Zombie zombie = collided.GetComponent<Zombie>();

        if (zombie != null)
        {
            // Exiting collision with a zombie
            touching -= 1;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Pickup pickup;

        switch (other.gameObject.tag)
        {
            case "Ammo":
                audioSource.PlayOneShot(ammoPickupSound, 0.5f);
                playerHUD.ammoCounter.Reload();
                pickup = other.gameObject.GetComponent<Pickup>();
                pickup.PickedUp();
                break;

            case "Health":
                audioSource.PlayOneShot(healthPickupSound, 0.5f);
                pickup = other.gameObject.GetComponent<Pickup>();
                pickup.PickedUp();
                SetMaxHealth();
                break;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject collided = collision.gameObject;

        switch (collided.tag)
        {
            case "Zombie":
                Zombie zombie = collided.GetComponent<Zombie>();
                touching += 1;

                if (!coolingOff)
                {
                    // Finished cooling off from last zombie hit
                    PlayerHitByZombie();
                }
                break;
        }
    }

    public void SetMaxHealth()
    {
        health = maxHealth;
        playerHUD.healthBar.SetHealth(1f);
    }
}
