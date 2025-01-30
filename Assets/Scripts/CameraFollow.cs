using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0, 0);
    [SerializeField] private float moveTime = 1f;
    [SerializeField] private Ease easing;
    [SerializeField] private bool isAutoRotate;
    
    private Vector3 _lastStableDirection;
    private float _rotationSmoothVelocity;
    private const float RotationSmoothTime = 0.3f;

    void LateUpdate()
    {
        if (!target) return;

        UpdateCameraPosition();
        HandleCameraRotation();
    }

    private void UpdateCameraPosition()
    {
        Vector3 basePosition = target.position + Vector3.up * distance;
        Vector3 finalPosition = basePosition + offset;
        
        transform.position = Vector3.Lerp(
            transform.position, 
            finalPosition, 
            Time.deltaTime * moveTime
        );
    }

    private void HandleCameraRotation()
    {
        if (!isAutoRotate)
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        Vector3 horizontalForward = GetStableHorizontalDirection();
        Quaternion targetRotation = Quaternion.Euler(90f, 0, 0) * 
                                   Quaternion.LookRotation(horizontalForward);

        transform.rotation = SmoothDampQuaternion(
            transform.rotation, 
            targetRotation, 
            ref _rotationSmoothVelocity, 
            RotationSmoothTime
        );
    }

    private Vector3 GetStableHorizontalDirection()
    {
        Vector3 rawDirection = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        
        if (rawDirection == Vector3.zero)
            return _lastStableDirection;

        _lastStableDirection = rawDirection.normalized;
        return _lastStableDirection;
    }

    private Quaternion SmoothDampQuaternion(
        Quaternion current, 
        Quaternion target, 
        ref float velocity, 
        float smoothTime)
    {
        Vector3 currentEuler = current.eulerAngles;
        Vector3 targetEuler = target.eulerAngles;
        
        return Quaternion.Euler(
            Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref velocity, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref velocity, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref velocity, smoothTime)
        );
    }

    public void SetTarget(Transform newTarget)
    {
        this.transform.DOMove(
                newTarget.position + Vector3.up * distance + offset, 
                moveTime
            )
            .SetEase(easing)
            .OnComplete(() => SelectTarget(newTarget));
    }

    private void SelectTarget(Transform newTarget)
    {
        target = newTarget;
        _lastStableDirection = Vector3.ProjectOnPlane(newTarget.forward, Vector3.up).normalized;
    }
}