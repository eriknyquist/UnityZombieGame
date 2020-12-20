using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    public void disableColliders()
    {
        foreach (Transform child in gameObject.transform)
        {
            BoxCollider2D boxCollider = child.gameObject.GetComponent<BoxCollider2D>();
            boxCollider.enabled = false;
        }
    }

    public void enableColliders()
    {
        foreach (Transform child in gameObject.transform)
        {
            BoxCollider2D boxCollider = child.gameObject.GetComponent<BoxCollider2D>();
            boxCollider.enabled = true;
        }
    }

    public void setAlpha(float alpha)
    {
        foreach (Transform child in gameObject.transform)
        {
            Color color = child.GetComponent<SpriteRenderer>().material.color;
            color.a = alpha;
            child.GetComponent<SpriteRenderer>().material.color = color;
        }
    }

    public void setColor(Color color)
    {
        foreach (Transform child in gameObject.transform)
        {
            child.GetComponent<SpriteRenderer>().material.color = color;
        }
    }

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
