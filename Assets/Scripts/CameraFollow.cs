using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // Цель, за которой следит камера
    [SerializeField] private float distance = 5;
    [SerializeField] private float distanceSmoothing = 5;

    [SerializeField] private Vector3 offset = new Vector3(0f, 0, 0); // Оффсет камеры

    // [SerializeField] private float angle = 45;
    // [SerializeField] private float angleSmoothing = 30;
    private Camera camera;

    private void Start()
    {
        camera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // transform.RotateAround(target.position, Vector3.up, 0);

            var position = target.position - Vector3.forward * distance + offset;
            var rotatedPosition = target.position - target.forward * distance + target.TransformDirection(offset);
            rotatedPosition.y = position.y;
            transform.position = Vector3.Lerp(transform.position, rotatedPosition, 1);
            // transform.RotateAround(target.position, -transform.right, target.eulerAngles.x);
            var rotation = Quaternion.LookRotation(target.position - position);
            var rotatedRotation = Quaternion.LookRotation(target.position - rotatedPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotatedRotation, 1);
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}