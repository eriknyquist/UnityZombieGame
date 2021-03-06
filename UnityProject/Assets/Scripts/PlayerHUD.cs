﻿/*
 * PlayerHUD.cs
 *
 * Attached to the top-level PlayerHUD prefab, handles moving the HUD objects
 * around to follow the player
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public HealthBar healthBar;
    public SprintBar sprintBar;
    public ScoreBoard scoreBoard;
    public AmmoCounter ammoCounter;
    public BlockCounter blockCounter;
    public GameObject player;

    // Offset from player's position
    Vector3 offset = new Vector3(0.0f, -4.0f, 0.0f);

    Vector3 pivotOffset;

    // Start is called before the first frame update
    void Start()
    {
        GameObject centerPoint = gameObject.transform.Find("HUDCenterPoint").gameObject;
        pivotOffset = gameObject.transform.position - centerPoint.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = player.transform.position + offset + pivotOffset;
    }
}
