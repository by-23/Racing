using AYellowpaper.SerializedCollections;
using DesignPatterns.ObjectPool;
using Ilumisoft.SkillDrive;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public bool isMultiplayer { get; private set; }
    public static GameController Instance;

    [SerializeField] private Vehicle player;
    [SerializeField] private NetworkManager networkManager;
    [SerializedDictionary("id", "name")] public SerializedDictionary<int, GameObject> players;

    private ObjectPool objectPool;

    public GameController()
    {
        Instance = this;
    }

    private void Start()
    {
        objectPool = GetComponent<ObjectPool>();
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);
        ListenNetworkEvents();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex > 1 && !isMultiplayer)
        {
            objectPool.SetupPool();
            Vehicle instantiatedPlayer = Instantiate(player, Vector3.zero, Quaternion.identity);
            instantiatedPlayer.playerCam.enabled = true;
            instantiatedPlayer.Rigidbody.isKinematic = false;
        }
    }

    private void ListenNetworkEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += NewClientConnected;
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            isMultiplayer = true;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += StartGame;
        };
        NetworkManager.Singleton.OnServerStopped += (bool success) => isMultiplayer = false;
    }

    private void NewClientConnected(ulong playerID)
    {
        if (isMultiplayer && networkManager.IsHost)
        {
            GameObject newPlayer = NetworkManager.Singleton.ConnectedClientsList[checked((int)playerID)].PlayerObject
                .gameObject;
            newPlayer.gameObject.SetActive(false);
            newPlayer.GetComponent<NetworkRigidbody>().enabled = false;
            newPlayer.GetComponent<NetworkObject>().transform.position =
                new Vector3(Random.Range(0, 20), 0, Random.Range(0, 20));
            DontDestroyOnLoad(newPlayer);
            players.Add(checked((int)playerID), newPlayer);
        }
    }

    void StartGame(ulong playerID, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (SceneManager.GetActiveScene().buildIndex > 1)
        {
            foreach (var player in players.Values)
            {
                objectPool.SetupPool();
                player.gameObject.SetActive(true);
                player.GetComponent<NetworkRigidbody>().enabled = true;
            }
        }
    }
}