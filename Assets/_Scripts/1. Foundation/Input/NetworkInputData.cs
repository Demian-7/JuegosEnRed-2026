using Fusion;
using UnityEngine;

namespace Game.Foundation
{
    public enum EAbilityButton
    {
        Shoot,
        Dash,
        Parry
    }

    public struct NetworkInputData : INetworkInput
    {
        public Vector2 MovementInput;
        public Vector2 AimWorldPosition;
        public NetworkButtons Buttons;
    }
}