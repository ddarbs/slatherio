using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

public class SuperFood : NetworkBehaviour
{
    
    public float speed = 1f; // Speed of the movement
    public float radiusIncreaseRate = 1f; // Rate at which the radius increases
    public float maxRadius = 75f; // Maximum radius for the spiral

    public float currentRadius = 1f; // Current radius of the spiral
    public float angle = 1f; // Angle to determine the position on the circle
    private Vector3 spawnposition;
    
    private LayerMask i_WallLayerMask;

    private void Awake()
    {
        i_WallLayerMask = LayerMask.GetMask("MapBound");
    }

    private void Start()
    {
        if (!IsServer)
        {
            this.enabled = false;
        }
        
        spawnposition = transform.position;
        currentRadius = 1f;
        angle = 1f;
    }

    private void FixedUpdate()
    {
        float adjustedSpeed = speed / currentRadius;
       // Increment angle based on speed
        angle += adjustedSpeed * Time.deltaTime;

        // Calculate position based on angle and current radius
        float x = Mathf.Cos(angle) * currentRadius + spawnposition.x;
        float y = Mathf.Sin(angle) * currentRadius + spawnposition.y;

        // Update object's position
        transform.position = new Vector3(x, y, 0);

        // Increase the radius
        currentRadius += radiusIncreaseRate * Time.deltaTime;

        // Clamp the radius to prevent it from exceeding the maximum
        currentRadius = Mathf.Clamp(currentRadius, 0f, maxRadius);
        
        Collider2D[] l_Colliders = new Collider2D[5];
        int l_Collisions = Physics2D.OverlapCircleNonAlloc(transform.position, 0.5f, l_Colliders, i_WallLayerMask);
        for (int i = 0; i < l_Collisions; i++)
        {
            if (l_Colliders[i].gameObject.CompareTag("MapBound"))
            {
                currentRadius = 1f;
                angle = 1f;
                transform.position = FoodSpawner.GetSuperFoodSpawnPoint();
                break;
            }
        }

    }
    
}

