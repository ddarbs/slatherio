using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Data"), System.Serializable]
public class PlayerData : ScriptableObject
{
    [Header("FishNet")]
    public int p_ClientID = -1; // INFO: not sure if this is needed?
    
    [Header("PlayFlow")]
    public string p_PlayFlowIP = "";
    
    [Header("PlayFab")]
    public string p_PlayFabLoginName;
    public LoginResult p_PlayFabLogin; // INFO: not sure if this is needed?

    [Header("Snake Name")] 
    public string p_CurrentName = "";
    
    [Header("Color")] 
    public int p_SnakeColorIndex = 0;
    public Color p_SnakeColor = Color.white;
}

