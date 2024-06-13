using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class FoodSync : NetworkBehaviour
{
    /*private void Start()
    {
        Debug.Log(this.GetComponent<SpriteRenderer>().color);
    }*/

    public override void OnStartClient()
    {
        GrabColor(LocalConnection);
    }
    
    [ServerRpc(RequireOwnership = false)] 
    private void GrabColor(NetworkConnection conn)
    {
        Color l_Color = this.GetComponent<SpriteRenderer>().color;
        SendColor(conn, l_Color);
    }

    [TargetRpc]
    private void SendColor(NetworkConnection conn, Color SelectedColor)
    {
        SelectedColor.a = 1f;
        //Debug.Log(SelectedColor);
        this.GetComponent<SpriteRenderer>().color = SelectedColor;
    }

}
