using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// INFO: ANY SCENE USING THIS SCRIPT CANNOT BE ADDED TO THE CLIENT BUILD
#if UNITY_EDITOR || UNITY_64
public class SlitherServerAPIManager : MonoBehaviour 
{
    private const string c_PlayFlowToken = "";
    private const string c_PlayFlowStartServerURL = "https://api.cloud.playflow.app/start_game_server";
    private const string c_PlayFlowGetServerURL = "https://api.cloud.playflow.app/get_server_status";
    private const string c_PlayFlowListServersURL = "https://api.cloud.playflow.app/list_servers";
    private const string c_PlayFlowStopServerURL = "https://api.cloud.playflow.app/stop_game_server";
    private const string c_PlayFlowRegion = "us-east";
    private const string c_PlayFlowType = "small";
    private const string c_PlayFlowServerTag = "slither"; // INFO: is this even used?
    private const float c_GetRate = 10f;
    private const string c_PlayFabSetTitleDataURL = "https://36699.playfabapi.com/Server/SetTitleData";
    private const string c_PlayFabKey = "";

    private bool i_PlayFlowStartRequested;
    private float i_GetCounter;
    private string i_MatchID;
    private bool i_PlayFabTitleDataUpdateRequested;
    private bool i_PlayFabTitleDataUpdated;
    private string i_PlayFlowIP;
    private bool i_Error;
    private float i_TimeOffset;

    [SerializeField] private TextMeshProUGUI i_StatusText;
    [SerializeField] private TextMeshProUGUI i_TimeSinceStart;
    [SerializeField] private Image i_GetTimerProgress;
    [SerializeField] private Button i_StartServerButton;
    [SerializeField] private Button i_StopServerButton;
    [SerializeField] private Button i_UpdateIPButton;
    
    [Serializable]
    public class ServerList
    {
        public int total_servers;
        public List<ServerListInfo> servers;
    }
    
    [Serializable]
    public class ServerListInfo
    {
        public string match_id;
        public string status;
        public string ip;
        public string start_time;
    }
    
    [Serializable]
    public class ServerStartInfo
    {
        public string match_id;
        public string status;
    }
    
    [Serializable]
    public class ServerInfo
    {
        public string status;
        public string ip;
    }

    private void Awake()
    {
        Application.targetFrameRate = 144;
    }

    private void Start()
    {
        i_GetCounter = c_GetRate;
        i_PlayFabTitleDataUpdated = false;
        i_Error = false;
        i_TimeOffset = -1;
        
        StartCoroutine(ListPlayFlowServers());
    }

    private void Update()
    {
        if (!i_PlayFlowStartRequested || i_PlayFabTitleDataUpdated || i_Error)
        {
            return;
        }
        
        i_GetCounter -= Time.deltaTime;
        i_GetTimerProgress.fillAmount = i_GetCounter / c_GetRate;
        if (i_GetCounter <= 0)
        {
            if (!i_PlayFabTitleDataUpdateRequested)
            {
                StartCoroutine(GetPlayFlowServerStatus());
            }
            i_GetCounter = c_GetRate;
            i_GetTimerProgress.fillAmount = i_GetCounter / c_GetRate;
        }

        if (i_TimeOffset > -0.001f)
        {
            i_TimeSinceStart.text = (Time.time - i_TimeOffset).ToString("N0");
        }
    }

    private IEnumerator ListPlayFlowServers()
    {
        i_StatusText.text = "Checking server list";
        using (UnityWebRequest l_Request = UnityWebRequest.Get(c_PlayFlowListServersURL))
        {
            l_Request.SetRequestHeader("token", c_PlayFlowToken);
            l_Request.SetRequestHeader("include-launching", "true");
            
            yield return l_Request.SendWebRequest();
            
            switch (l_Request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("Error in the PlayFlow API Request", this);
                    i_StatusText.text = "Error while checking servers";
                    i_Error = true;
                    break;
                case UnityWebRequest.Result.Success:
                    string l_Json = l_Request.downloadHandler.text;
                    ServerList l_Info = JsonUtility.FromJson<ServerList>(l_Json);
                    
                    if (l_Info.total_servers == 0)
                    {
                        i_StartServerButton.interactable = true;
                        
                        i_StatusText.text = "No Server Running";
                    }
                    else
                    {
                        i_MatchID = l_Info.servers[0].match_id;
                        i_PlayFlowIP = l_Info.servers[0].ip;
                        
                        i_StopServerButton.interactable = true;
                        i_UpdateIPButton.interactable = true;
                        
                        i_StatusText.text = "Server is already Running";
                    }
                    break;
            }
        }
    }
    
    private IEnumerator StartPlayFlowServer()
    {
        i_StartServerButton.interactable = false;
        i_StatusText.text = "Starting PlayFlow server";
        
        i_PlayFabTitleDataUpdateRequested = i_PlayFabTitleDataUpdated = false;
        
        using (UnityWebRequest l_Request = UnityWebRequest.PostWwwForm(c_PlayFlowStartServerURL, ""))
        {
            l_Request.SetRequestHeader("token", c_PlayFlowToken);
            l_Request.SetRequestHeader("type", c_PlayFlowType);
            l_Request.SetRequestHeader("region", c_PlayFlowRegion);
            l_Request.SetRequestHeader("server-tag", c_PlayFlowServerTag);
            
            yield return l_Request.SendWebRequest();
            
            switch (l_Request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("Error in the PlayFlow API Request", this);
                    i_StatusText.text = "Error while starting server";
                    i_Error = true;
                    break;
                case UnityWebRequest.Result.Success:
                    i_TimeOffset = Time.time;
                    
                    string l_Json = l_Request.downloadHandler.text;
                    ServerStartInfo l_Info = JsonUtility.FromJson<ServerStartInfo>(l_Json);

                    if (l_Info.status != "launching")
                    {
                        i_StatusText.text = "Error while starting PlayFlow server";
                        yield break;
                    }

                    i_MatchID = l_Info.match_id;
                    
                    i_PlayFlowStartRequested = true;

                    i_StopServerButton.interactable = true;
                    
                    i_StatusText.text = "Started PlayFlow server";
                    break;
            }
        }
    }
    
    private IEnumerator GetPlayFlowServerStatus()
    {
        using (UnityWebRequest l_Request = UnityWebRequest.Get(c_PlayFlowGetServerURL))
        {
            l_Request.SetRequestHeader("token", c_PlayFlowToken);
            l_Request.SetRequestHeader("match-id", i_MatchID);
            
            yield return l_Request.SendWebRequest();
            
            switch (l_Request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error in the PlayFlow API Request", this);
                    i_StatusText.text = "Error while checking status";
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log("Server not found with provided Match ID, did someone stop the server while it was launching?", this);
                    i_StatusText.text = "Error while checking status";
                    i_Error = true;
                    break;
                case UnityWebRequest.Result.Success:
                    string l_Json = l_Request.downloadHandler.text;
                    ServerInfo l_Info = JsonUtility.FromJson<ServerInfo>(l_Json);

                    switch (l_Info.status)
                    {
                        case "launching":
                            i_StatusText.text = "Server still launching";
                            yield break;
                        case "running":
                            i_PlayFlowIP = l_Info.ip;
                            
                            StartCoroutine(UpdatePlayFabTitleData());
                            
                            i_GetCounter = 0;
                            break;
                    }
                    break;
            }
        }
    }
    
    private IEnumerator StopPlayFlowServer()
    {
        i_StopServerButton.interactable = false;
        i_UpdateIPButton.interactable = false;
        i_StatusText.text = "Stopping PlayFlow server";
        using (UnityWebRequest l_Request = UnityWebRequest.Delete(c_PlayFlowStopServerURL))
        {
            l_Request.SetRequestHeader("token", c_PlayFlowToken);
            l_Request.SetRequestHeader("match-id", i_MatchID);
            
            yield return l_Request.SendWebRequest();
            
            switch (l_Request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("Error in the PlayFlow API Request", this);
                    i_StatusText.text = "Error while stopping server";
                    i_Error = true;
                    break;
                case UnityWebRequest.Result.Success:
                    i_MatchID = "";
                    i_PlayFlowIP = "";
                    
                    yield return StartCoroutine(UpdatePlayFabTitleData());
                    
                    i_PlayFlowStartRequested = false;
                    
                    i_StartServerButton.interactable = true;

                    i_GetCounter = 0;
                    
                    i_TimeOffset = -1;
                    
                    yield return new WaitForSeconds(0.5f);
                    
                    i_StatusText.text = "Stopped PlayFlow server";
                    break;
            }
        }
    }
    
    private IEnumerator UpdatePlayFabTitleData()
    {
        i_PlayFabTitleDataUpdateRequested = true;
        
        i_StatusText.text = "Uploading IP to PlayFab";

        string l_Json = "{\n\"key\":\"PlayFlowIP\",\n\"value\":\"" + i_PlayFlowIP + "\"\n}";
        
        using (UnityWebRequest l_Request = UnityWebRequest.Post(c_PlayFabSetTitleDataURL, l_Json, "application/json"))
        {
            l_Request.SetRequestHeader("X-SecretKey", c_PlayFabKey);
            
            yield return l_Request.SendWebRequest();
            
            switch (l_Request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("Error in the PlayFab API Request", this);
                    i_StatusText.text = "Error while uploading IP";
                    i_Error = true;
                    break;
                case UnityWebRequest.Result.Success:
                    i_PlayFabTitleDataUpdated = true;
                    i_StatusText.text = "IP uploaded to PlayFab";
                    break;
            }
        }
    }

    public void Button_StartServer()
    {
        StartCoroutine(StartPlayFlowServer());
    }
    
    public void Button_StopServer()
    {
        StartCoroutine(StopPlayFlowServer());
    }
    
    public void Button_UpdatePlayFab()
    {
        StartCoroutine(UpdatePlayFabTitleData());
    }
}
#endif