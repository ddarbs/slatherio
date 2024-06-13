using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Utility.Performance;
using UnityEngine;
using Random = UnityEngine.Random;

public class BotManager : NetworkBehaviour
{
    private readonly Vector2 c_SpawnBounds = new Vector2(80f - 5f, 80f - 5f);
    private const float c_SpawnOpenRadius = 10f;
    private const int c_MaxBots = 0; // 7
    private const int c_MaxHuntBots = 0; // 5
    private const int c_MaxSmartBots = 10;
    private const int c_InitialBodyCache = 1000;
    
    [SerializeField] private NetworkObject i_BotPrefab, i_HuntBotPrefab, i_SmartBotPrefab, i_BodyPrefab;
    
    public static BotManager i_Instance;
    public static int i_BotCount;

    private int i_BotID = -1;
    private LayerMask i_PlayerWallLayerMask;

    private void Awake()
    {
        if (i_Instance == null)
        {
            i_Instance = this;
            i_BotCount = 0;
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogError("This shouldn't happen");
        }
        
        i_PlayerWallLayerMask = LayerMask.GetMask("Player", "MapBound");
    }
    
    
    public override void OnStartServer()
    {
        if (!IsServer)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }

        //PrewarmBodyPool();
        
        for (int i = 0; i < c_MaxBots; i++)
        {
            SpawnBot();
            i_BotID--;
            i_BotCount++;
        }
        for (int i = 0; i < c_MaxHuntBots; i++)
        {
            SpawnHuntBot();
            i_BotID--;
            i_BotCount++;
        }
        for (int i = 0; i < c_MaxSmartBots; i++)
        {
            SpawnSmartBot();
            i_BotID--;
            i_BotCount++;
        }
    } 
    
    
    private void PrewarmBodyPool() 
    {
        DefaultObjectPool pool = InstanceFinder.NetworkManager.GetComponent<DefaultObjectPool>();
        pool.CacheObjects(i_BodyPrefab, c_InitialBodyCache, IsServer);
    }

    public static void OnBotDeath() // TODO: need to have a list that contains the bots, and they remove themselves from it on death, will help when we want to kill bots if new players join
    {
        i_Instance.SpawnBot();
        i_Instance.i_BotID--;
    }
    
    public static void OnHuntBotDeath() // TODO: need to have a list that contains the bots, and they remove themselves from it on death, will help when we want to kill bots if new players join
    {
        i_Instance.SpawnHuntBot();
        i_Instance.i_BotID--;
    }
    
    public static void OnSmartBotDeath() // TODO: need to have a list that contains the bots, and they remove themselves from it on death, will help when we want to kill bots if new players join
    {
        i_Instance.SpawnSmartBot();
        i_Instance.i_BotID--;
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsServer)
        {
            return;
        }

        /*if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnBot();
        }*/
    }

    private void SpawnBot()
    {
        NetworkObject l_Part = Instantiate(i_BotPrefab, GetOpenSpawnPoint(), Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f)));
        //l_Part.transform.position = GetSpawnPoint();
        
        ServerManager.Spawn(l_Part, base.Owner);
        
        l_Part.GetComponent<SnakeBotController>().SetupBot(i_BotID);
    }
    
    private void SpawnHuntBot()
    {
        NetworkObject l_Part = Instantiate(i_HuntBotPrefab, GetOpenSpawnPoint(), Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f)));
        //l_Part.transform.position = GetSpawnPoint();
        
        ServerManager.Spawn(l_Part, base.Owner);
        
        l_Part.GetComponent<SnakeHuntBotController>().SetupBot(i_BotID);
    }
    
    private void SpawnSmartBot()
    {
        NetworkObject l_Part = Instantiate(i_SmartBotPrefab, GetOpenSpawnPoint(), Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f)));
        //l_Part.transform.position = GetSpawnPoint();
        
        ServerManager.Spawn(l_Part, base.Owner);
        
        l_Part.GetComponent<SnakeSmartBotController>().SetupBot(i_BotID);
    }
    
    private Vector2 GetSpawnPoint()
    {
        Vector2 l_SpawnPoint = new Vector2(Random.Range(-c_SpawnBounds.x, c_SpawnBounds.x), Random.Range(-c_SpawnBounds.y, c_SpawnBounds.y));
        
        return l_SpawnPoint;
    }
    
    private Vector3 GetOpenSpawnPoint()
    {
        Vector3 l_SpawnPoint = Vector3.zero;
        Collider2D[] l_Colliders = new Collider2D[5];
        int l_Collisions = 69;
        float l_SpawnOpenRadius = c_SpawnOpenRadius;
        
        while (l_Collisions > 0)
        {
            l_SpawnPoint = new Vector3(Random.Range(-c_SpawnBounds.x, c_SpawnBounds.x), Random.Range(-c_SpawnBounds.y, c_SpawnBounds.y), 0f);
            l_Collisions = Physics2D.OverlapCircleNonAlloc(l_SpawnPoint, l_SpawnOpenRadius, l_Colliders, i_PlayerWallLayerMask);
            
            l_SpawnOpenRadius -= 0.05f;
            
            if (l_SpawnOpenRadius == 0f)
            {
                return Vector3.zero;
            }
        }
        
        return l_SpawnPoint;
    }
}
