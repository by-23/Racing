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

    void LateUpdate()
    {
        if (target != null)
        {
            var position = target.position - Vector3.forward * distance + offset;
            var rotatedPosition = target.position - target.forward * distance + target.TransformDirection(offset);
            rotatedPosition.y = position.y;
            transform.position = Vector3.Lerp(transform.position, rotatedPosition, 1);
            var rotation = Quaternion.LookRotation(target.position - rotatedPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1);
        }
    }

    public void SetTarget(Transform target)
    {
        this.transform.DOMove(target.position, moveTime).SetEase(easing).OnComplete(() => SelectTarget(target));
    }

    private void SelectTarget(Transform target)
    {
        this.target = target;
    }
}