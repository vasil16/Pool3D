using System;
using UnityEngine;

public class EventHandler : MonoBehaviour
{
    public static Action<GameManager.GameMode> StartGame;
    public static Action <GameObject> PlayableBallTapped;
    public static Action <int> ShotPlayed;
    public static Action CameraAction;
    public static Action <Type> Zoom;
    public static Action ShotCompleted;
}
