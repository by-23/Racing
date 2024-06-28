using System;
using AYellowpaper.SerializedCollections;
using Ilumisoft.SkillDrive;
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
    [SerializedDictionary("id", "name")] public SerializedDictionary<int, GameObject> players;

    public GameController()
    {
        Instance = this;
    }

    //Тут проблема в том, что isMultiplayer нигде не меняется на true. Нужно найти где делать true.

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex > 1 && !isMultiplayer)
        {
            Vehicle instantiatedPlayer = Instantiate(player, Vector3.zero, Quaternion.identity);
            instantiatedPlayer.playerCam.enabled = true;
            instantiatedPlayer.Rigidbody.isKinematic = false;
        }
    }

    void StartGame(ulong playerID, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (SceneManager.GetActiveScene().buildIndex > 1)
        {
            foreach (var player in players.Values)
            {
                player.gameObject.SetActive(true);
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}