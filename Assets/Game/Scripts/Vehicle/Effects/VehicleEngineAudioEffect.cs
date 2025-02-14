using System.Collections;
using UnityEngine;


/// <summary>
/// Effect creating a dynamic engine sfx effect by adjusting the pitch of the engine audio source depending on the velocity of the vehicle
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VehicleEngineAudioEffect : MonoBehaviour
{
    [SerializeField] float minPitch = 0.25f;

    [SerializeField] float maxPitch = 1.1f;

    [SerializeField] float multiplier = 1.2f;

    [SerializeField] private Vehicle vehicle;

    AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (vehicle == null) vehicle = GetComponentInParent<Vehicle>();
    }

    private void OnFinish()
    {
        StartCoroutine(MuteCoroutine());
    }

    IEnumerator MuteCoroutine()
    {
        float duration = 1.0f;

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            audioSource.volume = 1 - Mathf.Lerp(0, 1, elapsed / duration);

            yield return null;

            elapsed += Time.deltaTime;
        }
    }

    void Update()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Mathf.Lerp(minPitch, maxPitch,
                vehicle.vehicleMovement.NormalizedForwardSpeed * multiplier);
        }
    }
}