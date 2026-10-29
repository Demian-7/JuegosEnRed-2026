using Fusion;

namespace Game.Meta
{
    public struct PlayerAssignment : INetworkStruct
    {
        public PlayerRef Player;
        public byte TeamId;
        public PlayerRef Teammate;
    }
}