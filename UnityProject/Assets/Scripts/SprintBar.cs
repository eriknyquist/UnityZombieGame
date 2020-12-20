/*
 * SprintBar.cs
 *
 * Provides an interface to set the value of the sprint bar that displays the remaining
 * sprint time in the player's HUD
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprintBar : MonoBehaviour
{
    public float sprintValue;
    Transform sprintBar;

    // Start is called before the first frame update
    void Start()
    {
        sprintBar = gameObject.transform.Find("SprintBarGraphic");
    }

    /*
     * Set the health bar value (0.0-1.0)
     */
    public void SetSprint(float sprint)
    {
        sprintValue = sprint;
        Vector2 scale = sprintBar.transform.localScale;
        scale.x = sprint;
        sprintBar.transform.localScale = scale;
    }
}
