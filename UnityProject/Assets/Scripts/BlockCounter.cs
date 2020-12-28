/*
 * BlockCounter.cs
 *
 * Provides an API for setting the value displayed by the block counter within the
 * player's HUD.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockCounter : MonoBehaviour
{
    TextMesh textMesh;
    int count = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        textMesh = gameObject.GetComponent<TextMesh>();
        MeshRenderer rend = gameObject.GetComponent<MeshRenderer>();
        rend.sortingOrder = 100;
        DrawBlockCount();
    }

    void DrawBlockCount()
    {
        textMesh.text = count.ToString();
    }

    /*
     * Decrement the block counter value by 1 and re-draw the counter
     */
    public void DecrementCount()
    {
        count -= 1;
        DrawBlockCount();
    }
    
    /*
     * Increment the block counter value by 1 and re-draw the counter
     */
    public void IncrementCount()
    {
        count += 1;
        DrawBlockCount();
    }

    /* Set the block count value directly and re-draw the counter */
    public void SetCount(int newCount)
    {
        count = newCount;
        DrawBlockCount();
    }
}