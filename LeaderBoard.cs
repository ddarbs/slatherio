using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class LeaderBoard : NetworkBehaviour
{
    private const int c_MaxScoresDisplayed = 5;
    
    [SerializeField] private List<TextMeshProUGUI> Leaders = new List<TextMeshProUGUI>();
    public List<string> playerNames = new List<string>();
    public List<int> playerScores = new List<int>();
    private int myScore;
    private string myName;

    private class LeaderboardEntry
    {
        public string p_Name;
        public int p_Score;
    }
    private Dictionary<int, LeaderboardEntry> i_ScoreDictionary = new Dictionary<int, LeaderboardEntry>();
    //private Dictionary<NetworkConnection, LeaderboardEntry> i_ScoreDictionary = new Dictionary<NetworkConnection, LeaderboardEntry>();
    private string[] i_SortedNames = new string[5];
    private int[] i_SortedScores = new int[5];
    private int i_CurrentScoresDisplayed;
    public static bool p_SafeToDisconnect = false;
    
    public static LeaderBoard i_Instance;
    
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

    #region doobies
    private void OnEnable()
    {
        p_SafeToDisconnect = false;
    }
    
    /*public override void OnStartServer()
    {
        if (!base.IsServer)
        {
            return;
        }
        ServerManager.OnRemoteConnectionState += ClearStoppedConnections;
    }
    
    public override void OnStopServer()
    {
        if (!base.IsServer)
        {
            return;
        }
        ServerManager.OnRemoteConnectionState -= ClearStoppedConnections;
    }
    
    private void ClearStoppedConnections(NetworkConnection _conn, RemoteConnectionStateArgs _args)
    {
        if (_args.ConnectionState is RemoteConnectionState.Stopped)
        {
            Debug.Log(_conn.ClientId);
            ClearScoreOnDeath(_conn, true);
            Debug.Log(_conn.ClientId);
        }
    }*/

    public static void RequestSendServerScore(NetworkConnection _conn, string _name, int _score)
    {
        i_Instance.SendServerScore(_conn, _name, _score);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendServerScore(NetworkConnection _conn, string _name, int _score)
    {
        if (!i_ScoreDictionary.ContainsKey(_conn.ClientId))
        {
            i_ScoreDictionary.Add(_conn.ClientId, new LeaderboardEntry { p_Name = _name, p_Score = _score} );
            //Debug.Log("added new connection to leaderboard");
        }
        else
        {
            i_ScoreDictionary[_conn.ClientId].p_Score = _score;
            //Debug.Log("updated existing connection in leaderboard");
        }

        SortTopFive();
        ReturnLeaderBoard(i_SortedNames, i_SortedScores, i_CurrentScoresDisplayed);
    }
    
    public static void RequestSendServerScore(int _botID, string _name, int _score)
    {
        i_Instance.SendServerScore(_botID, _name, _score);
    }

    private void SendServerScore(int _botID, string _name, int _score)
    {
        if (!i_ScoreDictionary.ContainsKey(_botID))
        {
            i_ScoreDictionary.Add(_botID, new LeaderboardEntry { p_Name = _name, p_Score = _score} );
            //Debug.Log("added new connection to leaderboard");
        }
        else
        {
            i_ScoreDictionary[_botID].p_Score = _score;
            //Debug.Log("updated existing connection in leaderboard");
        }

        SortTopFive();
        ReturnLeaderBoard(i_SortedNames, i_SortedScores, i_CurrentScoresDisplayed);
    }
    
    private void SortTopFive() // INFO: only server uses this 
    {
        var l_SortedDictionary = (from entry in i_ScoreDictionary orderby entry.Value.p_Score descending select entry).Take(c_MaxScoresDisplayed);
        
        int _index = 0;
        foreach (var _entry in l_SortedDictionary)
        {
            i_SortedNames[_index] = _entry.Value.p_Name;
            i_SortedScores[_index] = _entry.Value.p_Score;
            _index++;
        }

        i_CurrentScoresDisplayed = _index;
    }
    
    [ObserversRpc(BufferLast = true)]
    private void ReturnLeaderBoard(string[] _sortedNames, int[] _sortedScores, int _scoresDisplayed)
    {
        for (int i = 0; i < c_MaxScoresDisplayed; i++)
        {
            Leaders[i].text = $"{_sortedNames[i]} - {_sortedScores[i]}";
        }

        if (_scoresDisplayed < c_MaxScoresDisplayed)
        {
            for (int i = _scoresDisplayed; i < c_MaxScoresDisplayed; i++)
            {
                Leaders[i].text = "";
            }
        }
    }
    
    public static void RequestTimeoutClearScore(NetworkConnection _conn) // TODO: need a way to call this when disconnecting, OnStopClient is too late
    {
        i_Instance.TimeoutClearScore(_conn);
    }
    
    private void TimeoutClearScore(NetworkConnection _conn) // INFO: if connection cuts off too fast, then this won't call
    {
        if (!i_ScoreDictionary.ContainsKey(_conn.ClientId))
        {
            return;
        }
        
        i_ScoreDictionary.Remove(_conn.ClientId);
        
        SortTopFive();
        ReturnLeaderBoard(i_SortedNames, i_SortedScores, i_CurrentScoresDisplayed);
    }
    
    public static void RequestClearScore(NetworkConnection _conn) // TODO: need a way to call this when disconnecting, OnStopClient is too late
    {
        i_Instance.ClearScoreOnDeath(_conn);
    }
    
    public static void RequestClearScore(int _botID) // TODO: need a way to call this when disconnecting, OnStopClient is too late
    {
        i_Instance.ClearScoreOnDeath(_botID);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ClearScoreOnDeath(NetworkConnection _conn) // INFO: if connection cuts off too fast, then this won't call
    {
        if (!i_ScoreDictionary.ContainsKey(_conn.ClientId))
        {
            return;
        }
        
        i_ScoreDictionary.Remove(_conn.ClientId);
        
        TargetClearScoreOnDeath(_conn);
        
        SortTopFive();
        ReturnLeaderBoard(i_SortedNames, i_SortedScores, i_CurrentScoresDisplayed);
    }
    
    private void ClearScoreOnDeath(int _botID) // INFO: if connection cuts off too fast, then this won't call
    {
        if (!i_ScoreDictionary.ContainsKey(_botID))
        {
            return;
        }
        
        i_ScoreDictionary.Remove(_botID);
        
        SortTopFive();
        ReturnLeaderBoard(i_SortedNames, i_SortedScores, i_CurrentScoresDisplayed);
    }

    [TargetRpc]
    private void TargetClearScoreOnDeath(NetworkConnection _conn)
    {
        p_SafeToDisconnect = true;
    }
    #endregion doobies
  
    //NO REPEAT NAMES
    //WON'T WORK WITH BOZO'S FOR SOME REASON
    //WHEN DO WE WANT TO CALL THIS?
    //need a way to clear list on server occasionally
    [ServerRpc(RequireOwnership = false)]
    public void SendServerScore(String pName, int Score)
    {
        if (playerNames.Contains(pName))
        {
            int index = playerNames.IndexOf(pName);
            playerScores[index] = Score;
        }
        else
        {
            playerNames.Add(pName);
            playerScores.Add(Score);
        }
        
        ReturnLeaderBoard(playerNames, playerScores);
    }

    [ObserversRpc(BufferLast = true)]
    public void ReturnLeaderBoard(List<string> names, List<int> scores)
    {
        //sort scores
        for (int j = 0; j < scores.Count; j++)
        {
            for (int i = 0; i < scores.Count - 1; i++)
            {
                if (scores[i] < scores[i + 1])
                {
                    int temp = scores[i];
                    scores[i] = scores[i + 1];
                    scores[i + 1] = temp;

                    string temp1 = names[i];
                    names[i] = names[i + 1];
                    names[i + 1] = temp1;
                }
            }
        }

        if (scores.Count >= 5)
        {
            Leaders[0].text = names[0] + " " + scores[0];
            Leaders[1].text = names[1] + " " + scores[1];
            Leaders[2].text = names[2] + " " + scores[2];
            Leaders[3].text = names[3] + " " + scores[3];
            Leaders[4].text = names[4] + " " + scores[4];
        }
        else
        {
            for (int i = 0; i < scores.Count; i++)
            {
                Leaders[i].text = names[i] + " " + scores[i];
            }
        }
    }
    
    //pass in i_Instance.myName, techincally people could use this method to reset other ppl's scores.
    //for death
    [ServerRpc(RequireOwnership = false)]
    private void ClearScoreOnDeath(String Pname)
    {
       int i = playerNames.IndexOf(Pname);
       playerScores[i] = 3;
    }
    
    /*private void Update()
    {
        if (IsClient && Input.GetKeyDown(KeyCode.P))
        {
            SendServerScore(i_Instance.myName, i_Instance.myScore);
        }
    }*/

    public static void OnScoreChange(int _length)
    {
        i_Instance.myScore = _length;
    }
    
    public static void OnNameSet(String Name)
    {
        i_Instance.myName = Name;
    }
    
}
