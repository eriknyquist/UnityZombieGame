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
    
    public void IncrementScore()
    {
        score += 1;
        SetScore(score);
    }
    
    public void SetScore(int score)
    {
        textMesh.text = score.ToString();
    }
}
