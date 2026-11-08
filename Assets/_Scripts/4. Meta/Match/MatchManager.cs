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
        [SerializeField] private HUDManager _hudManager;
        [SerializeField] private EndScreenManager _endScreenManager;
        [SerializeField] private int _lobbySceneIndex = 1;

        [Networked] public MatchPhase Phase { get; set; }
        [Networked] public int CurrentRound { get; set; }

        [Networked, OnChangedRender(nameof (OnMatchScoreUpdatePublish))] 
        public int ScoreTeam0 { get; set; }

        [Networked, OnChangedRender(nameof(OnMatchScoreUpdatePublish))] 
        public int ScoreTeam1 { get; set; }

        [Networked, OnChangedRender(nameof (OnMatchEndedEvent))]
        public int Winner { get; set; }
        [Networked] public int AliveTeam0 { get; set; }
        [Networked] public int AliveTeam1 { get; set; }
        [Networked] private TickTimer Timer { get; set; }

        private int _totalRounds => MatchSettings.Instance != null ? (int)MatchSettings.Instance.Rounds : 3;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                GameLauncher.Instance.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

                StartRound();
            }
                
            _hudManager.Init(GameLauncher.Instance.EventBus);
            _endScreenManager.Init(GameLauncher.Instance.EventBus);

            // During the match each player only talks to their teammate.
            int localTeam = GetTeamIndex(Runner.LocalPlayer);
            if (localTeam >= 0)
            {
                byte voiceGroup = localTeam == 0 ? MatchConstants.TEAM_A : MatchConstants.TEAM_B;
                GameLauncher.Instance.EventBus.Publish(new VoiceChannelChangedEvent(voiceGroup));
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

            int teamId = GetTeamIndex(evt.DeadPlayer);
            if (teamId < 0)
            {
                Debug.LogError("Dead player's index is < 0??");
                return;
            }

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
        
        //"Emulate" StubTeammatePairing logic to find a player's team (0 or 1), -1 if not found.
        //When real TeamAssignmentService si built, use it instead.
        private int GetTeamIndex(PlayerRef player)
        {
            var sorted = new List<PlayerRef>(Runner.ActivePlayers);
            sorted.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

            int index = sorted.IndexOf(player);
            if (index < 0)
                return -1;

            //Players 0 & 1 are Team 0, players 2 & 3 are Team 1.
            return (index < 2) ? 0 : 1;
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

            _spawner.DespawnAll();
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
                CalculateWinner();
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

        private void OnMatchScoreUpdatePublish()
        {
            GameLauncher.Instance.EventBus.Publish(new MatchScoreUpdateEvent(ScoreTeam0, ScoreTeam1));
        }

        private void OnMatchEndedEvent()
        {
            GameLauncher.Instance.EventBus.Publish(new MatchEndedEvent(Winner));
        }

        public void ReturnToLobby()
        {
            Runner.LoadScene(SceneRef.FromIndex(_lobbySceneIndex));
        }

        private void CalculateWinner()
        {
            if (ScoreTeam0 > ScoreTeam1)
            {
                Winner = 1;
            }
            else
            {
                Winner = 2;
            }
        }
    }
}
