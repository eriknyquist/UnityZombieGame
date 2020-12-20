using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    enum State
    {
        START,
        PLACING
    };

    public const float maxBuildDistance = 2.0f;
    public GameObject wallPrefab;
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
                    Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                    placingWall = SpawnWall(mousePos);
                    placingWallScript = placingWall.GetComponent<DestructibleBlock>();
                    origColor = placingWallScript.getColor();
                    Color newColor = inRangeColor;

                    inRange = inBuildingRange(mousePos);
                    if (!inRange)
                    {
                        newColor = outOfRangeColor;
                    }

                    newColor.a = 0.5f;
                    placingWallScript.disableColliders();
                    placingWallScript.setColor(newColor);
                    state = State.PLACING;
                }
                break;

            case State.PLACING:
                Vector3 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
                newPos.z = 0;
                placingWall.transform.position = newPos;

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
                    if (!inRange)
                    {
                        Destroy(placingWall);
                    }
                    else
                    {
                        placingWallScript.enableColliders();
                        Color newColor = origColor;
                        origColor.a = 1.0f;
                        placingWallScript.setColor(newColor);
                    }

                    placingWallScript = null;
                    placingWall = null;
                    state = State.START;
                }
                break;
        }
    }
}
