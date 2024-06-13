using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class SnakeController : NetworkBehaviour
{
    /* TODO general list in no specific order
     
     - (done) leaderboard with top x worms alive length
     - (done) add up time spent boosting per fixedupdate, every x remove one length 
     - (done) change food to use pooling instead of creating/destroying new ones all the time
     - (done) extended map size/map edges kill you on contact
     - (done) change body parts to use pooling instead of creating/destroying new ones all the time
     - (done) basic bots
     - (done) food spawner spawns based on player count 
     - (done) random spawnpoint for players that checks a circle around to make sure no one is nearby
     - (done) playfab api call after login grabs/stores playflow server ip from a playfab variable
     - (done) tie turning speed to size, big worm shouldn't be able to turn that fast
     - (done) skins/pickable worm colors
     - (done) make the big food that flies around, tie with food spawner for count to have alive
     - (done) starting playflow server and posting the ip to playfab
     - (done) login and playfab auth system
     - (done) sending/retrieving whatever data we want associated with playfab accounts 
        -- (done) lifetime top length and total length leaderboards
        -- (done) updating display name to last used name
          - (done)freaky sounds on buttons and fields, lots of screams.
     
     - (mostly done) ui (main menu, ingame, dying)
     
     - (wip) test/polish
        -- (done) snake body bug, turned off body pooling for now
        -- (done) double food bug
        -- body parts instatiating at 0,0 for a frame when they're never set to instantiate there, not sure if bot body part specific or not 
     
     - a way to clearly tell your worm body from enemy worm body (besides color)
     - a version check against playfab when logging in to stop old version clients from playing
     
     - (skipping) spectator client? no playfab just playflow and can change camera to follow diff snakes/bots, or free cam the map
     - (skipping) look into fixing both players dying from head on collisions?
     - (skipping) make stackable food/make food combine up to a certain point
    */
    
    // Settings
    private const float c_BaseSpeed = 4;
    private const float c_BoostSpeed = 6;
    private const float c_CollisionRate = 0.02f; // INFO : 0.02 is the fixed timestep
    
    //steer speed settings
    private float c_SteerSpeed = 180f;
    private const float c_SteerSpeed_Max = 180f;
    private const float c_SteerSpeed_Min = 90f;
    public float decayRate = 0.01f;
    public float lengthMultiplier = 0.01f;
    
    
    private const int c_Gap = 5;
    private const float c_BodyScaling = 0.0025f;
    private const float c_CameraScaling = 0.01f;
    [Range(1, 10)] private const int c_DropFoodRate = 3; // INFO: 1 / i_DropFoodRate chance to drop a food on death
    private const float c_SpawnVolume = 0.5f;
    private const float c_GrowVolume = 0.2f;
    private const float c_DeathVolume = 0.8f;
    private const float c_BoostDropFoodTime = 2f; // INFO: time spent boosting to drop your last part as food 
    private const int c_MinLength = 3; // INFO: starter length and length it stops you from boosting
    
    // Managed variables 
    private float i_HeadRadius = 0;
    private float i_CurrentSpeed = 0;
    private int i_Length = 0;
    private int i_CleanCounter = 0;
    private bool i_Alive;
    private List<GameObject> i_BodyParts = new List<GameObject>();
    private List<Vector3> i_PositionsHistory = new List<Vector3>();
    private Dictionary<Collider2D, bool> i_CollisionTest = new Dictionary<Collider2D, bool>();
    private Quaternion i_NoRotation = new Quaternion();
    private bool i_Boosting;
    private float i_BoostTime = 0f;
    private LayerMask i_PlayerWallLayerMask;
    private LayerMask i_FoodLayerMask;
    
    // References
    [SerializeField] private GameObject i_BodyPrefab;
    [SerializeField] private NetworkObject i_FoodPrefab;
    [SerializeField] private TextMeshPro i_NameText;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource i_Audio;
    [SerializeField] private AudioClip i_SpawnSFX, i_GrowSFX, i_DeathSFX;
    [SerializeField] private PlayerData i_PlayerData;
    [SerializeField] private SlitherClientAPIManager i_ClientAPI;
    
    private void Awake()
    {
        i_NoRotation = Quaternion.Euler(0, 0, 0);
        i_FoodLayerMask = LayerMask.GetMask("Food");
        i_PlayerWallLayerMask = LayerMask.GetMask("Player", "MapBound", "PlayerBoosting");
        i_ClientAPI = GameObject.FindWithTag("SlitherClientManager").GetComponent<SlitherClientAPIManager>();
    }
    
    public override void OnStartClient()
    {
        //base.OnStartClient();
        this.enabled = IsOwner;
        
        if (base.IsOwner)
        {
            Application.targetFrameRate = 60;

            i_PlayerData.p_ClientID = base.LocalConnection.ClientId;
            
            ServerSetName(base.LocalConnection, i_PlayerData.p_CurrentName);
            SpawnSnake();
            
            playerCamera.orthographicSize = 5f;
            playerCamera.gameObject.SetActive(true);
            
            i_HeadRadius = transform.localScale.x / 2f;
            
            playerCamera.transform.rotation = i_NameText.transform.rotation = i_NoRotation;
            
            i_Alive = true;
            
            StartCoroutine(CollisionThread());
        }
    }
    
    private void Update() // INFO: clean up the position list and watch for ui stuff
    {
        if (!base.IsOwner || !i_Alive)
        {
            return;
        }
        
        i_CleanCounter++;
        if (i_CleanCounter == 60)
        {
            /*// DEBUG:
            Debug.Log($"Pos History {i_PositionsHistory.Count}");
            Debug.Log($"Body Parts {i_BodyParts.Count}");
            Debug.Log($"Collision {i_CollisionTest.Count}");
            // DEBUG*/
            CleanPositionHistory();
            i_CleanCounter = 0;
        }
        
        if (Input.GetMouseButton(0) && i_Length > c_MinLength) // INFO: boosting
        {
            /*if (!i_Boosting)
            {
                ServerBoostStatus(true, i_BodyParts);
                foreach (var _part in i_BodyParts)
                {
                    _part.layer = LayerMask.NameToLayer("PlayerBoosting");
                }
            }*/
            i_Boosting = true;
            i_CurrentSpeed = c_BoostSpeed;
        }
        else
        {
            /*if (i_Boosting)
            {
                ServerBoostStatus(false, i_BodyParts);
                foreach (var _part in i_BodyParts)
                {
                    _part.layer = LayerMask.NameToLayer("Player");
                }
            }*/
            i_Boosting = false;
            i_CurrentSpeed = c_BaseSpeed;
        }
        
        /*if (Input.GetMouseButton(1)) // DEBUG: spawning
        {
            GrowSnake();
        }*/
    }

    [ServerRpc]
    private void ServerBoostStatus(bool _boosting, List<GameObject> _bodyParts)
    {
        ObserverBoostStatus(_boosting, i_BodyParts);
    }
    
    [ObserversRpc(ExcludeOwner = true)]
    private void ObserverBoostStatus(bool _boosting, List<GameObject> _bodyParts)
    {
        if (_boosting)
        {
            foreach (var _part in _bodyParts)
            {
                _part.layer = LayerMask.NameToLayer("PlayerBoosting");
            }
        }
        else
        {
            foreach (var _part in _bodyParts)
            {
                _part.layer = LayerMask.NameToLayer("Player");
            }
        }
    }

    private void FixedUpdate() // INFO: keep stuff from being reliant on fps by having physics done in fixedupdate
    {
        if (!base.IsOwner || !i_Alive)
        {
            return;
        }
        
        // INFO: turning
        Transform l_Transform = transform;
        Vector3 l_TransPos = l_Transform.position;
        Vector3 l_MousePos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        
        Vector2 direction = l_MousePos - l_TransPos;
        float angle = Vector2.SignedAngle(Vector2.up, direction);

        Vector3 targetRotation = new Vector3(0, 0, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(targetRotation), c_SteerSpeed * Time.fixedDeltaTime);
        
        // INFO: moving
        l_Transform.position += l_Transform.up * (i_CurrentSpeed * Time.deltaTime);
        l_TransPos = l_Transform.position;
        i_PositionsHistory.Insert(0, l_TransPos);
        l_TransPos.z = -10f;
        playerCamera.transform.position = l_TransPos;
        playerCamera.transform.rotation = i_NameText.transform.rotation = i_NoRotation;
        i_NameText.transform.position = i_PositionsHistory[Mathf.Clamp(c_Gap * 3, 0, i_PositionsHistory.Count - 1)];
        
        // INFO: moving body parts
        int index = 0;
        foreach (GameObject _body in i_BodyParts) {
            Vector3 point = i_PositionsHistory[Mathf.Clamp(index * c_Gap, 0, i_PositionsHistory.Count - 1)];

            // Move body towards the point along the snakes path
            Vector3 moveDirection = point - _body.transform.position;
            _body.transform.position += moveDirection * (i_CurrentSpeed * Time.fixedDeltaTime);

            index++;
        }
        
        // INFO: drop food if you boost for too long
        if (i_Boosting)
        {
            i_BoostTime += Time.fixedDeltaTime;

            if (i_BoostTime >= c_BoostDropFoodTime)
            {
                DestroyLastPart();
                i_BoostTime = 0;
            }
        }
    }
    
    [ServerRpc]
    private void ServerSetName(NetworkConnection _conn, string _name)
    {
        if (_name == "")
        {
            _name = "bozo_" + Random.Range(0, 69420);
        }
        i_NameText.text = _name;
        
        ObserverSetName(_name);
        TargetSetName(_conn, _name);
    }

    [ObserversRpc(BufferLast = true, ExcludeOwner = true)]
    private void ObserverSetName(string _name)
    {
        i_NameText.text = _name;
    }
    [TargetRpc] 
    private void TargetSetName(NetworkConnection _conn, string _name)
    {
        i_PlayerData.p_CurrentName = _name;
        //LeaderBoard.OnNameSet(_name);
    }

    private void SpawnSnake() // INFO: spawn initial body parts without setting position
    {
        ServerSpawnSnake(base.LocalConnection, GetColor());
    }

    [ServerRpc]
    private void ServerSpawnSnake(NetworkConnection _connection, Color _snakeColor)
    {
        if (!IsServer)
        {
            return;
        }

        GameObject l_Part = default;
        for (int i = 0; i < c_MinLength; i++)
        {
            //l_Part = Instantiate(i_BodyPrefab);
            l_Part = Instantiate(i_BodyPrefab, transform.position, Quaternion.identity);
            l_Part.GetComponent<SpriteRenderer>().color = _snakeColor;
            ObserverColorSnake(l_Part, _snakeColor);
            l_Part.gameObject.SetActive(true);
            ServerManager.Spawn(l_Part, _connection);
            TargetSpawnSnake(_connection, l_Part);
        }
    }
    
    [TargetRpc]
    private void TargetSpawnSnake(NetworkConnection _connection, GameObject _part)
    {
        i_CollisionTest.Add(_part.GetComponent<Collider2D>(), true);
        i_BodyParts.Add(_part.gameObject);
        
        i_Length++;
        
        _part.GetComponent<SpriteRenderer>().sortingOrder = -i_Length;
        
        ScaleSnake();
        //LeaderBoard.OnScoreChange(i_Length);
        LeaderBoard.RequestSendServerScore(base.LocalConnection, i_PlayerData.p_CurrentName, i_Length);
        StatsDisplay.OnLengthChange(i_Length);
        i_Audio.pitch = Random.Range(0.9f, 1.1f);
        i_Audio.PlayOneShot(i_SpawnSFX, c_SpawnVolume);
    }

    [ObserversRpc(BufferLast = true)] // BUG: think the buffer is only working for the last body part to be colored
    private void ObserverColorSnake(GameObject _part, Color _color)
    {
        _part.GetComponent<SpriteRenderer>().color = _color;
    }

    private Color GetColor()
    {
        //Debug.Log("snake controller sees the selected color as " + i_PlayerData.p_SnakeColorIndex);
        Color l_Color = new Color();
        switch (i_PlayerData.p_SnakeColorIndex)
        {
            case < 9:
                l_Color = i_PlayerData.p_SnakeColor;
                return l_Color;
            case 9:
                l_Color = new Color(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f), 1f);
                return l_Color;
            default:
                Debug.Log("How the fuck did we get here?");
                return Color.white;
        }
    }

    private void GrowSnake() 
    {
        ServerGrowSnake(base.LocalConnection, i_PositionsHistory[Mathf.Clamp((i_Length - 1) * c_Gap, 0, i_PositionsHistory.Count - 1)], GetColor());
    }
    
    [ServerRpc]
    private void ServerGrowSnake(NetworkConnection _connection, Vector3 _pos, Color _snakeColor)
    {
        if (!IsServer)
        {
            return;
        }
        
        GameObject l_Part = Instantiate(i_BodyPrefab, _pos, Quaternion.identity);
        
        l_Part.GetComponent<SpriteRenderer>().color = _snakeColor;
        ObserverColorSnake(l_Part, _snakeColor);
        
        ServerManager.Spawn(l_Part, _connection);
        
        TargetGrowSnake(_connection, l_Part);
        
        l_Part.gameObject.SetActive(true);
    }

    [TargetRpc(OrderType = DataOrderType.Last)] // INFO: idk if the ordertype does anything, but the target is being called twice by one server call or else it'd pass a new _part
    private void TargetGrowSnake(NetworkConnection _connection, GameObject _part)
    {
        i_CollisionTest.Add(_part.GetComponent<Collider2D>(), true);
        i_BodyParts.Add(_part.gameObject);
        
        i_Length++;
        // steer speed changes
        c_SteerSpeed = c_SteerSpeed_Max * Mathf.Exp(-decayRate * i_Length);
        float decreaseAmount = Mathf.Pow(i_Length, lengthMultiplier);
        c_SteerSpeed /= decreaseAmount;
        c_SteerSpeed = Mathf.Max(c_SteerSpeed, c_SteerSpeed_Min);
        //Debug.Log(c_SteerSpeed);

        _part.GetComponent<SpriteRenderer>().sortingOrder = -i_Length;

        ScaleSnake();
        
        // INFO: do less important stuff
        //LeaderBoard.OnScoreChange(i_Length); 
        LeaderBoard.RequestSendServerScore(base.LocalConnection, i_PlayerData.p_CurrentName, i_Length);
        StatsDisplay.OnLengthChange(i_Length);
        i_Audio.pitch = Random.Range(0.9f, 1.1f);
        i_Audio.PlayOneShot(i_GrowSFX, c_GrowVolume);
    }

    private void ScaleSnake()
    {
        float l_Scale = 1f + (c_BodyScaling * (i_Length - c_MinLength));
        Vector3 l_ScaleVector = new Vector3(l_Scale, l_Scale, 1f);
        foreach (GameObject l_Part in i_BodyParts)
        {
            l_Part.transform.localScale = l_ScaleVector;
        }
        
        i_HeadRadius = i_BodyParts[0].transform.localScale.x / 2f;
        //Debug.DrawLine(i_PositionsHistory[0], i_PositionsHistory[0] + new Vector3(0f, i_HeadRadius, 0f), Color.green, 1f);
        playerCamera.orthographicSize = 5f + (c_CameraScaling * (i_Length - c_MinLength));
    }

    private IEnumerator CollisionThread() // TODO: jank af
    {
        yield return new WaitForSeconds(0.5f);
        while (i_Alive)
        {
            //yield return new WaitForFixedUpdate(); // DEBUG: temporary to test, fixed time step is 0.02
            yield return new WaitForSeconds(c_CollisionRate);

            Collider2D[] l_Colliders = new Collider2D[5];
            int l_Collisions = Physics2D.OverlapCircleNonAlloc(i_PositionsHistory[c_Gap+1], i_HeadRadius, l_Colliders, i_PlayerWallLayerMask); // INFO: gap+1 to compensate lag
            
            for (int i = 0; i < l_Collisions; i++)
            {
                if (!i_CollisionTest.ContainsKey(l_Colliders[i]))
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning($"you got killed by {l_Colliders[i].gameObject.name}", l_Colliders[i].gameObject); // DEBUG
                    UnityEditor.EditorApplication.isPaused = true; // DEBUG
                    yield return new WaitForSeconds(2f);
                    #endif
                    i_Alive = false;
                    break;
                }
            }

            if (i_Alive)
            {
                l_Colliders = new Collider2D[5];
                l_Collisions = Physics2D.OverlapCircleNonAlloc(i_PositionsHistory[c_Gap+1], i_HeadRadius, l_Colliders, i_FoodLayerMask); // INFO: gap+1 to compensate lag

                for (int i = 0; i < l_Collisions; i++)
                {
                    //l_Colliders[i].gameObject.SetActive(false);
                    l_Colliders[i].transform.position = new Vector3(100f, 100f, 0f); // DEBUG
                    if (l_Colliders[i].gameObject.CompareTag("Food"))
                    {
                        DestroyFood(l_Colliders[i].GetComponent<NetworkObject>());
                        GrowSnake();
                    }
                    else if (l_Colliders[i].gameObject.CompareTag("SuperFood"))
                    {
                        ServerDestroySuperFood(l_Colliders[i].GetComponent<NetworkObject>());
                        for (int j = 0; j < 3; j++)
                        {
                            GrowSnake();
                        }
                    }
                }
            }
        }
        
        DestroyAllParts();
        
        LeaderBoard.RequestClearScore(base.LocalConnection);
        StartCoroutine(DelayedDisconnectThread()); // TODO: should this just be moved to leaderboard script?
        
        i_Audio.pitch = Random.Range(0.9f, 1.1f);
        i_Audio.PlayOneShot(i_DeathSFX, c_DeathVolume); // INFO: gets cut off cause audio listener is under SnakeController gameobject
    }
    
    private IEnumerator DelayedDisconnectThread() // INFO: wait until server tells us we're okay to disconnect
    {
        while (!LeaderBoard.p_SafeToDisconnect)
        {
            yield return new WaitForSeconds(0.25f);
        }
        
        ClientManager.StopConnection();

        i_ClientAPI.OnPlayerDeath(i_Length);
    }

    private void DestroyFood(NetworkObject _food) 
    {
        ServerDestroyFood(_food);
    }
    
    [ServerRpc]
    private void ServerDestroyFood(NetworkObject _food)
    {
        if (!IsServer)
        {
            return;
        }
        
        ServerManager.Despawn(_food, DespawnType.Pool);
        
        FoodSpawner.OnFoodChange(-1);
    }
    
    [ServerRpc]
    private void ServerDestroySuperFood(NetworkObject _food)
    {
        if (!IsServer)
        {
            return;
        }
        
        ServerManager.Despawn(_food, DespawnType.Pool);
        
        FoodSpawner.OnSuperFoodChange(-1);
    }

    /*[TargetRpc]
    private void ResetClientCollider(NetworkConnection con, NetworkObject _food)
    {
        _food.GetComponent<CircleCollider2D>().enabled = true;
    }*/ // DEBUG: testing doing this through the food's OnEnable
    
    private void DestroyLastPart()
    {
        int _index = i_BodyParts.Count - 1;
        ServerDestroyPart(base.LocalConnection, false, i_BodyParts[_index], i_BodyParts[_index].transform.position);
        
        i_BodyParts[_index].SetActive(false);
        i_BodyParts.RemoveAt(_index); // BUG: moved this out of the TargetRpc cause sometimes the stars would align to cause a missing ref, still testing
        i_Length--;
    }

    [TargetRpc]
    private void TargetDestroyLastPart(NetworkConnection _conn)
    {
        ScaleSnake();
        
        // steer speed changes
        c_SteerSpeed = c_SteerSpeed_Max * Mathf.Exp(-decayRate * i_Length);
        float decreaseAmount = Mathf.Pow(i_Length, lengthMultiplier);
        c_SteerSpeed /= decreaseAmount;
        c_SteerSpeed = Mathf.Max(c_SteerSpeed, c_SteerSpeed_Min);
        
        LeaderBoard.RequestSendServerScore(base.LocalConnection, i_PlayerData.p_CurrentName, i_Length);
        StatsDisplay.OnLengthChange(i_Length);
    }
    
    private void DestroyAllParts()
    {
        Vector2[] l_FoodSpawns = new Vector2[i_BodyParts.Count];
        for (int i = 0; i < l_FoodSpawns.Length; i++)
        {
            l_FoodSpawns[i] = i_BodyParts[i].transform.position;
        }
        for (int i = 0; i < l_FoodSpawns.Length; i++)
        {
            ServerDestroyPart(base.LocalConnection, true, i_BodyParts[i], l_FoodSpawns[i]);
        }
    }
    
    [ServerRpc]
    private void ServerDestroyPart(NetworkConnection _conn, bool _death, GameObject _part, Vector2 _position)
    {
        if (!IsServer)
        {
            return;
        }

        ServerManager.Despawn(_part);

        if (_death) // TODO: prob split this into two diff functions
        {
            if (Random.Range(0, c_DropFoodRate) == 0)
            {
                /*GameObject l_Food = Instantiate(i_FoodPrefab, _position, Quaternion.identity);
                ServerManager.Spawn(l_Food);*/
                
                ServerSpawnFood(_conn, _position);
            }
        }
        else
        {
            TargetDestroyLastPart(_conn);
            
            /*GameObject l_Food = Instantiate(i_FoodPrefab, _position, Quaternion.identity);
            ServerManager.Spawn(l_Food);*/
            
            ServerSpawnFood(_conn, _position);
        }
    }

    private void ServerSpawnFood(NetworkConnection _conn, Vector2 _position) // INFO: only called in ServerRpc
    {
        NetworkObject l_Food = NetworkManager.GetPooledInstantiated(i_FoodPrefab, _position, Quaternion.identity, InstanceFinder.IsServer);
        ServerManager.Spawn(l_Food);
        //ResetClientCollider(_conn, l_Food); // BUG: is this working 100% of the time?
        //SetInitialColor(l_Food);
        FoodSpawner.OnFoodChange(1);
    }
    
    /*[ObserversRpc]
    private void SetInitialColor(NetworkObject l_Food)
    {
        l_Food.GetComponent<FoodColorHandler>().AssignColor();
    }*/ // DEBUG: testing doing this through the food's OnEnable

    private void CleanPositionHistory()
    {
        int l_Required = (i_Length + 2) * c_Gap; // INFO: leave space for bursts of new body parts
        if (i_PositionsHistory.Count - l_Required > 0) // 
        {
            i_PositionsHistory.RemoveRange(l_Required, i_PositionsHistory.Count - l_Required);
        }
    }
}