using UnityEngine;

namespace Game.Foundation
{
    public readonly struct MatchScoreUpdateEvent
    {
        public readonly int team0Rounds;
        public readonly int team1Rounds;

        public MatchScoreUpdateEvent(int team0, int team1)
        {
            team0Rounds = team0;
            team1Rounds = team1;
        }
    }

}
