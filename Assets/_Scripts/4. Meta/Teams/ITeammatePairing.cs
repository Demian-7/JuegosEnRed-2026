using System.Collections.Generic;
using Fusion;

namespace Game.Meta
{
    /// <summary>
    /// Contract for "who provides this player's character's abilities". i.e. their teammate.
    /// </summary>
    public interface ITeammatePairing
    {
        // TankController depends on this instead of computing pairing itself, so the real
        // TeamAssignmentService can be dropped in later without TankController changing at all.
        PlayerRef GetAbilitySourceFor(PlayerRef movementOwner, NetworkRunner runner);
    }

    public class StubTeammatePairing : ITeammatePairing
    {
        public PlayerRef GetAbilitySourceFor(PlayerRef movementOwner, NetworkRunner runner)
        {
            if (runner == null || runner.ActivePlayers == null)
                return PlayerRef.None;

            var sorted = new List<PlayerRef>(runner.ActivePlayers);
            sorted.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

            int index = sorted.IndexOf(movementOwner);
            if (index < 0)
                return PlayerRef.None;

            int partnerIndex = (index % 2 == 0) ? index + 1 : index - 1;
            if (partnerIndex < 0 || partnerIndex >= sorted.Count)
                return PlayerRef.None;

            return sorted[partnerIndex];
        }
    }
}