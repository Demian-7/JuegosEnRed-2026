using System.Collections.Generic;
using Fusion;
using Game.Core;
using Game.Foundation;
using Game.Simulation;
using UnityEngine;

namespace Game.Meta
{
    public enum MatchPhase
    {
        Playing,
        RoundEnded,
        Ended
    }

    public class MatchManager : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private MatchSpawner _spawner;
        [SerializeField] private int _lobbySceneIndex = 1;
        [SerializeField] private int _totalRounds = 3;

        [Networked] public MatchPhase Phase { get; set; }
        [Networked] public int CurrentRound { get; set; }
        [Networked] public int ScoreTeam0 { get; set; }
        [Networked] public int ScoreTeam1 { get; set; }

        [Networked] public int AliveTeam0 { get; set; }
        [Networked] public int AliveTeam1 { get; set; }
        [Networked] private TickTimer Timer { get; set; }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                Runner.GetComponentInParent<GameLauncher>().EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
                StartRound();
            }
        }

        private void Update()
        {
            if (Object == null || !Object.HasStateAuthority) 
                return;

            if (Input.GetKeyDown(KeyCode.Escape)) 
                EndMatch();
        }

        private void StartRound()
        {
            CurrentRound++;
            Phase = MatchPhase.Playing;
            _spawner.DespawnAll();
            ClearRemainingBullets();
            _spawner.SpawnAll();

            AliveTeam0 = 2;
            AliveTeam1 = 2;
            
            Debug.Log($"[Match] Round {CurrentRound} started");
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (Phase != MatchPhase.Playing)
                return;
            
            //"Emulate" StubTeammatePairing logic to find this player's team.
            //When real TeamAssignmentService si built, use it instead.
            var sorted = new List<PlayerRef>(Runner.ActivePlayers);
            sorted.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

            int index = sorted.IndexOf(evt.DeadPlayer);
            if (index < 0)
            {
                Debug.LogError("Dead player's index is < 0??");
                return;
            }

            //Players 0 & 1 are Team 0, players 2 & 3 are Team 1.
            int teamId = (index < 2) ? 0 : 1;

            if (teamId == 0)
            {
                AliveTeam0--;
            }
            else
            {
                AliveTeam1--;
            }
            
            Debug.Log($"Player {evt.DeadPlayer} died. Team 0 Alive: {AliveTeam0}, Team 1 Alive: {AliveTeam1}");

            if (AliveTeam0 <= 0)
            {
                EndRound(1);
            }
            else if (AliveTeam1 <= 0)
            {
                EndRound(0);
            }
        }
        
        // Call this on the host when a team is fully dead.
        public void EndRound(int winnerTeam)
        {
            if (Phase != MatchPhase.Playing) 
                return;

            if (winnerTeam == 0) ScoreTeam0++;
            else ScoreTeam1++;

            Debug.Log($"[Match] Round {CurrentRound} won by team {winnerTeam}. Score {ScoreTeam0} - {ScoreTeam1}");
            Phase = MatchPhase.RoundEnded;
            Timer = TickTimer.CreateFromSeconds(Runner, 2f);
        }

        public void EndMatch()
        {
            if (Phase == MatchPhase.Ended) 
                return;

            Debug.Log($"[Match] Match ended. Score {ScoreTeam0} - {ScoreTeam1}");
            Phase = MatchPhase.Ended;
            Timer = TickTimer.CreateFromSeconds(Runner, 3f);
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (Object.HasStateAuthority)
                EndMatch();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || !Timer.Expired(Runner))
                return;
            
            Timer = TickTimer.None;

            if (Phase == MatchPhase.RoundEnded)
            {
                if (CurrentRound >= _totalRounds) EndMatch();
                else StartRound();
            }
            else if (Phase == MatchPhase.Ended)
            {
                Runner.LoadScene(SceneRef.FromIndex(_lobbySceneIndex));
            }
        }
        
        private void ClearRemainingBullets()
        {
            // Create a temporary list to safely iterate while the HashSet modifies itself
            var bulletsToDespawn = new List<Bullet>(Bullet.ActiveBullets);
            
            foreach (var bullet in bulletsToDespawn)
            {
                if (bullet.Object != null && bullet.Object.IsValid)
                {
                    Runner.Despawn(bullet.Object);
                }
            }
        }
    }
}
