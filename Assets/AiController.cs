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

    public override void OnEpisodeBegin()
    {
        Debug.Log("ml beggg");
        if (GameLogic.instance && GameLogic.instance.players[GameLogic.instance.currentPlayer].name == "CPU")
        {

        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(cueBall.position);

        if(PoolMain.instance.lockedPocket!=null)
        {
            sensor.AddObservation(PoolMain.instance.lockedPocket.position);
        }
        if (PoolMain.instance.lockedBall != null)
        {
            sensor.AddObservation(PoolMain.instance.lockedBall.position);
            sensor.AddObservation(PoolMain.instance.lockedBall.gameObject.activeInHierarchy ? 1f : 0f);
        }
    }

    private bool cpuActionInProgress = false;

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (cpuActionInProgress||training) return;
        StartCoroutine(cut(actions));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(PoolMain.instance.waiting)
        {
            Debug.Log("heu learn"); 
            ActionSegment<float> continousActions = actionsOut.ContinuousActions;
            continousActions[0] = PoolMain.instance.cueAnchor.transform.eulerAngles.x;
            Debug.Log("h 1 " + continousActions[0]); 
            continousActions[1] = PoolMain.instance.cueAnchor.transform.eulerAngles.y;
            Debug.Log("h 2 " + continousActions[1]);
            continousActions[2] = PoolMain.instance.cueAnchor.transform.eulerAngles.z;
            Debug.Log("h 3 " + continousActions[2]);
            continousActions[3] = PoolMain.instance.hitPower;
            Debug.Log("h 4 " + continousActions[3]);
            PoolMain.instance.waiting = false;
        }
    }

    IEnumerator cut(ActionBuffers actions)
    {
        cpuActionInProgress = true;
        yield return new WaitUntil(() => PoolMain.instance.cpuReady);

        Debug.Log("CPU playing");

        float xRotation = actions.ContinuousActions[0];
        float yRotation = actions.ContinuousActions[1];
        float zRotation = actions.ContinuousActions[2];
        float shotPower = actions.ContinuousActions[3];

        Debug.Log("Rotation: X: " + xRotation + ", Y: " + yRotation + ", Z: " + zRotation + " Power: " + shotPower);

        Vector3 targetDirection = new Vector3(xRotation, yRotation, zRotation);

        yield return new WaitForSeconds(0);
        PoolMain.instance.MakeCpuShot(shotPower, targetDirection);

        cpuActionInProgress = false;
        PoolMain.instance.cpuReady = false;
    }

    public void EndLearn(bool success, int reward)
    {
        cpuActionInProgress = false;
        EndEpisode();
        //AddReward(success ? 2 : -1);
        AddReward(reward);
    }
}

