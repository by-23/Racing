using System.Collections.Generic;
using Photon.Pun;
public class GameController : MonoBehaviourPunCallbacks
{
    public event Action OnRankingsChanged;
    public event Action<Vehicle> OnVehicleFinished;
    
    #region Fields

    /// <summary>
    ///     The instance.
    /// </summary>
    private static GameController instance;
    private readonly List<Vehicle> _vehicles = new List<Vehicle>();
    private readonly HashSet<Vehicle> _finishedVehicles = new HashSet<Vehicle>();

    [Header("Debug")]
    [SerializeField] private List<Vehicle> _finishedVehiclesForInspector;

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

    [SerializeField] internal bool isGameStarted = true;
    [SerializeField] internal bool isDevMode = true;

    public void ReportCheckpointPassed()
    {
        OnRankingsChanged?.Invoke();
    }

    public void VehicleFinished(Vehicle vehicle)
    {
        if (_finishedVehicles.Add(vehicle))
        {
            OnVehicleFinished?.Invoke(vehicle);
        }
    }

    public void RegisterVehicle(Vehicle vehicle)
    {
        if (!_vehicles.Contains(vehicle))
        {
            _vehicles.Add(vehicle);
        }
    }

    public void UnregisterVehicle(Vehicle vehicle)
    {
        if (_vehicles.Contains(vehicle))
        {
            _vehicles.Remove(vehicle);
        }
    }

    public List<Vehicle> GetRankedVehicles()
    {
        // Сортируем машины по убыванию их прогресса.
        // Машины с большим ProgressDistance будут первыми в списке.
        return _vehicles.OrderByDescending(v => v.vehicleMovement.ProgressDistance).ToList();
    }

    public bool IsVehicleFinished(Vehicle vehicle)
    {
        return _finishedVehicles.Contains(vehicle);
    }

    public List<Vehicle> GetRacingVehicles()
    {
        return _vehicles.Where(v => !_finishedVehicles.Contains(v)).ToList();
    }


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

#if UNITY_EDITOR
    private void Update()
    {
        if (_finishedVehicles == null || _finishedVehiclesForInspector == null)
         return;
        // For debugging in the inspector, show the contents of the HashSet.
        if (Application.isPlaying && _finishedVehicles.Count != _finishedVehiclesForInspector.Count)
        {
            _finishedVehiclesForInspector = _finishedVehicles.ToList();
        }
    }
#endif
}