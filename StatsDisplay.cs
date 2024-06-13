using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class StatsDisplay : NetworkBehaviour
{
    private const string c_FoodPrefix = "Food: ";
    private const string c_LengthPrefix = "Size: ";
    
    [SerializeField] private TextMeshProUGUI i_FoodText; // INFO: handled on server side then by ObserverRPC
    [SerializeField] private TextMeshProUGUI i_TimePlayedText, i_LengthText; // INFO: handled locally

    public static StatsDisplay i_Instance;

    private float i_TimeOffset;
    private float i_TimePlayed;

    private void Awake()
    {
        if (i_Instance == null)
        {
            i_Instance = this;
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogError("This shouldn't happen");
        }
    }

    public override void OnStartClient()
    {
        if (base.IsOwner)
        {
            i_TimeOffset = Time.time;
            i_LengthText.text = c_LengthPrefix + "3";
        }
    }
    
    public static void OnLengthChange(int _length)
    {
        i_Instance.i_LengthText.text = c_LengthPrefix + _length.ToString("N0");
    }

    public static void OnFoodChange(int _food)
    {
        i_Instance.ObserverSetFood(_food);
    }
    
    [ObserversRpc(BufferLast = true)]
    private void ObserverSetFood(int _food)
    {
        i_FoodText.text = c_FoodPrefix + _food.ToString("N0");
    }

    private void Update()
    {
        i_TimePlayed = Time.time - i_TimeOffset;
        int _minutes = Mathf.FloorToInt(i_TimePlayed / 60);
        int _seconds = Mathf.FloorToInt(i_TimePlayed % 60); 
        i_TimePlayedText.text = _minutes.ToString("00") + ":" + _seconds.ToString("00");
    }
}
