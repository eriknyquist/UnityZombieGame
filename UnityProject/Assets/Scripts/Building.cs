﻿/*
 * Building.cs
 *
 * Handles putting down the destructible blocks that the player can create by clicking
 * the right mouse button
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
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

    Color outOfRangeColor = Color.red;
    Color inRangeColor = Color.green;

    State state = State.START;
    GameObject placingWall = null;
    DestructibleBlock placingWallScript = null;
    Color origColor;
    bool inRange = false;

    GameObject SpawnWall(Vector3 position)
    {
        // Make sure z position is zero'd out
        position.z = 0;

        return Instantiate(wallPrefab, position, Quaternion.identity);
    }

    bool inBuildingRange(Vector3 mousePos)
    {
        float distance = Vector3.Distance(gameObject.transform.position, mousePos);
        return (distance <= maxBuildDistance);
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.START:
                if (Input.GetMouseButtonDown(1))
                {
                    /* Build button was pressed. Find out whene the mouse cursor is,
                    and spawn our building block prefab there. */
                    Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                    placingWall = SpawnWall(mousePos);

                    placingWallScript = placingWall.GetComponent<DestructibleBlock>();
                    origColor = placingWallScript.getColor();
                    Color newColor = inRangeColor;

                    /* If not in building range, make sure to set sprite color appropriately */
                    inRange = inBuildingRange(mousePos);
                    if (!inRange)
                    {
                        newColor = outOfRangeColor;
                    }

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
                newPos.z = 0;
                placingWall.transform.position = newPos;

                /* Set sprite color based on whether current mouse position is in building range */
                bool newInRange = inBuildingRange(newPos);
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
