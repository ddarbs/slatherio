using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerSpawnManager : MonoBehaviour
{
    private readonly Vector2 c_SpawnBounds = new Vector2(80f, 80f);
    private const float c_SpawnOpenRadius = 10f;
    
    public event Action<NetworkObject> OnSpawned; // INFO: if we want to run code outside of the player after the player spawns in

    [SerializeField] private NetworkObject i_PlayerPrefab;
    
    private NetworkManager i_NetworkManager;
    private LayerMask i_PlayerWallLayerMask;
    
    private void Start()
    {
        i_NetworkManager = InstanceFinder.NetworkManager;
        
        if (i_NetworkManager == null)
        {
            Debug.LogError("Couldn't find a NetworkManager", this);
            return;
        }
        if (i_PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab isn't assigned", this);
            return;
        }
        
        i_PlayerWallLayerMask = LayerMask.GetMask("Player", "MapBound");
        
        i_NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
    }

    private void OnDisable()
    {
        if (i_NetworkManager != null && i_PlayerPrefab != null)
        {
            i_NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        }
    }

    private void OnClientLoadedStartScenes(NetworkConnection _conn, bool _server)
    {
        if (!_server)
        {
            return;
        }

        NetworkObject l_Player = Instantiate(i_PlayerPrefab, GetOpenSpawnPoint(), Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f)));
        //NetworkObject l_Player = i_NetworkManager.GetPooledInstantiated(i_PlayerPrefab, GetOpenSpawnPoint(), Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f)), _server);
        
        i_NetworkManager.ServerManager.Spawn(l_Player, _conn);
        i_NetworkManager.SceneManager.AddOwnerToDefaultScene(l_Player); // INFO: is this needed?
        
        OnSpawned?.Invoke(l_Player);
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
            
            l_SpawnOpenRadius -= 0.5f;
            
            if (l_SpawnOpenRadius == 0f)
            {
                return Vector3.zero;
            }
        }
        
        return l_SpawnPoint;
    }
}
