using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AiController : Agent
{
    public Transform cueBall;
    public List<Transform> balls;
    public Transform[] pockets;
    public bool training;

    //public override void OnEpisodeBegin()
    //{
    //    Debug.Log("ml beggg");
    //    if (GameLogic.instance && GameLogic.instance.players[GameLogic.instance.currentPlayer].name == "CPU")
    //    {

    //    }
    //    else return;
    //}

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add observations for cue ball position
        sensor.AddObservation(cueBall.position);

        // Add observations for all balls
        foreach (var ball in balls)
        {
            sensor.AddObservation(ball.position);
            sensor.AddObservation(ball.gameObject.activeInHierarchy ? 1f : 0f); // Is the ball active?
        }

        // Add observations for all pockets
        foreach (var pocket in pockets)
        {
            sensor.AddObservation(pocket.position);
        }
    }

    private bool cpuActionInProgress = false;

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!GameLogic.instance) return;
        string currentPlayerName = GameLogic.instance.players[GameLogic.instance.currentPlayer].name;

        if (currentPlayerName == "CPU" && !cpuActionInProgress)
        {
            Debug.Log("CPU's turn - taking action.");
            cpuActionInProgress = true; // Prevent further calls until reset
            if(training)
            {
                StartCoroutine(PoolMain.instance.HandleCpuPlay());
            }
            else
            {
                StartCoroutine(cut(actions));
            }
        }
    }

    IEnumerator cut(ActionBuffers actions)
    {
        yield return new WaitUntil(() => PoolMain.instance.cpuReady);
        Debug.Log("CPU playing");
        TakeShot(actions);
        PoolMain.instance.cpuReady = false;
    }

    private void TakeShot(ActionBuffers actions)
    {
        // Agent decides shot parameters based on action values
        int selectedBallIndex = actions.DiscreteActions[0];
        int selectedPocketIndex = actions.DiscreteActions[1];
        float xDir = actions.ContinuousActions[0];
        float zDir = actions.ContinuousActions[1];

        float shotPower = actions.ContinuousActions[2];

        Debug.Log("power " + shotPower + " xdir "+ xDir+" zdir "+zDir);

        Transform targetBall = balls[selectedBallIndex];
        Transform targetPocket = pockets[selectedPocketIndex];

        // Implement logic to aim and apply force based on agent's actions
        PoolMain.instance.MakeCpuShot(shotPower, xDir, zDir);

        // Reset `cpuActionInProgress` after shot is completed
        cpuActionInProgress = false;
    }

    // Reset flag at end of turn or episode
    public void ResetCpuActionFlag()
    {
        
    }

    public void EndLearn(bool success)
    {
        cpuActionInProgress = false;
        EndEpisode();
        AddReward(success ? 2 : -1);
    }
}

