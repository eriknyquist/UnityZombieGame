/*
 * Bullet.cs
 *
 * Attached to each bullet prefab fired from the player's gun. Handles any actions
 * that need to happen when the bullet collides with something, e.g. playing a sound
 * or destroying the object that was hit.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public AudioClip wallSound;
    public AudioClip zombieSound;

    AudioSource audioSource;
    ParticleSystem blood;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject collided = collision.gameObject;
        Zombie zombie = collided.GetComponent<Zombie>();

        // Did we hit a zombie?
        if (null!= zombie)
        {
            audioSource.PlayOneShot(zombieSound, 0.2f);
            zombie.BulletHit();
        }
        else
        {
            // Play a sound
            audioSource.PlayOneShot(wallSound, 0.2f);

            // Did we hit a destructible wall?
            if (collided.transform.parent != null)
            {
                if (collided.transform.parent.gameObject.tag == "DestructibleBlock")
                {
                    // Destroy the smaller sub-block that was hit
                    Destroy(collided.transform.gameObject);
                }
            }
        }

        /* Disable rigidbody and boxcollider, so the bullet effectively disappears,
         * but the sound will keep playing  */
        SpriteRenderer rend = GetComponent<SpriteRenderer>();
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        rend.enabled = false;
        Destroy(bc);

        // Finally, destroy the bullet after 1s
        Destroy(gameObject, 1f);
    }
}
