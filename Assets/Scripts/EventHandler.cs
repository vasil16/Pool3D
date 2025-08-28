using System;
using UnityEngine;

public class EventHandler : MonoBehaviour
{
    public static Action<GameManager.GameMode> StartGame;
    public static Action <GameObject> PlayableBallTapped;
    public static Action <int> ShotPlayed;
    public static Action <Vector2> SwipeAim;
    public static Action<Vector2> DragAim;
    public static Action<Vector2> SwipeCueBall;
    public static Action<Vector2> MoveCueBall;
    public static Action <Type> Zoom;
    public static Action ShotCompleted;
    public static Action <Vector2> AddSpin;
    public static Action<Vector2> RotateCameraBreak;
    public static Action ResetCam;
    public static Action WaitCPU;
}
