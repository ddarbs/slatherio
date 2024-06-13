using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class SlitherClientAPIManager : MonoBehaviour
{
    private const string c_LifetimeLengthStatistic = "Lifetime Total Length Leaderboard";
    private const string c_LongestLengthStatistic = "Longest Snakes Leaderboard";
    private const float c_APIWaitRate = 0.25f;
    
    //private int i_PlayerLifetimeLength = 0;
    //private int i_PlayerLongestLength = 0;
    
    //private int i_PullCount = 0;
    private bool i_Pushed = false;
    
    public void OnPlayerDeath(int _length)
    {
        //i_PullCount = 0;
        i_Pushed = false;
        #if !UNITY_SERVER
        StartCoroutine(RequestUpdateLeaderboard(_length));
        #endif
    }

    private IEnumerator RequestUpdateLeaderboard(int _length)
    {
        SubmitScores(_length);
        
        // INFO: wait for both of the requests to be finished
        float l_Waited = 0f;
        while (!i_Pushed)
        {
            yield return new WaitForSeconds(c_APIWaitRate);
            
            l_Waited += c_APIWaitRate;

            if (l_Waited > 5f)
            {
                i_Pushed = true;
            }
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    
    public void SubmitScores(int _value) 
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest {
            Statistics = new List<StatisticUpdate> { 
                new StatisticUpdate 
                {
                    StatisticName = c_LifetimeLengthStatistic,
                    Value = _value
                },
                new StatisticUpdate 
                {
                    StatisticName = c_LongestLengthStatistic,
                    Value = _value
                },
            }
        }, result=> OnStatisticsUpdated(result), FailureCallback);
    }
    
    /*public void RequestPlayerLifetimeLeaderboard() {
        PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = c_LifetimeLengthStatistic,
            
        }, result=> OnPlayerLifetimeLeaderboardRetrieved(result), FailureCallback);
    }
    
    public void RequestPlayerLongestLeaderboard() {
        PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = c_LongestLengthStatistic,
            
        }, result=> OnPlayerLongestLeaderboardRetrieved(result), FailureCallback);
    }*/

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult _result) 
    {
        i_Pushed = true;
    }
    
    /*private void OnPlayerLifetimeLeaderboardRetrieved(GetLeaderboardAroundPlayerResult _result) 
    {
        i_PlayerLifetimeLength = _result.Leaderboard[0].StatValue;
        i_PullCount++;
    }
    
    private void OnPlayerLongestLeaderboardRetrieved(GetLeaderboardAroundPlayerResult _result) 
    {
        i_PlayerLongestLength = _result.Leaderboard[0].StatValue;
        i_PullCount++;
    }*/
    

    private void FailureCallback(PlayFabError error){
        Debug.LogWarning("Something went wrong with your API call. Here's some debug information:");
        Debug.LogError(error.GenerateErrorReport());
        StopAllCoroutines();
    }
}
