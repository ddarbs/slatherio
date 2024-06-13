using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class SnakeSmartBotController : NetworkBehaviour
{
    // Settings
    private readonly Vector2 c_MapBounds = new Vector2(80f, 80f); 
    private const float c_BaseSpeed = 4;
    private const float c_BoostSpeed = 6;
    private const float c_CollisionRate = 0.02f; // INFO : 0.02 is the fixed timestep
    private const float c_SteerSpeed = 180;
    private const int c_Gap = 5;
    private const float c_BodyScaling = 0.0025f;
    [Range(1, 10)] private const int c_DropFoodRate = 3; // INFO: 1 / i_DropFoodRate chance per part to drop a food on death 
    private const float c_BoostDropFoodTime = 2f; // INFO: time spent boosting to drop your last part as food 
    private const int c_MinLength = 3; // INFO: starter length and length it stops you from boosting
    private const float c_ForwardWeight = 1f; // INFO: weight for no rotation change
    private const float c_TurnWeight = 0.75f; // INFO: weight for left/right rotation change
    private const float c_TurnWeightModifier = 0.05f; // INFO: gives extra weight to turning towards map origin
    private const float c_DistanceWeightModifier = 0.5f; // INFO: gives extra weight to turning towards map origin
    private const float c_HuntRadius = 5f;
    private const float c_HuntTimeout = 2f;
    private const float c_HuntCooldown = 0.5f; 
    private const float c_AvoidRadius = 4f;
    
    // Managed variables
    private float i_HeadRadius = 0;
    private float i_CurrentSpeed = 0;
    private int i_Length = 0;
    private int i_CleanCounter = 0;
    private bool i_Alive;
    private List<GameObject> i_BodyParts = new List<GameObject>();
    private List<Vector3> i_PositionsHistory = new List<Vector3>();
    private Dictionary<Collider2D, bool> i_CollisionTest = new Dictionary<Collider2D, bool>();
    private Quaternion my_rotation;
    private bool i_Boosting;
    private float i_BoostTime = 0f;
    private bool i_Hunting = false;
    private bool i_CanHunt = false;
    private Vector3 i_HuntPosition;
    private float i_TurnLeftWeight = 0f;
    private float i_ForwardWeight = 0f;
    private float i_TurnRightWeight = 0f;
    private LayerMask i_FoodLayerMask;
    private Coroutine i_HuntTimeoutThread;
    private Coroutine i_HuntCooldownThread;
    private LayerMask i_PlayerLayerMask;
    private LayerMask i_PlayerWallLayerMask;
    private bool i_Avoiding = false;
    private int i_BotID = 0;
    
    // References
    [SerializeField] private GameObject i_BodyPrefab;
    [SerializeField] private NetworkObject i_FoodPrefab;
    [SerializeField] private TextMeshPro i_NameText;
    /*[SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource i_Audio;
    [SerializeField] private AudioClip i_SpawnSFX, i_GrowSFX, i_DeathSFX;*/

    private void Awake()
    {
        i_FoodLayerMask = LayerMask.GetMask("Food");
        i_PlayerLayerMask = LayerMask.GetMask("Player");
        i_PlayerWallLayerMask = LayerMask.GetMask("Player", "MapBound");
    }

    public void SetupBot(int _botID)
    {
        //base.OnStartClient();
        this.enabled = IsServer;
        
        if (base.IsServer)
        {
            i_BotID = _botID;

            //my_rotation = this.transform.rotation;
            my_rotation = Quaternion.Euler(0f, 0f, 0f);
            
            ServerSetName();
            ServerSpawnSnake();
            
            i_CurrentSpeed = c_BaseSpeed; // DEBUG
            
            i_HeadRadius = transform.localScale.x / 2f;

            i_TurnLeftWeight = c_TurnWeight;
            i_ForwardWeight = i_TurnLeftWeight + c_ForwardWeight;
            i_TurnRightWeight = i_ForwardWeight + c_TurnWeight;
            i_CanHunt = true;
            i_Alive = true;
            
            StartCoroutine(CollisionThread());
        }
    }
    
    public override void OnStartClient()
    {
        this.enabled = base.IsServer;
    }
    
    private void Update() // INFO: clean up the position list and watch for ui stuff
    {
        if (!base.IsServer || !i_Alive)
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
        
        // TODO: randomly boost for .25s increments? weight chance to boost based on length up to an extent?
        /*if (Input.GetMouseButton(0) && i_Length > c_MinLength) // INFO: boosting
        {
            i_Boosting = true;
            i_CurrentSpeed = c_BoostSpeed;
        }
        else
        {
            i_Boosting = false;
            i_CurrentSpeed = c_BaseSpeed;
        }*/
    }

    private void FixedUpdate() // INFO: keep stuff from being reliant on fps by having physics done in fixedupdate
    {
        if (!base.IsServer || !i_Alive)
        {
            return;
        }
        
        // INFO: turning
        Transform l_Transform = transform;
        Vector3 l_TransPos = l_Transform.position;
        
        float l_Angle = 0f;
        bool l_ChangeAngle = false;

        Collider2D[] l_Colliders = new Collider2D[30];
        int l_Collisions = Physics2D.OverlapCircleNonAlloc(l_TransPos, c_AvoidRadius, l_Colliders, i_PlayerLayerMask);
        for (int i = 0; i < l_Collisions; i++)
        {
            if (!i_CollisionTest.ContainsKey(l_Colliders[i]))
            {
                Vector3 l_EnemyDirection = l_Colliders[i].transform.position - l_TransPos;
                
                Debug.DrawRay(l_TransPos, transform.up, Color.red, Time.fixedDeltaTime);
                
                // turn around
                l_Angle = Vector2.SignedAngle(Vector2.up, -l_EnemyDirection);
                Debug.DrawRay(l_TransPos, -l_EnemyDirection, Color.yellow, Time.fixedDeltaTime);
                
                i_Avoiding = true;
                l_ChangeAngle = true;
                break;
            }

            i_Avoiding = false;
        }

        if (!i_Avoiding)
        {
            if (!i_Hunting && i_CanHunt)
            {
                l_Colliders = new Collider2D[5];
                l_Collisions = Physics2D.OverlapCircleNonAlloc(l_TransPos, c_HuntRadius, l_Colliders, i_FoodLayerMask);
                for (int i = 0; i < l_Collisions; i++)
                {
                    //if (l_Colliders[i].gameObject.CompareTag("Food")) // DEBUG: let's also try to grab super food 
                    //{
                        i_Hunting = true;
                        i_CanHunt = false;
                        i_HuntPosition = l_Colliders[i].transform.position;
                        if (i_HuntTimeoutThread == null)
                        {
                            i_HuntTimeoutThread = StartCoroutine(HuntTimeoutThread());
                        }
                        else
                        {
                            StopCoroutine(i_HuntTimeoutThread);
                            i_HuntTimeoutThread = StartCoroutine(HuntTimeoutThread());
                        }
                        if (i_HuntCooldownThread == null)
                        {
                            i_HuntCooldownThread = StartCoroutine(HuntCooldownThread());
                        }
                        else
                        {
                            StopCoroutine(i_HuntCooldownThread);
                            i_HuntCooldownThread = StartCoroutine(HuntCooldownThread());
                        }
                        
                        Vector3 l_TargetDirection = (i_HuntPosition - l_TransPos).normalized;
                        l_Angle = Vector2.SignedAngle(Vector2.up, l_TargetDirection);
                        l_ChangeAngle = true;
                        Debug.DrawRay(l_TransPos, l_TargetDirection, Color.green, Time.fixedDeltaTime);
                        break;
                    //}
                }
            }
            if (!i_Hunting)
            {
                Vector3 l_OriginDirection = (Vector3.zero - l_TransPos).normalized;
                float l_OriginAngle = Vector2.SignedAngle(Vector2.up, l_OriginDirection);
                float l_AngleDiff = Vector2.Angle(transform.up, l_OriginDirection);
                float l_TurnWeight = Mathf.Abs(l_AngleDiff / 180f); // INFO: higher number = less pointing towards map origin
                float l_DistanceWeight = Mathf.Max(Mathf.Abs(l_TransPos.x / c_MapBounds.x), Mathf.Abs(l_TransPos.y / c_MapBounds.y)); // INFO: higher number = further away from map origin
                
                float l_Random = Random.Range(0f, i_TurnRightWeight + (l_TurnWeight * c_TurnWeightModifier) + (l_DistanceWeight * c_DistanceWeightModifier)); // INFO: left, straight, right, towards origin
                if (l_Random <= i_TurnLeftWeight)
                {
                    l_Angle = Vector2.SignedAngle(Vector2.up, -transform.right);
                    l_ChangeAngle = true;
                }
                else if (l_Random <= i_ForwardWeight)
                {
                    //l_Angle = Vector2.SignedAngle(Vector2.up, transform.up);
                }
                else if (l_Random <= i_TurnRightWeight)
                {
                    l_Angle = Vector2.SignedAngle(Vector2.up, transform.right);
                    l_ChangeAngle = true;
                }
                else if (l_Random > i_TurnRightWeight)
                {
                    l_Angle = l_OriginAngle;
                    l_ChangeAngle = true;
                }
            }
            else
            {
                Vector3 l_TargetDirection = (i_HuntPosition - l_TransPos).normalized;
                l_Angle = Vector2.SignedAngle(Vector2.up, l_TargetDirection);
                l_ChangeAngle = true;
                Debug.DrawRay(l_TransPos, l_TargetDirection, Color.green, Time.fixedDeltaTime);
            }
        }
        
        if (l_ChangeAngle)
        {
            Vector3 l_TargetRotation = new Vector3(0, 0, l_Angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(l_TargetRotation), c_SteerSpeed * Time.fixedDeltaTime);
        }
        
        // INFO: moving
        l_Transform.position += l_Transform.up * (i_CurrentSpeed * Time.deltaTime);
        l_TransPos = l_Transform.position;
        i_PositionsHistory.Insert(0, l_TransPos);
        l_TransPos.z = -10f;
        i_NameText.transform.rotation = my_rotation;
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
        
        /*// INFO: drop food if you boost for too long
        if (i_Boosting)
        {
            i_BoostTime += Time.fixedDeltaTime;

            if (i_BoostTime >= c_BoostDropFoodTime)
            {
                DestroyLastPart();
                i_BoostTime = 0;
            }
        }*/
    }

    private IEnumerator HuntTimeoutThread()
    {
        yield return new WaitForSeconds(c_HuntTimeout);
        i_Hunting = false;
    }

    private IEnumerator HuntCooldownThread()
    {
        yield return new WaitForSeconds(c_HuntCooldown);
        i_CanHunt = true;
    }
    
    private void ServerSetName()
    {
        string l_Name = "bozo_" + Random.Range(0, 69420);
        
        i_NameText.text = l_Name;
        
        ObserverSetName(l_Name);
    }

    [ObserversRpc(BufferLast = true, ExcludeServer = true)]
    private void ObserverSetName(string _name)
    {
        i_NameText.text = _name;
    }

    private void ServerSpawnSnake()
    {
        GameObject l_Part = default;
        for (int i = 0; i < c_MinLength; i++)
        {
            l_Part = Instantiate(i_BodyPrefab, transform.position, Quaternion.identity);
            //l_Part = NetworkManager.GetPooledInstantiated(i_BodyPrefab, transform.position, Quaternion.identity, base.IsServer);
            ServerManager.Spawn(l_Part);
            i_BodyParts.Add(l_Part.gameObject);
            i_CollisionTest.Add(l_Part.GetComponent<Collider2D>(), true);
            i_Length++;
            l_Part.gameObject.SetActive(true);
        }
        LeaderBoard.RequestSendServerScore(i_BotID, i_NameText.text, i_Length);
    }

    private void GrowSnake() 
    {
        ServerGrowSnake(i_PositionsHistory[Mathf.Clamp((i_Length - 1) * c_Gap, 0, i_PositionsHistory.Count - 1)]);
    }
    
    private void ServerGrowSnake(Vector3 _pos)
    {
        if (!IsServer)
        {
            return;
        }
        
        //GameObject l_Part = Instantiate(i_BodyPrefab);
        NetworkObject l_Part = NetworkManager.GetPooledInstantiated(i_BodyPrefab, _pos, Quaternion.identity, base.IsServer);
        
        ServerManager.Spawn(l_Part, base.Owner);
        i_BodyParts.Add(l_Part.gameObject);
        i_CollisionTest.Add(l_Part.GetComponent<Collider2D>(), true);
        i_Length++;
        
        ScaleSnake();
        l_Part.gameObject.SetActive(true);
        
        if (i_Hunting)
        {
            i_Hunting = false;
        }
        
        LeaderBoard.RequestSendServerScore(i_BotID, i_NameText.text, i_Length);
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
    }

    private IEnumerator CollisionThread() // TODO: jank af
    {
        yield return new WaitForSeconds(0.5f);
        while (i_Alive)
        {
            //yield return new WaitForFixedUpdate(); // DEBUG: temporary to test, fixed time step is 0.02
            yield return new WaitForSeconds(c_CollisionRate);

            Collider2D[] l_Colliders = new Collider2D[5];
            int l_Collisions = Physics2D.OverlapCircleNonAlloc(i_PositionsHistory[c_Gap+1], i_HeadRadius, l_Colliders, i_PlayerWallLayerMask);
            
            for (int i = 0; i < l_Collisions; i++)
            {
                if (!i_CollisionTest.ContainsKey(l_Colliders[i]))
                {
                    i_Alive = false;
                    break;
                }
            }

            if (i_Alive)
            {
                l_Colliders = new Collider2D[5];
                l_Collisions = Physics2D.OverlapCircleNonAlloc(i_PositionsHistory[c_Gap+1], i_HeadRadius, l_Colliders, i_FoodLayerMask);

                for (int i = 0; i < l_Collisions; i++)
                {
                    l_Colliders[i].transform.position = new Vector3(100f, 100f, 0f); // DEBUG
                    if (l_Colliders[i].gameObject.CompareTag("Food"))
                    {
                        //l_Colliders[i].enabled = false;
                        DestroyFood(l_Colliders[i].GetComponent<NetworkObject>());
                        GrowSnake();
                    }
                    else if (l_Colliders[i].gameObject.CompareTag("SuperFood"))
                    {
                        //l_Colliders[i].enabled = false;
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
        
        LeaderBoard.RequestClearScore(i_BotID);
        
        BotManager.OnSmartBotDeath();
        
        StartCoroutine(DelayedDisconnectThread());
    }

    private IEnumerator DelayedDisconnectThread()
    {
        yield return new WaitForSeconds(0.2f);
        ServerManager.Despawn(gameObject); 
    }

    private void DestroyFood(NetworkObject _food) 
    {
        ServerDestroyFood(_food);
    }
    
    private void ServerDestroyFood(NetworkObject _food)
    {
        if (!IsServer)
        {
            return;
        }
        
        ServerManager.Despawn(_food, DespawnType.Pool);
        
        FoodSpawner.OnFoodChange(-1);
    }
    
    private void ServerDestroySuperFood(NetworkObject _food)
    {
        if (!IsServer)
        {
            return;
        }
        
        ServerManager.Despawn(_food, DespawnType.Pool);
        
        FoodSpawner.OnSuperFoodChange(-1);
    }
    
    private void DestroyLastPart()
    {
        int _index = i_BodyParts.Count - 1;
        ServerDestroyPart(false, i_BodyParts[_index], i_BodyParts[_index].transform.position);
        
        i_BodyParts[_index].SetActive(false);
        i_BodyParts.RemoveAt(_index); // BUG: moved this out of the TargetRpc cause sometimes the stars would align to cause a missing ref, still testing
        i_Length--;
        ScaleSnake();
        
        LeaderBoard.RequestSendServerScore(i_BotID, i_NameText.text, i_Length);
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
            ServerDestroyPart(true, i_BodyParts[i], l_FoodSpawns[i]);
        }
    }
    
    private void ServerDestroyPart(bool _death, GameObject _part, Vector2 _position)
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
                ServerSpawnFood(_position);
            }
        }
        else
        {
            ServerSpawnFood(_position);
        }
    }

    private void ServerSpawnFood(Vector2 _position) // INFO: only called in ServerRpc
    {
        NetworkObject l_Food = NetworkManager.GetPooledInstantiated(i_FoodPrefab, _position, Quaternion.identity, InstanceFinder.IsServer);
        ServerManager.Spawn(l_Food);
        FoodSpawner.OnFoodChange(1);
    }

    private void CleanPositionHistory()
    {
        int l_Required = (i_Length + 2) * c_Gap; // INFO: leave space for bursts of new body parts
        if (i_PositionsHistory.Count - l_Required > 0) // 
        {
            i_PositionsHistory.RemoveRange(l_Required, i_PositionsHistory.Count - l_Required);
        }
    }
}