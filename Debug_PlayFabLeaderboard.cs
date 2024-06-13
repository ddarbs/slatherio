using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class Debug_PlayFabLeaderboard : MonoBehaviour
{
    // INFO: https://learn.microsoft.com/en-us/gaming/playfab/features/social/tournaments-leaderboards/quickstart
    // on death add to lifetime total length leaderboard summation
    // on death check to see if your death length is larger than your longest snakes leaderboard
    // on color menu scene, pull leaderboard top 5 in both categories
    
    public void SubmitScore(int playerScore) 
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest {
            Statistics = new List<StatisticUpdate> { 
                new StatisticUpdate 
                {
                    StatisticName = "Lifetime Total Length Leaderboard",
                    Value = playerScore
                }, 
                new StatisticUpdate 
                {
                    StatisticName = "Longest Snakes Leaderboard",
                    Value = playerScore * 2
                }, 
            }
        }, result=> OnStatisticsUpdated(result), FailureCallback);
    }
    
    public void RequestLifetimeLeaderboard() {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest {
            StatisticName = "Lifetime Total Length Leaderboard",
            StartPosition = 0,
            MaxResultsCount = 10
        }, result=> OnLeaderboardRetrieved(result), FailureCallback);
    }
    
    public void RequestLongestLeaderboard() {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest 
        {
            StatisticName = "Longest Snakes Leaderboard",
            StartPosition = 0,
            MaxResultsCount = 10
        }, result=> OnLeaderboardRetrieved(result), FailureCallback);
    }

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult _result) 
    {
        Debug.Log("Successfully submitted high score");
    }
    
    private void OnLeaderboardRetrieved(GetLeaderboardResult _result) 
    {
        Debug.Log("Successfully retrieved leaderboard");
    }

    private void FailureCallback(PlayFabError error){
        Debug.LogWarning("Something went wrong with your API call. Here's some debug information:");
        Debug.LogError(error.GenerateErrorReport());
    }
}
