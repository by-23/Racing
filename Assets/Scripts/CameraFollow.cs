using System;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : NetworkBehaviour
{
    [SerializeField] private Transform target; // Цель, за которой следит камера
    [SerializeField] private float smoothing = 5f; // Коэффициент сглаживания
    [SerializeField] private Vector3 offset = new Vector3(0f, 0, 0); // Оффсет камеры
    private Camera camera;

    private void Start()
    {
        camera = GetComponent<Camera>();
        target = GetComponentInParent<Transform>();
        if (IsOwner)
            camera.enabled = true;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing * Time.deltaTime);
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}