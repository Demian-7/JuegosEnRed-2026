using Fusion;
using System;
using UnityEngine;

public struct NetworkInputData: INetworkInput
{
    public Vector2 Direction;
    public NetworkButtons Buttons;
    
}

public enum InputButton
{
    Shoot = 0
}
