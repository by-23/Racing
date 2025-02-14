using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomList : MonoBehaviourPunCallbacks
{
    public static RoomList Instance;

    [Header("UI")] internal Transform roomListParent;
    [SerializeField] private GameObject roomListItemPrefab;
    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();


    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }

        yield return new WaitUntil(() => PhotonNetwork.IsConnected);

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (cachedRoomList.Count <= 0)
        {
            cachedRoomList = roomList;
        }
        else
        {
            foreach (var room in roomList)
            {
                for (int i = 0; i < cachedRoomList.Count; i++)
                {
                    if (cachedRoomList[i].Name == room.Name)
                    {
                        List<RoomInfo> newList = cachedRoomList;

                        if (room.RemovedFromList)
                        {
                            newList.Remove(newList[i]);
                        }
                        else
                        {
                            newList[i] = room;
                        }

                        cachedRoomList = newList;
                    }
                }
            }
        }

        UpdaterUI();
    }

    public void UpdaterUI()
    {
        if (roomListParent && roomListParent.childCount > 0)
            foreach (Transform roomItem in roomListParent)
            {
                Destroy(roomItem);
            }

        foreach (var room in cachedRoomList)
        {
            if (room.PlayerCount > 0)
            {
                GameObject roomItem = Instantiate(roomListItemPrefab, roomListParent);
                roomItem.GetComponent<RoomItemButton>().SetRoom(room.Name, room.PlayerCount);
            }
        }
    }
}