using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameController : MonoBehaviourPunCallbacks
{
    #region Fields

    /// <summary>
    ///     The instance.
    /// </summary>
    private static GameController instance;

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static GameController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameController>();
                if (instance == null)
                {
                    var obj = new GameObject();
                    obj.name = typeof(GameController).Name;
                    instance = obj.AddComponent<GameController>();
                }
            }

            return instance;
        }
    }

    #endregion

    public Transform[] targets; // Массив точек маршрута
    [SerializeField] internal bool isGameStarted = true;
    [SerializeField] internal bool isDevMode = true;


    private void Awake()
    {
        if (instance == null)
            instance = this as GameController;
        else
            Destroy(gameObject);

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}