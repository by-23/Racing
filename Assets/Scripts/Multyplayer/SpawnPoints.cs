using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPoint
    {
        public Transform spawPoint;
        public bool isFull;
    }

    public List<SpawnPoint> spawnPoints;
}