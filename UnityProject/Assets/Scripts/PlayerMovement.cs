/*
 * PlayerMovement.cs
 * 
 * This file handles moving the player sprite based on inputs, and rotating the
 * player sprite so that it is always facing towards the mouse cursor
 */
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Player movement speed while walking
    public float walkSpeed = 1.0f;
    
    // Player movement speed while sprinting
    public float sprintSpeed = 10.0f;
    
    // Player ribidbody object
    public Rigidbody2D rb;
    
    // Game camera object
    public Camera cam;
    
    // Max. continuous sprinting time
    public float sprintTimeSecs = 2.0f;
    
    // Time to recharge a fully depleted spring
    public float sprintRechargeTimeSecs = 4.0f;
    
    float sprintBarValue = 1.0f;
    float moveSpeed = 0.0f;
    bool sprinting = false;
    Vector2 movement;
    Vector2 mousePos;
    PlayerHUD playerHUD;
    
    void Start()
    {
        GameObject hud = GameObject.FindGameObjectWithTag("PlayerHUD");
        playerHUD = hud.GetComponent<PlayerHUD>();  
        moveSpeed = walkSpeed;
    }

    // Grow the spring bar by 1 frames worth of time, taking 'sprintTimeSecs' and 
    // 'sprintRechargeMultipler' into account
    void incrementSprint(bool sprinting)
    {
        if (sprintBarValue >= 1.0f)
        {
            // Sprint  time has expired
            sprintBarValue = 1.0f;
        }
        else
        {
            sprintBarValue += (Time.deltaTime / (sprintRechargeTimeSecs));
            if (sprinting)
            {
                moveSpeed = sprintSpeed;
            }
        } 
    }

    // Shrink the sprint bar by 1 frames worth of time, taking 'sprintTimeSecs' into account
    void decrementSprint(bool sprinting)
    {
        if (sprintBarValue <= 0.0f)
        {
            sprintBarValue = 0.0f;
            if (sprinting)
            {
                // Can't sprint anymore
                moveSpeed = walkSpeed;
            }
        }
        else
        {
           sprintBarValue -= (Time.deltaTime / sprintTimeSecs);
        }
    }
    
    // Set the player's move speed based on whether the shift key
    // is being held down, and whether we have any sprint time remaining.
    // Also handles setting the value of the "sprint bar" in the HUD.
    void handleSprinting()
    {
        if (sprinting)
        {
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {                
                moveSpeed = walkSpeed;
                sprinting = false; 
            }
            else
            {
                if  ((movement.x == 0.0f) && (movement.y == 0.0f))
                {
                    incrementSprint(sprinting);
                }
                else
                {
                    decrementSprint(sprinting);
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                moveSpeed = sprintSpeed;
                sprinting = true;
            }
            else
            {
                incrementSprint(sprinting);
            }
        }
        
        playerHUD.sprintBar.SetSprint(sprintBarValue);
    }
    
    // Update is called once per frame
    void Update()
    {
        // Read inputs for player movement
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // Read mouse cursor position
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        handleSprinting();
    }
    
    void FixedUpdate()
    {
        // Move player based on axis positions
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        
        // Rotate player to look at mouse cursor position
        Vector2 lookDir = mousePos - rb.position;
        rb.rotation = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
    }
}
