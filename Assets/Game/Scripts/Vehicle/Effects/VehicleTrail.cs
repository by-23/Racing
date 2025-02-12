using Ilumisoft.SkillDrive;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class VehicleTrail : Vehicle
{
    TrailRenderer trailRenderer;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
    }

    void Update()
    {
        if(IsGrounded != trailRenderer.emitting)
        {
            trailRenderer.emitting = IsGrounded;
        }
    }
}