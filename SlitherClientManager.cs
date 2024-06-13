using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using PlayFab.ClientModels;
using UnityEngine;

public class SlitherClientManager : MonoBehaviour
{
    private NetworkManager i_NetworkManager;
    
    [SerializeField] private PlayerData i_PlayerData;

    [SerializeField] private Tugboat i_Tugboat;

    [SerializeField] private GameObject i_DebugStatsPanel, i_LeaderboardPanel;

#if !UNITY_SERVER
    private void Awake()
    {
        i_NetworkManager = InstanceFinder.NetworkManager;
        i_Tugboat.SetClientAddress(i_PlayerData.p_PlayFlowIP == "" ? "localhost" : i_PlayerData.p_PlayFlowIP);
    }

    private void Start()
    {
        Debug.Log("Starting Client Connection");
        i_NetworkManager.ClientManager.StartConnection();
    }

    private void Update()
    {
        if (i_NetworkManager.IsOffline)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            i_LeaderboardPanel.SetActive(!i_LeaderboardPanel.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            i_DebugStatsPanel.SetActive(!i_DebugStatsPanel.activeSelf);
        }
    }
#endif
}
