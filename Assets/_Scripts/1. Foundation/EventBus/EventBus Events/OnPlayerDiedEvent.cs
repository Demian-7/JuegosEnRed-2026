using Fusion;

namespace Game.Foundation
{
    public readonly struct PlayerDiedEvent
    {
        public readonly PlayerRef DeadPlayer;

        public PlayerDiedEvent(PlayerRef playerRef)
        {
            DeadPlayer = playerRef;
        }
    }
}