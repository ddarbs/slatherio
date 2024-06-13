using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class PlayFabLeaderboardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] i_LifetimeNames = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI[] i_LifetimeScores = new TextMeshProUGUI[5];
    
    [SerializeField] private TextMeshProUGUI[] i_LongestNames = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI[] i_LongestScores = new TextMeshProUGUI[5];
    
    public void OnPlayFabLogin()
    {
        RequestLifetimeLeaderboard();
        RequestLongestLeaderboard();
    }
    
    public void RequestLifetimeLeaderboard() {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest {
            StatisticName = "Lifetime Total Length Leaderboard",
            StartPosition = 0,
            MaxResultsCount = 5
        }, result=> OnLifetimeLeaderboardRetrieved(result), FailureCallback);
    }
    
    public void RequestLongestLeaderboard() {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest 
        {
            StatisticName = "Longest Snakes Leaderboard",
            StartPosition = 0,
            MaxResultsCount = 5
        }, result=> OnLongestLeaderboardRetrieved(result), FailureCallback);
    }
    
    private void OnLifetimeLeaderboardRetrieved(GetLeaderboardResult _result) 
    {
        for (int i = 0; i < _result.Leaderboard.Count; i++)
        {
            i_LifetimeNames[i].text = _result.Leaderboard[i].DisplayName;
            i_LifetimeScores[i].text = _result.Leaderboard[i].StatValue.ToString();
        }
    }
    
    private void OnLongestLeaderboardRetrieved(GetLeaderboardResult _result) 
    {
        for (int i = 0; i < _result.Leaderboard.Count; i++)
        {
            i_LongestNames[i].text = _result.Leaderboard[i].DisplayName;
            i_LongestScores[i].text = _result.Leaderboard[i].StatValue.ToString();
        }
    }
    

    private void FailureCallback(PlayFabError error){
        Debug.LogWarning("Something went wrong with your API call. Here's some debug information:");
        Debug.LogError(error.GenerateErrorReport());
    }
}
