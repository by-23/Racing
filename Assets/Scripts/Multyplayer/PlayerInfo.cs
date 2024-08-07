using Photon.Pun;
using UnityEngine;

public class PlayerInfo : MonoBehaviourPun, IPunObservable
{
    public string PlayerName;

    private static readonly string[] RandomNames = new string[]
    {
        "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Jamie", "Avery", "Peyton", "Quinn",
        "Reese", "Skyler", "Rowan", "Sawyer", "Emerson", "Finley", "Dakota", "Harper", "Hayden", "Parker",
        "Cameron", "Drew", "Elliot", "Jesse", "Kai", "Logan", "Micah", "Phoenix", "River", "Sage",
        "Shawn", "Tatum", "Teagan", "Tristan", "Wren", "Blake", "Charlie", "Dylan", "Eden", "Frankie",
        "Gray", "Hunter", "Jaden", "Kendall", "Lane", "Lennon", "Marlowe", "Nico", "Oakley", "Payton",
        "Reagan", "Remy", "Rory", "Sam", "Spencer", "Toby", "Tyler", "Winter", "Zion", "Aiden",
        "Bailey", "Brady", "Carter", "Dallas", "Easton", "Emery", "Grayson", "Harlow", "Jules", "Kieran",
        "Luca", "Maddox", "Mason", "Noah", "Piper", "Quincy", "Reed", "Riley", "Rowan", "Sawyer",
        "Shiloh", "Sky", "Tanner", "Tate", "Toby", "Wesley", "Wyatt", "Zane", "Archer", "Beckett",
        "Brooks", "Chase", "Colby", "Dane", "Eli", "Finn", "Gage", "Holden", "Jace", "Kade"
    };

    void Awake()
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            PlayerName = RandomNames[Random.Range(0, RandomNames.Length)];
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(PlayerName);
        }
        else
        {
            PlayerName = (string)stream.ReceiveNext();
        }
    }
}