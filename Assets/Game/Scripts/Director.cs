using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

public class Director : MonoBehaviour
{
    [SerializedDictionary("Point name", "Point Transform")]
    public SerializedDictionary<string, Transform> camPositions;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float moveTime;
    [SerializeField] private Ease easing;

    private void Start()
    {
        mainCamera = FindAnyObjectByType<Camera>();
    }

    public void MoveCamera(string pointName)
    {
        mainCamera.transform.DOMove(camPositions[pointName].position, moveTime).SetEase(easing);
    }
}