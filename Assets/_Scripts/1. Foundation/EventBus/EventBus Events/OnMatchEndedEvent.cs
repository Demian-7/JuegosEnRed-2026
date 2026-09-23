using Fusion;

namespace Game.Foundation
{
    public readonly struct MatchEndedEvent
    {
        public readonly int teamNumber;

        public MatchEndedEvent(int team)
        {
            teamNumber = team;
        }
    }
}