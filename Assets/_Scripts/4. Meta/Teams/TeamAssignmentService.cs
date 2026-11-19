using System.Collections.Generic;
using Fusion;
using Game.Core;
using UnityEngine;

namespace Game.Meta
{
    public class TeamAssignmentService : ITeammatePairing
    {
        /// <summary>
        /// Host-only: Shuffles active players and splits them evenly between TEAM_A and TEAM_B.
        /// </summary>
        public static void AssignRandomTeams(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
                return;

            var players = new List<PlayerRef>(runner.ActivePlayers);

            // Fisher-Yates shuffle
            for (int i = players.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (players[i], players[j]) = (players[j], players[i]);
            }

            int half = players.Count / 2;

            for (int i = 0; i < players.Count; i++)
            {
                if (runner.TryGetPlayerObject(players[i], out var playerObj))
                {
                    var state = playerObj.GetComponent<NetworkPlayerState>();
                    if (state != null)
                    {
                        state.TeamId = (i < half) ? MatchConstants.TEAM_A : MatchConstants.TEAM_B;
                    }
                }
            }
        }

        /// <summary>
        /// Returns 0 for TEAM_A, 1 for TEAM_B, or -1 if unassigned/not found.
        /// </summary>
        public static int GetTeamIndex(PlayerRef player, NetworkRunner runner)
        {
            if (runner != null && runner.TryGetPlayerObject(player, out var playerObj))
            {
                var state = playerObj.GetComponent<NetworkPlayerState>();
                if (state != null)
                {
                    if (state.TeamId == MatchConstants.TEAM_A) return 0;
                    if (state.TeamId == MatchConstants.TEAM_B) return 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the teammate who shares the same TeamId as movementOwner.
        /// </summary>
        public PlayerRef GetAbilitySourceFor(PlayerRef movementOwner, NetworkRunner runner)
        {
            if (runner == null || runner.ActivePlayers == null)
                return PlayerRef.None;

            int ownerTeam = GetTeamIndex(movementOwner, runner);
            if (ownerTeam < 0)
                return PlayerRef.None;

            foreach (var player in runner.ActivePlayers)
            {
                if (player == movementOwner)
                    continue;

                if (GetTeamIndex(player, runner) == ownerTeam)
                {
                    return player;
                }
            }

            return PlayerRef.None;
        }
    }
}