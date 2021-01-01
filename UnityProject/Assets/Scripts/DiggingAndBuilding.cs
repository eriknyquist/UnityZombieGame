/*
 * Building.cs
 *
 * Handles putting down the destructible blocks that the player can create by clicking
 * the right mouse button
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiggingAndBuilding : MonoBehaviour
{
    /* Possible states for building blocks */
    enum State
    {
        START,   // Build button is not being presssed
        PLACING  // Build button is being pressed
    };

    /* How far away from the player can blocks be built? */
    public const float maxBuildDistance = 2.0f;

    /* The desctructible block prefab */
    public GameObject wallPrefab;

    /* Game camera */
    public Camera cam;

    float digChunkRadius = 0.35f;
    float distanceFromDigger = 0.4f;

    Color outOfRangeColor = Color.red;
    Color inRangeColor = Color.green;

    int blocksAvailable = 0;
    int blocksPerBuild = 16;
    float subBlockSize = 0.0f;

    State state = State.START;
    GameObject placingWall = null;
    DestructibleBlock placingWallScript = null;
    Color origColor;
    bool inRange = false;
    BoxCollider2D playerCollider;
    PlayerHUD playerHUD;

    void Start()
    {
        playerCollider = gameObject.GetComponent<BoxCollider2D>();
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();
    }

    GameObject SpawnWall(Vector3 position)
    {
        // Make sure z position is zero'd out
        position.z = 0;

        return Instantiate(wallPrefab, position, Quaternion.identity);
    }

    bool canPlaceWall(Vector3 mousePos)
    {
        // Do we have enough blocks to build a wall?
        if (blocksAvailable < blocksPerBuild)
        {
            // Nope
            return false;
        }

        // Is the mouse position close enough to the player sprite?
        float distance = Vector3.Distance(gameObject.transform.position, mousePos);
        return ((distance <= maxBuildDistance) && (distance >= 0.5f));
    }

    int digChunk(float radius)
    {
        int destroyed = 0;

        Vector2 origin = gameObject.transform.position + gameObject.transform.up * distanceFromDigger;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, gameObject.transform.up, 0.0f);

        foreach (RaycastHit2D hit in hits)
        {
            // Did we get any destructible blocks within the radius?
            if (hit.transform.parent != null)
            {
                if (hit.transform.parent.gameObject.tag == "DestructibleBlock")
                {
                    // Destroy the block
                    Destroy(hit.transform.gameObject);
                    destroyed += 1;
                }
            }
        }

        return destroyed;
    }

    // Update is called once per frame
    void Update()
    {
        // First, check if the dig button has been hit
        if (Input.GetKeyDown(KeyCode.Space))
        {
            blocksAvailable += digChunk(digChunkRadius);
            playerHUD.blockCounter.SetCount(blocksAvailable / blocksPerBuild);
        }

        // Now, run the state machine for building
        switch (state)
        {
            case State.START:
                if (Input.GetMouseButtonDown(1))
                {
                    /* Build button was pressed. Find out whene the mouse cursor is,
                    and spawn our building block prefab there. */
                    Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                    mousePos.z = 0;
                    placingWall = SpawnWall(mousePos);

                    placingWallScript = placingWall.GetComponent<DestructibleBlock>();
                    blocksPerBuild = placingWallScript.numSubBlocks;
                    subBlockSize = placingWallScript.subBlockSize;
                    origColor = placingWallScript.getColor();
                    Color newColor = inRangeColor;

                    /* If not in building range, make sure to set sprite color appropriately */
                    inRange = canPlaceWall(mousePos);
                    if (!inRange)
                    {
                        newColor = outOfRangeColor;
                    }

                    /* Move the block sprite to snap to sub-block size */
                    Vector3 mPos = placingWall.transform.position;
                    mPos.x = Mathf.Round(mPos.x / subBlockSize) * subBlockSize;
                    mPos.y = Mathf.Round(mPos.y / subBlockSize) * subBlockSize;
                    placingWall.transform.position = mPos;

                    /* Collider is disabled and sprite is translucent until build button is released */
                    newColor.a = 0.5f;
                    placingWallScript.disableColliders();
                    placingWallScript.setColor(newColor);

                    /* Go to PLACING until the build button is released... */
                    state = State.PLACING;
                }
                break;

            case State.PLACING:
                /* Move the building block prefab to keep it under the current mouse position */
                Vector3 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
                newPos.x = Mathf.Round(newPos.x / subBlockSize) * subBlockSize;
                newPos.y = Mathf.Round(newPos.y / subBlockSize) * subBlockSize;
                newPos.z = 0;
                placingWall.transform.position = newPos;

                /* Set sprite color based on whether current mouse position is in building range */
                bool newInRange = canPlaceWall(newPos);
                if (newInRange != inRange)
                {
                    inRange = newInRange;
                    Color newColor = inRangeColor;

                    if (!inRange)
                    {
                        newColor = outOfRangeColor;
                    }

                    newColor.a = 0.5f;
                    placingWallScript.setColor(newColor);
                }

                if (Input.GetMouseButtonUp(1))
                {
                    /* Build button released. If the current mouse position is in
                     * range of the player, set the block sprite color + transparency
                     * back to normal and enable the block's collider again. Otherwise,
                     * destroy the block prefab, since it can't be placed. */
                    if (inRange)
                    {
                        placingWallScript.enableColliders();
                        Color newColor = origColor;
                        origColor.a = 1.0f;
                        placingWallScript.setColor(newColor);
                        blocksAvailable -= blocksPerBuild;
                        playerHUD.blockCounter.SetCount(blocksAvailable / blocksPerBuild);
                    }
                    else
                    {
                        Destroy(placingWall);
                    }

                    placingWallScript = null;
                    placingWall = null;

                    /* Back to the start state. */
                    state = State.START;
                }
                break;
        }
    }
}
