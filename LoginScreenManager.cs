using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScreenManager : MonoBehaviour
{
    [SerializeField] private PlayerData i_PlayerData;
    
    #if !UNITY_SERVER
    private void Awake()
    {
        i_PlayerData.p_ClientID = -1;
    }

    
#endif
}
