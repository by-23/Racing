using System.Collections;
using UnityEngine;

public class BotReverseHandler
{
    private BotAI bot;
    private float stuckTimer = 0f;
    private BotAI.BotState previousStateBeforeReverse;

    public BotReverseHandler(BotAI bot)
    {
        this.bot = bot;
    }

    public void CheckStuck(VehicleMovement vehicleMovement, Transform botTransform)
    {
        if (GameController.Instance.isGameStarted &&
            vehicleMovement.rb.linearVelocity.magnitude < bot.stuckVelocityThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= bot.stuckTimeThreshold)
            {
                bot.StartCoroutine(ReverseRoutine(vehicleMovement, botTransform));
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private IEnumerator ReverseRoutine(VehicleMovement vehicleMovement, Transform botTransform)
    {
        previousStateBeforeReverse = bot.currentState;
        bot.currentState = BotAI.BotState.Reversing;

        float timer = 0f;
        while (timer < bot.reverseDuration)
        {
            bot.SetSteering(0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        bot.currentState = (previousStateBeforeReverse == BotAI.BotState.Detouring &&
                            IsObstacleStillBlocking(botTransform))
            ? BotAI.BotState.Detouring
            : BotAI.BotState.Normal;
    }


    private bool IsObstacleStillBlocking(Transform botTransform)
    {
        return Physics.CheckSphere(botTransform.position, bot.detectionDistance, bot.obstacleLayerMask);
    }

    public void ProcessReverse(VehicleMovement vehicleMovement)
    {
        vehicleMovement.ApplyAcceleration(-bot.reverseAccelerationFactor);
        vehicleMovement.ApplySteering(0f);
    }
}