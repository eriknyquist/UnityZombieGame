/*
 * Zombie.cs
 * 
 * Handles controlling a zombie and making it move around the game world to
 * pursue to player.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour
{
    /* Enumeration of possible states a zombie can be in */
    public enum State
    {
        IDLE,             // Randomly milling about
        PURSUING,         // Following player with line of sight
        PURSUING_BLINDLY, // Following a zombie that has line-of-sight to the player
        TRACKING_BLINDLY, // Following a zombie that is in the TRACKING state
        TRACKING          // Moving toward player's last seen position
    }


    /* Enumeration of possible results from a linecast from zombie to player */
    public enum RaycastHitType
    {
        PLAYER_LOS,    // Zombie has line-of-sight to player
        PURSUING_LOS,  // Line-of-sight to player is being blocked by a pursuing zombie
        TRACKING_LOS,  // Zombie has line of sight to another zombie in the TRACKING state
        NO_LOS         // Can't see nuthin good
    }

    // Constant velocity damping value
    public float damping = 0.9f;

    // Number of times a zombie can be shot before dying
    public int hp = 2;

    public State state = State.IDLE;

    public bool killed;

    /* We will do a raycast towards the player's position, to see if we have line-of-sight,
     * this often */
    public const float secondsBetweenPlayerRaycasts = 0.5f;

    /* When in the idle state, we will do a 180 degree raycast sweep in front of
     * the zombie to find the best direction to face, this often */
    public const float secondsBetweenRaycastSweeps = 0.5f;

    // Reference to player gameobject
    Transform player;

    // Movement speed for IDLE state
    const float SLOW_SPEED = 0.008f;

    // Movement speeds for PURSUING and TRACKING states
    const float FAST_SPEED = 0.04f;

    // Min. wall distance before zombie will turn away
    const float WALL_BOUNDARY = 1f;

    float lastPlayerRaycastTime = 0.0f;
    float lastRaycastSweepTime = 0.0f;
    BoxCollider2D boxCollider;
    LevelManager levelManager;
    PlayerHUD playerHUD;
    Vector2 playerPos;
    Vector2 lastSeenPlayerPos;
    RaycastHit2D playerHit;
    ParticleSystem blood;
    Quaternion idleLookDirection;

    public GameObject buddyZombie;
    Zombie buddyZombieScript;
    public RaycastHitType hitType = RaycastHitType.NO_LOS;

    public void SetRotationAngle(float angle)
    {
        gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void BulletHit()
    {
        hp -= 1;

        EnableBlood();
        Invoke("DisableBlood", 0.2f);

        if (0 == hp)
        {
            Death();
        }
    }

    void DestroyZombie()
    {
        Destroy(gameObject);
    }

    void Death()
    {
        killed = true;
        playerHUD.scoreBoard.IncrementScore();

        /* Disable rigidbody and boxcollider, so the zombie effectively disappears,
         * but the particle system will keep emitting */
        SpriteRenderer rend = gameObject.GetComponent<SpriteRenderer>();
        rend.enabled = false;
        Destroy(boxCollider);

        // Inform LevelManager that a zombie was killed
        levelManager.ZombieKilled();

        /* Destroy the gameobject, which will also destroy the particle system,
         * in 1 second */
        Invoke("DestroyZombie", 1f);
    }

    void EnableBlood()
    {
        ParticleSystem.EmissionModule em = blood.emission;
        em.enabled = true;
    }

    void DisableBlood()
    {
        ParticleSystem.EmissionModule em = blood.emission;
        em.enabled = false;
    }

    /* Used when a zombie is in the State.IDLE state, to do multiple raycasts in a 180
     * degree sweep in front of the zombie, and calculate the rotation vector required to
     * make the zombie look in the desired direction. The desired direction depends on what the rays
     * we cast collided with:
     *
     * Collided with nothing, or with static scenery: zombie will turn to face in the direction
     * of whichever raycast had the highest distance, and remain in the IDLE state.
     *
     * Collided with another zombie in any state except IDLE: zombie will turn to
     * face the other zombie, and start following the other zombie by entering the
     * TRACKING_BLINDLY state */
    Quaternion RaycastSweepForLookDirection()
    {
        // Return cached copy until time is up for the next sweep
        if ((Time.time - lastRaycastSweepTime) < secondsBetweenRaycastSweeps)
        {
            return idleLookDirection;
        }

        lastRaycastSweepTime = Time.time;

        // Total number of raycasts to do in the sweep
        const int numCasts = 9;

        // Number of degrees to increment rotation by after each cast
        float degreesIncrement = 180f / (float) numCasts;

        // Highest cast distance we've seen so far
        float highestDistance = 0f;

        // Rotation angle offset corresponding with highest cast distance
        float highestAngle = 0f;

        // Temporarily disable boxcollider so we don't hit ourselves with the raycast
        boxCollider.enabled = false;

        // Do a 180 degree sweep of raycasts
        for (float angleOffset = 0f; angleOffset <= 180f; angleOffset += degreesIncrement)
        {
            // Calculate direction for this raycast
            Vector3 castDir = Quaternion.Euler(0, 0, angleOffset) * (-transform.up);

            // Do the raycast
            RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, castDir);

            // Draw a line showing the raycast, uncomment for debugging
            Debug.DrawLine(gameObject.transform.position, hit.point, Color.green);

            if ("Zombie" == hit.transform.gameObject.tag)
            {
                Zombie otherZombie = hit.transform.gameObject.GetComponent<Zombie>();
                if (State.IDLE != otherZombie.state)
                {
                    if (buddyZombie == null)
                    {
                        buddyZombie = hit.transform.gameObject;
                        buddyZombieScript = buddyZombie.GetComponent<Zombie>();
                        state = State.TRACKING_BLINDLY;
                    }

                    // Return direction to look at non-idle zombie
                    return gameObject.transform.rotation * Quaternion.Euler(0, 0, -90f) *
                           Quaternion.Euler(0, 0, AngleTowardsPosition(hit.transform.position));
                }
            }

            if (hit.distance > highestDistance)
            {
                highestDistance = hit.distance;
                highestAngle = angleOffset;
            }
        }

        // Re-enable boxcollider
        boxCollider.enabled = true;

        // Calculate angle of rotation to face direction of cast with the highest distance
        idleLookDirection = gameObject.transform.rotation * Quaternion.Euler(0, 0, -90f) * Quaternion.Euler(0, 0, highestAngle);
        return idleLookDirection;
    }

    RaycastHitType TranslatePlayerLineCast(RaycastHit2D hit)
    {
        if (!hit)
        {
            return RaycastHitType.NO_LOS;
        }

        if ("Player" == hit.transform.gameObject.tag)
        {
            lastSeenPlayerPos = hit.transform.position;
            return RaycastHitType.PLAYER_LOS;
        }

        if ("Zombie" == hit.transform.gameObject.tag)
        {
            Zombie otherZombie = hit.transform.gameObject.GetComponent<Zombie>();
            if ((State.PURSUING == otherZombie.state) || (State.PURSUING_BLINDLY == otherZombie.state))
            {
                return RaycastHitType.PURSUING_LOS;
            }
            else if ((State.TRACKING == otherZombie.state) || (State.TRACKING_BLINDLY == otherZombie.state))
            {
                if (buddyZombie == null)
                {
                    buddyZombie = hit.transform.gameObject;
                    buddyZombieScript = buddyZombie.GetComponent<Zombie>();
                }

                return RaycastHitType.TRACKING_LOS;
            }
        }

        return RaycastHitType.NO_LOS;
    }

    void UpdatePlayerLineCast()
    {
        if ((Time.time - lastPlayerRaycastTime) >= secondsBetweenPlayerRaycasts)
        {
            lastPlayerRaycastTime = Time.time;
            playerHit = Physics2D.Linecast(gameObject.transform.position, playerPos);

            // Uncomment for debugging
            Debug.DrawLine(gameObject.transform.position, playerHit.point, Color.blue);

            hitType = TranslatePlayerLineCast(playerHit);
        }
    }

    void Start()
    {
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();

        GameObject mgr = GameObject.FindGameObjectWithTag("LevelManager");
        levelManager = mgr.GetComponent<LevelManager>();

        blood = gameObject.GetComponent<ParticleSystem>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        boxCollider = gameObject.GetComponent<BoxCollider2D>();

        killed = false;
    }


    // Update is called once per frame
    void Update()
    {

    }

    // Calculate angle of rotation required to make zombie look at given position
    float AngleTowardsPosition(Vector2 pos)
    {
        Vector2 currPos = gameObject.transform.position;
        Vector2 lookDir = pos - currPos;
        return Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
    }

    // Rotate zombie to face given position
    void LookTowardsPosition(Vector2 pos)
    {
        //gameObject.transform.rotation = Quaternion.Euler(0, 0, AngleTowardsPosition(pos));
        Quaternion newRot = Quaternion.Euler(0, 0, AngleTowardsPosition(pos));
        gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation, newRot, 0.1f);
    }

    // Move zombie forwards in the direction of rotation
    void MoveForwards(float speed)
    {
        gameObject.transform.position += gameObject.transform.right * speed;
    }

    void FixedUpdate()
    {
        if (killed)
        {
            /* If zombie has been killed and we're just waiting for the gameobject
             * to be destroyed, no need to do anything here */
            return;
        }

        // Save player position
        playerPos = player.position;

        // Temporarily disable boxcollider so we don't hit ourselves with the raycast
        boxCollider.enabled = false;

        // Update raycast towards player
        UpdatePlayerLineCast();

        // Re-enable boxcollider
        boxCollider.enabled = true;


        switch (state)
        {
            case State.PURSUING:
                if (RaycastHitType.PURSUING_LOS == hitType)
                {
                    state = State.PURSUING_BLINDLY;
                    break;
                }
                else if (RaycastHitType.NO_LOS == hitType)
                {
                    state = State.TRACKING;
                    break;
                }
                else if (RaycastHitType.TRACKING_LOS == hitType)
                {
                    state = State.TRACKING_BLINDLY;
                    break;
                }

                // Turn zombie to face towards player
                LookTowardsPosition(playerPos);
                MoveForwards(FAST_SPEED);
                break;

            case State.PURSUING_BLINDLY:
                if (RaycastHitType.PLAYER_LOS == hitType)
                {
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.NO_LOS == hitType)
                {
                    state = State.IDLE;
                    break;
                }
                else if (RaycastHitType.TRACKING_LOS == hitType)
                {
                    state = State.TRACKING_BLINDLY;
                    break;
                }

                // Turn zombie to face towards player
                LookTowardsPosition(playerPos);
                MoveForwards(FAST_SPEED);
                break;

            case State.TRACKING_BLINDLY:
                if ((buddyZombieScript == null) || buddyZombieScript.killed || (State.IDLE == buddyZombieScript.state))
                {
                    buddyZombie = null;
                    buddyZombieScript = null;
                    state = State.IDLE;
                    break;
                }

                if (RaycastHitType.PLAYER_LOS == hitType)
                {
                    buddyZombie = null;
                    buddyZombieScript = null;
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.PURSUING_LOS == hitType)
                {
                    buddyZombie = null;
                    buddyZombieScript = null;
                    state = State.PURSUING_BLINDLY;
                    break;
                }

                // Do we still have line-of-sight to our buddy zombie?
                boxCollider.enabled = false;
                RaycastHit2D buddyHit = Physics2D.Linecast(gameObject.transform.position, buddyZombie.transform.position);
                boxCollider.enabled = true;

                // Uncomment for debugging
                Debug.DrawLine(gameObject.transform.position, buddyHit.point, Color.red);

                if ((buddyZombie == null) || !Object.ReferenceEquals(buddyZombie, buddyHit.transform.gameObject))
                {
                    // No. back to idle state.
                    buddyZombie = null;
                    buddyZombieScript = null;
                    state = State.IDLE;
                    break;
                }

                LookTowardsPosition(buddyZombie.transform.position);
                MoveForwards(FAST_SPEED);
                break;

            case State.TRACKING:
                if (RaycastHitType.PLAYER_LOS == hitType)
                {
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.PURSUING_LOS == hitType)
                {
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.TRACKING_LOS == hitType)
                {
                    state = State.TRACKING_BLINDLY;
                    break;
                }
                // Do we still have line-of-sight to last seen player position?
                boxCollider.enabled = false;
                RaycastHit2D trackingHit = Physics2D.Linecast(gameObject.transform.position, lastSeenPlayerPos);
                Debug.DrawLine(gameObject.transform.position, lastSeenPlayerPos, Color.yellow);
                boxCollider.enabled = true;


                if (trackingHit.collider != null)
                {
                    if ("Player" != trackingHit.transform.gameObject.tag)
                    {
                        // We hit something, no longer have line-of-sight to last player position
                        state = State.IDLE;
                        break;
                    }
                }

                if (Vector2.Distance(lastSeenPlayerPos, gameObject.transform.position) < 0.5f)
                {
                    state = State.IDLE;
                    break;
                }

                LookTowardsPosition(lastSeenPlayerPos);
                MoveForwards(FAST_SPEED);
                break;

            case State.IDLE:
                if (RaycastHitType.PLAYER_LOS == hitType)
                {
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.PURSUING_LOS == hitType)
                {
                    state = State.PURSUING_BLINDLY;
                    break;
                }
                else if (RaycastHitType.TRACKING_LOS == hitType)
                {
                    state = State.TRACKING_BLINDLY;
                    break;
                }

                gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation,
                                                                RaycastSweepForLookDirection(),
                                                                0.05f);
                MoveForwards(SLOW_SPEED);
                break;
        }

        /* Constant velocity damping (otherwise zombies would fly off into eternity
         * whenever a bullet hits them) */
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        rb.velocity *= damping;
    }
}
