/*
 * Zombie.cs
 *
 * Handles controlling a zombie and making it move around the game world to
 * pursue to player.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/* The result of a single ray cast by RaycastSweep180() */
public class RaycastSweepHit
{
    public RaycastHit2D hit;   // Result of the raycat
    public float angle;        // Angle of the raycast, relative to player rotation

    public RaycastSweepHit(RaycastHit2D raycastHit, float raycastAngle)
    {
        hit = raycastHit;
        angle = raycastAngle;
    }
}

/* The result returned by ProcessRaycastSweep. Holds the information taken from
 * an array of RaycastSweepHit objects, needed to determine the zombie's next action
 * when in the IDLE state. */
public class ProcessedRaycastSweep
{
    public GameObject nearestZombie;        // Nearest follow-able zombie
    public Zombie nearestZombieScript;      // Nearest follow-able zombie script
    public GameObject nearestDestructible;  // Nearest destructible block
    public float highestDistanceAngle;      // Angle of raycast that had the highest distance
    public float lowestDistanceAngle;       // Angle of raycast that had the lowest distance

    public ProcessedRaycastSweep(GameObject zombie, Zombie zombieScript, GameObject destructible, float highest, float lowest)
    {
        nearestZombie = zombie;
        nearestZombieScript = zombieScript;
        nearestDestructible = destructible;
        highestDistanceAngle = highest;
        lowestDistanceAngle = lowest;
    }
}

public class Zombie : MonoBehaviour
{
    /* Enumeration of possible states a zombie can be in */
    public enum State
    {
        IDLE,             // Randomly milling about
        PURSUING,         // Following player with line of sight
        PURSUING_BLINDLY, // Following a zombie that has line-of-sight to the player
        TRACKING_BLINDLY, // Following a zombie that is in the TRACKING state
        DESTRUCTION,      // Destroying something the player built
        TRACKING,         // Moving toward player's last seen position
        INVALID_STATE     // No state
    }


    /* Enumeration of possible results from a linecast from zombie to player */
    public enum RaycastHitType
    {
        PLAYER_LOS,         // Have line-of-sight to player
        PURSUING_LOS,       // Line-of-sight to player is being blocked by a pursuing zombie
        TRACKING_LOS,       // Have line of sight to another zombie in the TRACKING state
        NO_LOS              // Can't see nuthin good
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

    // Max. time a zombie is allowed to stay in the TRACKING_BLINDLY state
    public float maxTrackingBlindlySeconds = 10.0f;

    // List of Zombies currently following this zombie
    public List<GameObject> followers = new List<GameObject>();

    // The zombie gameobject we are currently following
    public GameObject leaderZombie = null;

    // The Zombie class instance we are currently following
    public Zombie leaderZombieScript = null;

    GameObject trackedBlock = null;

    // Timestamp of last entry into TRACKING_BLINDLY state
    float trackingBlindlyEntryTime = -1.0f;

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
    float lastMovementTime = 0.0f;
    Vector3 lastPosition;

    public RaycastHitType hitType = RaycastHitType.NO_LOS;

    public void addFollower(GameObject follower)
    {
        followers.Add(follower);
    }

    public void removeFollower(GameObject follower)
    {
        followers.Remove(follower);
    }

    public void stopFollowing()
    {
        if (leaderZombieScript != null)
        {
            leaderZombieScript.removeFollower(gameObject);
            leaderZombie = null;
            leaderZombieScript = null;
        }

        state = State.IDLE;
        trackingBlindlyEntryTime = -1.0f;
    }

    public void dropFollowers()
    {
        List<GameObject> zombiesToDrop = new List<GameObject>();

        // Add ourself to the list to start off
        zombiesToDrop.Add(gameObject);

        /* Can't call dropFollowers on follower zombies, might cause stack overflow.
         * Need to maintain our own stack of zombies to drop instead. */
        while (zombiesToDrop.Count > 0)
        {
            // Pop the next item
            GameObject zombie = zombiesToDrop[0];
            zombiesToDrop.RemoveAt(0);

            if (zombie != null)
            {
                // Make this zombie stop following
                Zombie zombieScript = zombie.GetComponent<Zombie>();
                zombieScript.stopFollowing();

                // Add this zombie's followers to our stack
                zombiesToDrop.AddRange(zombieScript.followers);

                // Clear this zombie's followers list
                zombieScript.followers.Clear();
            }
        }
    }

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

    bool isWorthFollowing(Zombie otherZombie)
    {
        const int maxHopsToLeader = 16;
        Zombie currZombie = otherZombie;

        if ((otherZombie.state != State.PURSUING) &&
            (otherZombie.state != State.PURSUING_BLINDLY) &&
            (otherZombie.state != State.TRACKING) &&
            (otherZombie.state != State.TRACKING_BLINDLY))
        {
            return false;
        }

        for (int hops = 0; hops < maxHopsToLeader + 1; hops++)
        {
            if ((currZombie.state == State.PURSUING) ||
                (currZombie.state == State.PURSUING_BLINDLY) ||
                (currZombie.state == State.TRACKING))
            {
                /* Found a zombie that is actually following/tracking something within
                 * N hops, this zombie is worth following. */
                return true;
            }

            if (currZombie.leaderZombieScript == null)
            {
                return false;
            }
            else
            {
                // Check the next zombie up the chain
                currZombie = currZombie.leaderZombieScript;
            }
        }

        return false;
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

    /* Performs a 180 degree sweep of multiple raycasts in front of the zombie,
     * returning an array of RaycastSweepHit objects */
    RaycastSweepHit[] RaycastSweep180(int numCasts = 9)
    {
        List<RaycastSweepHit> hits = new List<RaycastSweepHit>();

        // Number of degrees to increment rotation by after each cast
        float degreesIncrement = 180f / (float) numCasts;

        // Temporarily disable boxcollider so we don't hit ourselves with the raycast
        boxCollider.enabled = false;

        // Do a 180 degree sweep of raycasts
        for (float angleOffset = 0f; angleOffset <= 180f; angleOffset += degreesIncrement)
        {
            // Calculate direction for this raycast
            Vector3 castDir = Quaternion.Euler(0, 0, angleOffset) * (-transform.up);

            // Do the raycast
            RaycastSweepHit hit = new RaycastSweepHit(Physics2D.Raycast(gameObject.transform.position, castDir), angleOffset);
            hits.Add(hit);

            // Draw a line showing the raycast, uncomment for debugging
            Debug.DrawLine(gameObject.transform.position, hit.hit.point, Color.green);
        }

        // Re-enable boxcollider
        boxCollider.enabled = true;

        // Convert results to array and return
        return hits.ToArray();
    }

    /* Processes an array of RaycastSweepHit objects into a single ProcessedRaycastSweep
     * object containing only the information needed to determine next actions. */
    ProcessedRaycastSweep ProcessRaycastSweep(RaycastSweepHit[] hits)
    {
        // Highest cast distance we've seen so far
        float highestDistance = 0.0f;

        // Rotation angle offset corresponding with highest cast distance
        float highestAngle = 0.0f;

        // Shortes cast distance we've seen so far
        float shortestDistance = float.MaxValue;

        // Rotation angle offset corresponding with highest cast distance
        float shortestAngle = 0.0f;

        // Nearest follow-able zombie
        RaycastSweepHit nearestZombieHit = null;
        GameObject nearestZombie = null;

        // Nearest follow-able zombie script
        Zombie nearestZombieScript = null;

        // nearest destructible object
        RaycastSweepHit nearestDestructibleHit = null;
        GameObject nearestDestructible = null;

        foreach (RaycastSweepHit hit in hits)
        {
            // Did we hit a zombie?
            if ("Zombie" == hit.hit.transform.gameObject.tag)
            {
                // Is it in a state that means it is following the player?
                Zombie otherZombie = hit.hit.transform.gameObject.GetComponent<Zombie>();
                if (nearestZombieHit == null || (hit.hit.distance < nearestZombieHit.hit.distance))
                {
                    if (isWorthFollowing(otherZombie))
                    {
                        nearestZombieHit = hit;
                        nearestZombie = hit.hit.transform.gameObject;
                    }
                }
            }
            // Did we hit a destructible wall?
            else if (hit.hit.transform.parent != null)
            {
                if (hit.hit.transform.parent.gameObject.tag == "DestructibleBlock")
                {
                    if ((nearestDestructibleHit == null) || (hit.hit.distance < nearestDestructibleHit.hit.distance))
                    {
                        nearestDestructibleHit = hit;
                        nearestDestructible = hit.hit.transform.gameObject;
                    }
                }
            }

            // Keep track of shortest and longest raycast distance
            if (hit.hit.distance > highestDistance)
            {
                highestDistance = hit.hit.distance;
                highestAngle = hit.angle;
            }
            if (hit.hit.distance < shortestDistance)
            {
                shortestDistance = hit.hit.distance;
                shortestAngle = hit.angle;
            }
        }

        if (nearestZombie != null)
        {
            nearestZombieScript = nearestZombie.GetComponent<Zombie>();
        }

        return new ProcessedRaycastSweep(nearestZombie, nearestZombieScript, nearestDestructible, highestAngle, shortestAngle);
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
                if (leaderZombie == null)
                {
                    leaderZombie = hit.transform.gameObject;
                    leaderZombieScript = leaderZombie.GetComponent<Zombie>();

                    if (isWorthFollowing(leaderZombieScript))
                    {
                        leaderZombieScript.addFollower(gameObject);
                        return RaycastHitType.TRACKING_LOS;
                    }
                }
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
        lastPosition = gameObject.transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        float now = Time.time;
        float distance = Vector3.Distance(lastPosition, gameObject.transform.position);

        if (distance > 0.5f)
        {
            lastMovementTime = now;
        }
    }

    bool isStuck()
    {
        return ((Time.time - lastMovementTime) >= 1.0f);
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
                if ((leaderZombieScript == null) || leaderZombieScript.killed || (State.IDLE == leaderZombieScript.state))
                {
                    // Leader zombie was destroyed or killed-- drop all followers, go to idle state
                    dropFollowers();
                    break;
                }

                if (trackingBlindlyEntryTime < 0.0f)
                {
                    trackingBlindlyEntryTime = Time.time;
                }

                if (maxTrackingBlindlySeconds <= (Time.time - trackingBlindlyEntryTime))
                {
                    // Time is up, drop all followers and back to idle state
                    dropFollowers();
                    break;
                }

                if (RaycastHitType.PLAYER_LOS == hitType)
                {
                    // We can see the player-- keep followers but move to PURSUING
                    leaderZombieScript.removeFollower(gameObject);
                    leaderZombie = null;
                    leaderZombieScript = null;
                    trackingBlindlyEntryTime = -1.0f;
                    state = State.PURSUING;
                    break;
                }
                else if (RaycastHitType.PURSUING_LOS == hitType)
                {
                    // We can see a PURSUING zombie-- keep followers but move to PURSUING_BLINDLY
                    leaderZombieScript.removeFollower(gameObject);
                    leaderZombie = null;
                    leaderZombieScript = null;
                    trackingBlindlyEntryTime = -1.0f;
                    state = State.PURSUING_BLINDLY;
                    break;
                }

                // Do we still have line-of-sight to our buddy zombie?
                boxCollider.enabled = false;
                RaycastHit2D buddyHit = Physics2D.Linecast(gameObject.transform.position, leaderZombie.transform.position);
                boxCollider.enabled = true;

                // (Uncomment for debugging)
                Debug.DrawLine(gameObject.transform.position, buddyHit.point, Color.red);

                if ((leaderZombie == null) ||
                    (buddyHit.transform == null)||
                    (buddyHit.transform.gameObject == null))
                {
                    // Buddy zombie was destroyed/killed, drop followers and go back to idle state.;
                    dropFollowers();
                    break;
                }

                if (!Object.ReferenceEquals(leaderZombie, buddyHit.transform.gameObject))
                {
                    // We do not have line of sight to buddy zombie. Did our linecast
                    // hit another zombie that is doing zomething interesting?
                    if (buddyHit.transform.gameObject.tag == "Zombie")
                    {
                        Zombie otherZombie = buddyHit.transform.gameObject.GetComponent<Zombie>();
                        if (isWorthFollowing(otherZombie))
                        {
                            // Make this guy our new leader
                            leaderZombieScript.removeFollower(gameObject);
                            leaderZombie = buddyHit.transform.gameObject;
                            leaderZombieScript = otherZombie;
                            leaderZombieScript.addFollower(gameObject);
                            break;
                        }
                    }

                    // No other interesting zombies to follow, back to idle state
                    dropFollowers();
                    break;
                }

                LookTowardsPosition(leaderZombie.transform.position);
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

            case State.DESTRUCTION:
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

                if (trackedBlock == null)
                {
                    state = State.IDLE;
                    break;
                }

                if (isStuck() || (Vector2.Distance(trackedBlock.transform.position, gameObject.transform.position) < 0.5f))
                {
                    Destroy(trackedBlock);
                    trackedBlock = null;
                    state = State.IDLE;
                    break;
                }

                LookTowardsPosition(trackedBlock.transform.position);
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

                // If time is up, do another sweep
                if ((Time.time - lastRaycastSweepTime) >= secondsBetweenRaycastSweeps)
                {
                    lastRaycastSweepTime = Time.time;
                    RaycastSweepHit[] hits = RaycastSweep180();
                    ProcessedRaycastSweep res = ProcessRaycastSweep(hits);

                    // Do we have a zombie worth following in view?
                    if (res.nearestZombie != null)
                    {
                        // Start following this zombie
                        leaderZombie = res.nearestZombie;
                        leaderZombieScript = leaderZombie.GetComponent<Zombie>();
                        leaderZombieScript.addFollower(gameObject);
                        state = State.TRACKING_BLINDLY;
                        break;
                    }
                    // Do we have a destructible block in view
                    else if (res.nearestDestructible != null)
                    {
                        // Keep track of the block we hit, and go to DESTRUCTION state
                        trackedBlock = res.nearestDestructible;
                        state = State.DESTRUCTION;
                        break;
                    }
                    // Nothing of interest in view, face towards direction of raycast with highest distance
                    else
                    {
                        idleLookDirection = gameObject.transform.rotation *
                                            Quaternion.Euler(0, 0, -90f) *
                                            Quaternion.Euler(0, 0, res.highestDistanceAngle);
                    }
                }

                gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation,
                                                                idleLookDirection,
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
