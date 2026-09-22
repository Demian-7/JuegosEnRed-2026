using Fusion;
using Game.Core;
using Game.Foundation;
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
        [Networked] private TickTimer Timer { get; set; }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                StartRound();

                Runner.GetComponent<GameLauncher>().EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            }

        }

        private void Update()
        {
            if (Object == null || !Object.HasStateAuthority) return;

            if (Input.GetKeyDown(KeyCode.Escape)) EndMatch();

            // DEBUG until characters can die: give the round to team 0 / team 1
            if (Input.GetKeyDown(KeyCode.Alpha1)) EndRound(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EndRound(1);
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            Debug.Log($"Player {evt.PlayerRef.Name} died!");
        }

        private void StartRound()
        {
            CurrentRound++;
            Phase = MatchPhase.Playing;
            _spawner.DespawnAll();
            _spawner.SpawnAll();
            Debug.Log($"[Match] Round {CurrentRound} started");
        }

        // Call this on the host when a team is fully dead.
        public void EndRound(int winnerTeam)
        {
            if (Phase != MatchPhase.Playing) return;

            if (winnerTeam == 0) ScoreTeam0++;
            else ScoreTeam1++;

            Debug.Log($"[Match] Round {CurrentRound} won by team {winnerTeam}. Score {ScoreTeam0} - {ScoreTeam1}");
            Phase = MatchPhase.RoundEnded;
            Timer = TickTimer.CreateFromSeconds(Runner, 2f);
        }

        public void EndMatch()
        {
            if (Phase == MatchPhase.Ended) return;

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
            if (!Object.HasStateAuthority || !Timer.Expired(Runner)) return;
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
    }
}
