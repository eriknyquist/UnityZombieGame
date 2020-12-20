/*
 * AmmoCounter.cs
 *
 * Provides an API for setting the value displayed by the ammo counter within the
 * player's HUD.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoCounter : MonoBehaviour
{
    /* Maximum ammo. Calling Reload() reloads the counter with this value. */
    public int maxAmmo = 20;

    /* Current ammo counter value */
    public int ammo;

    TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = gameObject.GetComponent<TextMesh>();
        MeshRenderer rend = gameObject.GetComponent<MeshRenderer>();
        rend.sortingOrder = 100;

        Reload();
    }

    void DrawAmmoCount()
    {
        textMesh.text = ammo.ToString();
    }

    /*
     * Decrement the ammo counter value by 1 and re-draw the counter
     */
    public void DecrementAmmo()
    {
        ammo -= 1;
        DrawAmmoCount();
    }

    /*
     * Reload the counter value with maxAmmo and re-draw the counter
     */
    public void Reload()
    {
        ammo = maxAmmo;
        DrawAmmoCount();
    }
}
