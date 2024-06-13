using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using Random = UnityEngine.Random;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using FishNet.Utility.Performance;

public class FoodSpawner : NetworkBehaviour // INFO: everything here is only run on the server
{
    private const int c_InitialFood = 250;
    private const float c_SpawnRate = 1f; 
    private const float c_SFSpawnRate = 15f;
    private const int c_MaxFood = 300;
    private const int c_MaxSuperFood = 5; 
    private const float c_SpawnRatePlayerModifier = 0.01f; // INFO: spawnrate / (1 + modifier * player count) 
    private const float c_SFSpawnRatePlayerModifier = 0.005f; // INFO: sf spawnrate / (1 + modifier * player count) 
    private const int c_MaxFoodBonusPerPlayer = 10; // INFO: bonus * player count + max food
    private const int c_MaxSuperFoodBonusPerPlayer = 1; // INFO: bonus * player count + max super food
    private readonly Vector2 c_SpawnBounds = new Vector2(80f - 2f, 80f - 2f); 
    private readonly Vector2 c_SpawnBoundsSuperFood = new Vector2(30f, 30f); 
    
    private bool i_Active;
    private int i_ActiveFood;
    private int i_ActiveSuperFood;// INFO: only set by ServerRPC calls
    private float i_SpawnRate = 0f;
    private float i_SFSpawnRate = 0f;
    private int i_MaxFood = 0;
    private int i_MaxSuperFood = 0;

    public static FoodSpawner i_Instance;

    [SerializeField] private NetworkObject i_FoodPrefab;
    [SerializeField] private NetworkObject i_SuperFoodPrefab;

    public override void OnStartServer()
    {
        if (!IsServer)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }
        
        if (i_Instance == null)
        {
            i_Instance = this;
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogError("This shouldn't happen");
        }
        
        PrewarmPools();

        UpdateSettings(BotManager.i_BotCount); // INFO: BotManager currently initializes before FoodSpawner on the server, so we can grab the count like this
        
        i_Active = true;
        
        StartCoroutine(SpawnInitialFoodThread());
        StartCoroutine(SpawnFoodThread());
        StartCoroutine(SpawnSuperFoodThread());
        
        ServerManager.OnRemoteConnectionState += OnPlayerConnectionChange; // INFO: shouldn't be doing this here but it works cause it's the server and I'm lazy rn
    }
    
    private void OnPlayerConnectionChange(NetworkConnection _conn, RemoteConnectionStateArgs _args)
    {
        switch (_args.ConnectionState)
        {
            case RemoteConnectionState.Started:
                UpdateSettings(BotManager.i_BotCount + ServerManager.Clients.Count); 
                break;
            case RemoteConnectionState.Stopped:
                //LeaderBoard.RequestClearScore(_conn, true);
                LeaderBoard.RequestTimeoutClearScore(_conn);
                UpdateSettings(BotManager.i_BotCount + ServerManager.Clients.Count - 1); // INFO: it still registers the disconnecting person as a client at this point
                break;                                                                          // INFO: if we setup login auth we can have that store the # of active clients and ref that instead
                
        }
    }

    private void UpdateSettings(int _count)
    {
        // INFO: update the maxes and rates
        i_SpawnRate = c_SpawnRate / (1f + c_SpawnRatePlayerModifier * _count);
        i_SFSpawnRate = c_SFSpawnRate / (1f + c_SFSpawnRatePlayerModifier * _count);
        i_MaxFood = c_MaxFoodBonusPerPlayer * _count + c_MaxFood;
        i_MaxSuperFood = c_MaxSuperFoodBonusPerPlayer * _count + c_MaxSuperFood;
    }
    
    private void PrewarmPools() 
    {
        DefaultObjectPool pool = InstanceFinder.NetworkManager.GetComponent<DefaultObjectPool>();
        pool.CacheObjects(i_FoodPrefab, c_MaxFood, IsServer);
        pool.CacheObjects(i_SuperFoodPrefab, c_MaxSuperFood, IsServer);
    }

    private void OnDisable()
    {
        i_Active = false;
    }


    public static void OnFoodChange(int _change)
    {
        i_Instance.i_ActiveFood += _change;
        
        StatsDisplay.OnFoodChange(i_Instance.i_ActiveFood);
    }
    public static void OnSuperFoodChange(int _change)
    {
        i_Instance.i_ActiveSuperFood += _change;
    }
    
    private IEnumerator SpawnInitialFoodThread() // INFO: spawn bunch of food when server starts
    {
        int l_Index = 0;
        while (l_Index < c_InitialFood)
        {
            yield return new WaitForEndOfFrame();
            
            NetworkObject l_Food = NetworkManager.GetPooledInstantiated(i_FoodPrefab, GetSpawnPoint(), Quaternion.identity, InstanceFinder.IsServer);
            ServerManager.Spawn(l_Food);
            OnFoodChange(1); 
            
            l_Index++;
        }
    }

    /*[ObserversRpc] // INFO: this only works if client is already connected, not used in initial spawn thread cause no one will be connected when server runs that
    private void SetInitialColor(NetworkObject l_Food)
    {
        l_Food.GetComponent<FoodColorHandler>().AssignColor();
    }*/ // DEBUG: testing doing this through OnEnable
    
    private IEnumerator SpawnFoodThread() // INFO: continuously spawn food when under the food limit
    {
        while (i_Active)
        {
            yield return new WaitForSeconds(i_SpawnRate);
            if (i_ActiveFood < i_MaxFood)
            {
                NetworkObject l_Food = NetworkManager.GetPooledInstantiated(i_FoodPrefab, GetSpawnPoint(), Quaternion.identity, InstanceFinder.IsServer);
                ServerManager.Spawn(l_Food);
                OnFoodChange(1);
                //SetInitialColor(l_Food);
            }
        }
    }
    
    private IEnumerator SpawnSuperFoodThread() // INFO: continuously spawn food when under the food limit
    {
        while (i_Active)
        {
            yield return new WaitForSeconds(i_SFSpawnRate);
            if (i_ActiveSuperFood < i_MaxSuperFood)
            {
                NetworkObject l_Food = NetworkManager.GetPooledInstantiated(i_SuperFoodPrefab, GetSuperFoodSpawnPoint(), Quaternion.identity, InstanceFinder.IsServer);
                ServerManager.Spawn(l_Food);
                OnSuperFoodChange(1);
            }
        }
    }

    public static Vector2 GetSuperFoodSpawnPoint()
    {
        Vector2 l_SpawnPoint = new Vector2(Random.Range(-i_Instance.c_SpawnBoundsSuperFood.x, i_Instance.c_SpawnBoundsSuperFood.x), Random.Range(-i_Instance.c_SpawnBoundsSuperFood.y, i_Instance.c_SpawnBoundsSuperFood.y));
        return l_SpawnPoint;
    }
    private Vector2 GetSpawnPoint()
    {
        Vector2 l_SpawnPoint = new Vector2(Random.Range(-c_SpawnBounds.x, c_SpawnBounds.x), Random.Range(-c_SpawnBounds.y, c_SpawnBounds.y));
        
        return l_SpawnPoint;
    }
}
