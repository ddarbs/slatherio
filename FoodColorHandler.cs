using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using Random = UnityEngine.Random;

public class FoodColorHandler : NetworkBehaviour // INFO: this is all local, don't think it will matter what each person sees food color as
{
    [SerializeField] private ParticleSystem i_Particles;
    [SerializeField] private Color[] i_Colors;
    [SerializeField] private CircleCollider2D i_Collider; // INFO: we shouldn't be doing this here but fuck it
    
    private void Start()
    {
        AssignColor();
    }

    private void OnEnable()
    {
        AssignColor();
        //i_Collider.enabled = true;
    }
    
    private void AssignColor()
    {
        i_Particles.startColor = i_Colors[Random.Range(0, i_Colors.Length)];
        i_Particles.Play();
        //Debug.Log("Assign color was called");
    }
}
