using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.Netcode;

public class HealthController : NetworkBehaviour, IPunObservable
{
    [SerializeField] private float health = 100.0f;
    public event Action OnDeath;
    public event Action<float> OnHealthChanged;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
            health = 0;
            if (TryGetComponent(out BotAI botAI) && botAI.resetCoroutine == null)
                botAI.resetCoroutine = StartCoroutine(botAI.ResetCarPosition());
        }

        OnHealthChanged?.Invoke(health);
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        health += amount;
        if (health >= 100)
        {
            health = 100;
        }

        OnHealthChanged?.Invoke(health);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем данные о здоровье другим игрокам
            stream.SendNext(health);
        }
        else
        {
            // Получаем данные о здоровье от других игроков
            float receivedHealth = (float)stream.ReceiveNext();
            if (receivedHealth < health)
            {
                // Если полученное здоровье меньше текущего, обновляем его
                health = receivedHealth;
                Debug.Log($"Health updated from network: {health}");
                OnHealthChanged?.Invoke(health);
            }
        }
    }
}