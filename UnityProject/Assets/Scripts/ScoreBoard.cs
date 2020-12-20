/*
 * ScoreBoard.cs
 *
 * Provides an interface for setting the score value shown on the player HUD
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreBoard : MonoBehaviour
{
    int score = 0;

    TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = gameObject.GetComponent<TextMesh>();
        MeshRenderer rend = gameObject.GetComponent<MeshRenderer>();
        rend.sortingOrder = 100;

        SetScore(score);
    }

    /*
     * Increment the displayed score value by 1
     */
    public void IncrementScore()
    {
        score += 1;
        SetScore(score);
    }

    /*
     * Set the displayed score value
     */
    public void SetScore(int score)
    {
        textMesh.text = score.ToString();
    }
}
