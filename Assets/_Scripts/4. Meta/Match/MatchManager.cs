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
        TeamAssignment, // Added to pause execution until assignments exist
        Playing,
        RoundEnded,
        Ended
    }

    public class MatchManager : NetworkBehaviour, IPlayerLeft
    {
        [SerializeField] private MatchSpawner _spawner;
        [SerializeField] private int _lobbySceneIndex = 1;

        [Networked, OnChangedRender(nameof(OnPhaseChanged))] 
        public MatchPhase Phase { get; set; }
        
        [Networked] public int CurrentRound { get; set; }
        [Networked] public int AliveTeam0 { get; set; }
        [Networked] public int AliveTeam1 { get; set; }

        [Networked, OnChangedRender(nameof(OnMatchScoreUpdatePublish))] 
        public int ScoreTeam0 { get; set; }

        [Networked, OnChangedRender(nameof(OnMatchScoreUpdatePublish))] 
        public int ScoreTeam1 { get; set; }

        [Networked, OnChangedRender(nameof(OnMatchEndedEvent))]
        public int Winner { get; set; } = -1; 
        
        [Networked] private TickTimer Timer { get; set; }

        private int _totalRounds => MatchSettings.Instance != null ? (int)MatchSettings.Instance.Rounds : 3;
        private int _winThreshold => (_totalRounds / 2) + 1;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                GameLauncher.Instance.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
                
                Phase = MatchPhase.TeamAssignment; 
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Object.HasStateAuthority)
            {
                GameLauncher.Instance.EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            }
        }

        private void Update()
        {
            if (Object == null || !Object.HasStateAuthority) 
                return;

            if (Input.GetKeyDown(KeyCode.Escape)) 
                EndMatch();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (Phase == MatchPhase.TeamAssignment)
            {
                if (TeamAssignmentService.Instance == null) return;

                TeamAssignmentService.Instance.GeneratePairings();
                StartRound();
                return;
            }

            if (!Timer.Expired(Runner))
                return;
            
            Timer = TickTimer.None;

            if (Phase == MatchPhase.RoundEnded)
            {
                if (ScoreTeam0 >= _winThreshold || ScoreTeam1 >= _winThreshold) 
                    EndMatch();
                else 
                    StartRound();
            }
            else if (Phase == MatchPhase.Ended)
            {
                ReturnToLobby();
            }
        }

        private void StartRound()
        {
            CurrentRound++;
            _spawner.DespawnAll();
            ClearRemainingBullets();
            _spawner.SpawnAll();

            AliveTeam0 = 2;
            AliveTeam1 = 2;
            
            Phase = MatchPhase.Playing;
            Debug.Log($"[Match] Round {CurrentRound} started");
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (Phase != MatchPhase.Playing)
                return;

            int teamId = GetTeamIndex(evt.DeadPlayer);
            if (teamId < 0)
            {
                Debug.LogError($"Could not find team for dead player: {evt.DeadPlayer}");
                return;
            }

            if (teamId == 0) AliveTeam0--;
            else AliveTeam1--;
            
            Debug.Log($"Player {evt.DeadPlayer} died. Team 0 Alive: {AliveTeam0}, Team 1 Alive: {AliveTeam1}");

            if (AliveTeam0 <= 0) EndRound(1);
            else if (AliveTeam1 <= 0) EndRound(0);
        }
        
        private int GetTeamIndex(PlayerRef player)
        {
            if (TeamAssignmentService.Instance == null) return -1;

            var assignments = TeamAssignmentService.Instance.Assignments;
            for (int i = 0; i < assignments.Length; i++)
            {
                var assign = assignments.Get(i);
                if (assign.Player == player)
                    return assign.TeamId;
            }
            return -1;
        }

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

            _spawner.DespawnAll();
            Debug.Log($"[Match] Match ended. Score {ScoreTeam0} - {ScoreTeam1}");
            
            Winner = ScoreTeam0 > ScoreTeam1 ? 0 : 1;
            Phase = MatchPhase.Ended;
            
            Timer = TickTimer.CreateFromSeconds(Runner, 4f); 
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (Object.HasStateAuthority)
                EndMatch();
        }
        
        private void ClearRemainingBullets()
        {
            var bulletsToDespawn = new List<Bullet>(Bullet.ActiveBullets);
            foreach (var bullet in bulletsToDespawn)
            {
                if (bullet.Object != null && bullet.Object.IsValid)
                {
                    Runner.Despawn(bullet.Object);
                }
            }
        }

        private void OnPhaseChanged()
        {
            if (Phase == MatchPhase.Playing && CurrentRound == 1)
            {
                int localTeam = GetTeamIndex(Runner.LocalPlayer);
                if (localTeam >= 0)
                {
                    byte voiceGroup = localTeam == 0 ? MatchConstants.TEAM_A : MatchConstants.TEAM_B;
                    GameLauncher.Instance.EventBus.Publish(new VoiceChannelChangedEvent(voiceGroup));
                }
            }
        }

        private void OnMatchScoreUpdatePublish()
        {
            GameLauncher.Instance.EventBus.Publish(new MatchScoreUpdateEvent(ScoreTeam0, ScoreTeam1));
        }

        private void OnMatchEndedEvent()
        {
            if (Winner != -1)
                GameLauncher.Instance.EventBus.Publish(new MatchEndedEvent(Winner));
        }

        public void ReturnToLobby()
        {
            Runner.LoadScene(SceneRef.FromIndex(_lobbySceneIndex));
        }
    }
}