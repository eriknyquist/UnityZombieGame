/*
 * HealthBar.cs
 *
 * Provides an API for setting the value shown by the health bar in the player's HUD
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    Transform bar;

    // Start is called before the first frame update
    void Start()
    {
        bar = gameObject.transform.Find("HealthBarGraphic");
    }

    /*
     * Set the health bar value (0.0-1.0)
     */
    public void SetHealth(float health)
    {
        Vector2 scale = bar.transform.localScale;
        scale.x = health;
        bar.transform.localScale = scale;
    }
}
