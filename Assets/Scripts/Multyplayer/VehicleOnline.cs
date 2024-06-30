using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleOnline : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCarPosition();
        }
    }

    void ResetCarPosition()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

}
