using TMPro;
using UnityEngine;
public class PlayerRankUIEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    public void SetPlayerData(int rank, string playerName)
    {
        rankText.text = rank.ToString();
        playerNameText.text = playerName;
    }
} 