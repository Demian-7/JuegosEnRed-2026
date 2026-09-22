using Fusion;

namespace Game.Foundation
{
    public readonly struct PlayerDiedEvent
    {
        public readonly NetworkObject PlayerRef;

        public PlayerDiedEvent(NetworkObject playerRef)
        {
            PlayerRef = playerRef;
        }
    }
}