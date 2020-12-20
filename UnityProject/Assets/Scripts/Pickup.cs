/*
 * Pickup.cs
 *
 * Attached to pickup items that recharge after a certain amount of time.
 * Handles disabling the gameobject and re-enabling after the recharge time has elapsed.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public float pickupDelay = 30f;

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
    
    public void PickedUp()
    {
        Disable();
        Invoke("Enable", pickupDelay);
    }
}
