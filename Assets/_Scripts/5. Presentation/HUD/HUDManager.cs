using Game.Foundation;
using Game.Meta;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    private IEventBus _eventBus;
    
    [SerializeField] private TMP_Text _actualBullets;
    [SerializeField] private TMP_Text _reloadingState;
    [SerializeField] private TMP_Text _dashState;
    [SerializeField] private TMP_Text _matchState;
    [SerializeField] private TMP_Text _countdownText;

    private float _shootTextLingerTimer;

    public void Init(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
        _eventBus.Subscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
        _eventBus.Subscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
        _eventBus.Subscribe<DashStateChangedEvent>(OnDashStateChanged);
        
        _countdownText.text = "";
    }

    private void Update()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.Runner == null) return;

        if (MatchManager.Instance.Phase == MatchPhase.RoundEnded)
        {
            _countdownText.text = $"Team {MatchManager.Instance.LastRoundWinner} wins the round!\nScore: {MatchManager.Instance.ScoreTeam0} - {MatchManager.Instance.ScoreTeam1}";
        }
        else if (MatchManager.Instance.Phase == MatchPhase.PreRound)
        {
            float? remaining = MatchManager.Instance.Timer.RemainingTime(MatchManager.Instance.Runner);
            if (remaining.HasValue)
            {
                int seconds = Mathf.CeilToInt(remaining.Value);
                _countdownText.text = seconds.ToString();
                _shootTextLingerTimer = 0f; 
            }
        }
        else if (MatchManager.Instance.Phase == MatchPhase.Playing)
        {
            if (_shootTextLingerTimer < 1f)
            {
                _countdownText.text = "Shoot!";
                _shootTextLingerTimer += Time.deltaTime;
            }
            else
            {
                _countdownText.text = "";
            }
        }
        else if (MatchManager.Instance.Phase == MatchPhase.Ended)
        {
            _countdownText.text = $"Match Over!\nTeam {MatchManager.Instance.Winner} Wins!";
        }
        else
        {
            _countdownText.text = "";
        }
    }

    public void OnBulletsUpdate(CurrentBulletsUpdateEvent evt) => _actualBullets.text = $"{evt.currentBullets} / 1";
    public void OnUpdateReloadingState(ReloadingStateChangedEvent evt) => _reloadingState.text = evt.reloading ? "Reloading" : "";
    public void OnDashStateChanged(DashStateChangedEvent evt) => _dashState.text = evt.canDash ? "Ready" : "Cooldown";
    public void OnMatchStateUpdate(MatchScoreUpdateEvent evt) => _matchState.text = $"{evt.team0Rounds} - {evt.team1Rounds}";

    private void OnDisable()
    {
        _eventBus?.Unsubscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
        _eventBus?.Unsubscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
        _eventBus?.Unsubscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
        _eventBus?.Unsubscribe<DashStateChangedEvent>(OnDashStateChanged);
    }
}