using System.Collections.Generic;
using Fusion;
using Game.Core;
using UnityEngine;

namespace Game.Meta
{
    public class TeamAssignmentService : NetworkBehaviour, ITeammatePairing
    {
        public static TeamAssignmentService Instance { get; private set; }

        [Networked, Capacity(4)] 
        public NetworkArray<PlayerAssignment> Assignments { get; }

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                Runner.Despawn(Object);
                return;
            }
            Instance = this;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public void GeneratePairings()
        {
            if (!Object.HasStateAuthority) return;

            var players = new List<PlayerRef>(Runner.ActivePlayers);

            if (MatchSettings.Instance.TeamMode == ETeamSelectionMode.Manual)
            {
                var teamA = new List<PlayerRef>();
                var teamB = new List<PlayerRef>();

                foreach (var player in players)
                {
                    if (Runner.TryGetPlayerObject(player, out var playerObj) && 
                        playerObj.TryGetComponent<NetworkPlayerState>(out var state))
                    {
                        if (state.TeamId == MatchConstants.TEAM_A) teamA.Add(player);
                        else if (state.TeamId == MatchConstants.TEAM_B) teamB.Add(player);
                    }
                }

                int arrayIndex = 0;
                arrayIndex = WriteTeamToNetworkArray(teamA, MatchConstants.TEAM_A, arrayIndex);
                arrayIndex = WriteTeamToNetworkArray(teamB, MatchConstants.TEAM_B, arrayIndex);
            }
            else
            {
                for (int i = 0; i < players.Count; i++)
                {
                    int r = Random.Range(i, players.Count);
                    (players[i], players[r]) = (players[r], players[i]);
                }

                var teamA = new List<PlayerRef>();
                var teamB = new List<PlayerRef>();

                for (int i = 0; i < players.Count; i++)
                {
                    if (i < 2) teamA.Add(players[i]);
                    else teamB.Add(players[i]);
                }

                int arrayIndex = 0;
                arrayIndex = WriteTeamToNetworkArray(teamA, MatchConstants.TEAM_A, arrayIndex);
                arrayIndex = WriteTeamToNetworkArray(teamB, MatchConstants.TEAM_B, arrayIndex);
            }
        }

        private int WriteTeamToNetworkArray(List<PlayerRef> teamMembers, byte teamId, int startIndex)
        {
            PlayerRef p1 = teamMembers.Count > 0 ? teamMembers[0] : PlayerRef.None;
            PlayerRef p2 = teamMembers.Count > 1 ? teamMembers[1] : PlayerRef.None;

            if (p1 != PlayerRef.None)
            {
                Assignments.Set(startIndex++, new PlayerAssignment
                {
                    Player = p1,
                    TeamId = teamId,
                    Teammate = p2
                });
            }

            if (p2 != PlayerRef.None)
            {
                Assignments.Set(startIndex++, new PlayerAssignment
                {
                    Player = p2,
                    TeamId = teamId,
                    Teammate = p1
                });
            }

            return startIndex;
        }

        public PlayerRef GetAbilitySourceFor(PlayerRef movementOwner, NetworkRunner runner)
        {
            for (int i = 0; i < Assignments.Length; i++)
            {
                var assign = Assignments.Get(i);
                if (assign.Player == movementOwner)
                    return assign.Teammate;
            }
            return PlayerRef.None;
        }
    }
}