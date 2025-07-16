using System.Collections.Generic;
using UnityEngine;
public class RaceStandingsUI : MonoBehaviour
{
    [SerializeField] private PlayerRankUIEntry playerRankPrefab;
    [SerializeField] private Transform container;

    private readonly List<PlayerRankUIEntry> _uiEntries = new List<PlayerRankUIEntry>();
    private GameController _gameController;

    private void Start()
    {
        _gameController = GameController.Instance;
        if (_gameController != null)
        {
            _gameController.OnRankingsChanged += UpdateStandings;
        }
        
        // Первоначальное обновление при запуске
        UpdateStandings();
    }
    
    private void OnDestroy()
    {
        if (_gameController != null)
        {
            _gameController.OnRankingsChanged -= UpdateStandings;
        }
    }
    
    private void UpdateStandings()
    {
        if (_gameController == null && playerRankPrefab == null) return;

        var rankedVehicles = _gameController.GetRankedVehicles();

        // Активируем или создаем нужное количество UI-элементов
        for (int i = 0; i < rankedVehicles.Count; i++)
        {
            PlayerRankUIEntry entry;
            if (i < _uiEntries.Count)
            {
                entry = _uiEntries[i];
            }
            else
            {
                entry = Instantiate(playerRankPrefab, container);
                _uiEntries.Add(entry);
            }
            
            entry.SetPlayerData(i + 1, rankedVehicles[i].gameObject.name);
            entry.gameObject.SetActive(true);
        }

        // Деактивируем лишние UI-элементы, если машин стало меньше
        for (int i = rankedVehicles.Count; i < _uiEntries.Count; i++)
        {
            _uiEntries[i].gameObject.SetActive(false);
        }
    }
}