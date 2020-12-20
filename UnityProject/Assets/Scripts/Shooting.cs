/*
 * Shooting.cs
 *
 * Handles firing projectiles originating from the player sprite position,
 * in the direction the player sprite is facing, when the "fire" button is pressed
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    // This should be set to the transform of an empty game object, positioned on
    // the player sprite at the point from which you want projectiles to appear.
    public Transform firePoint;

    // Game object to spwan to represent a single projectile
    public GameObject bulletPrefab;

    // Projectile initial force when spawned
    public float bulletForce = 1f;

    // Audio clip to play when firing projectile
    public AudioClip gunSound;

    AudioSource audioSource;
    PlayerHUD playerHUD;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();
    }

    void FireBullet()
    {
        if (0 == playerHUD.ammoCounter.ammo)
        {
            return;
        }

        audioSource.PlayOneShot(gunSound, 0.5f);
        playerHUD.ammoCounter.DecrementAmmo();
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse);
        Destroy(bullet, 2f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            InvokeRepeating("FireBullet", 0, 0.1f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            CancelInvoke("FireBullet");
        }
    }
}
