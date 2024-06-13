#if !UNITY_SERVER

using PlayFab.ClientModels;
using UnityEngine;

public class Debug_ResetPlayerData : MonoBehaviour
{
    [SerializeField] private PlayerData i_PlayerData;
    
    private void OnApplicationQuit()
    {
        i_PlayerData.p_SnakeColorIndex = 0;
        i_PlayerData.p_SnakeColor = Color.white;
        i_PlayerData.p_CurrentName = "";
        i_PlayerData.p_PlayFlowIP = "localhost";
        i_PlayerData.p_PlayFabLoginName = "";
        i_PlayerData.p_PlayFabLogin = new LoginResult();
    }
}

#endif