using AYellowpaper.SerializedCollections;
using Ilumisoft.SkillDrive;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public bool isMultiplayer { get; private set; }
    public static GameController Instance;

    [SerializeField] private NetworkManager networkManager;
    [SerializedDictionary("id", "name")] public SerializedDictionary<int, GameObject> players;

    public GameController()
    {
        Instance = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);
        ListenNetworkEvents();
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
        if (networkManager.IsHost)
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
                player.gameObject.SetActive(true);
                player.GetComponent<NetworkRigidbody>().enabled = true;
            }
        }
    }
}