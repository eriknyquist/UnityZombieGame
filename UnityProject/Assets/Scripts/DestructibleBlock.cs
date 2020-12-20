/*
 * DestructibleBlock.cs
 *
 * A single destructible block is made of multiple smaller individual blocks. This
 * script provides an API for performing some common operations on all of the individual
 * child blocks of a single destructible block.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    /*
     * Disable the box collider of all child blocks
     */
    public void disableColliders()
    {
        foreach (Transform child in gameObject.transform)
        {
            BoxCollider2D boxCollider = child.gameObject.GetComponent<BoxCollider2D>();
            boxCollider.enabled = false;
        }
    }

    /*
     * Enable the box collider of all child blocks
     */
    public void enableColliders()
    {
        foreach (Transform child in gameObject.transform)
        {
            BoxCollider2D boxCollider = child.gameObject.GetComponent<BoxCollider2D>();
            boxCollider.enabled = true;
        }
    }

    /*
     * Set the color of all child blocks
     */
    public void setColor(Color color)
    {
        foreach (Transform child in gameObject.transform)
        {
            child.GetComponent<SpriteRenderer>().material.color = color;
        }
    }

    /*
     * Get the color of the first child block
     */
    public Color getColor()
    {
        Color color = Color.red;

        foreach (Transform child in gameObject.transform)
        {
            color = child.GetComponent<SpriteRenderer>().material.color;
            break;
        }

        return color;
    }
}