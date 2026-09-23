using Game.Foundation;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    private IEventBus _eventBus;
    [SerializeField] private TMP_Text _actualBullets;
    [SerializeField] private TMP_Text _reloadingState;
    [SerializeField] private TMP_Text _dashState;
    [SerializeField] private TMP_Text _matchState;
    [SerializeField] private TMP_Text _x1;
    [SerializeField] private TMP_Text _x2;


    public void Init(IEventBus eventBus)
    {
        _eventBus = eventBus;
        Debug.Log(_eventBus);
        _eventBus.Subscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
        _eventBus.Subscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
        _eventBus.Subscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
        _eventBus.Subscribe<DashStateChangedEvent>(OnDashStateChanged);
        Debug.Log("Hud Manager start");
    }
    public void OnBulletsUpdate(CurrentBulletsUpdateEvent evt)
    {
        Debug.Log("OnBulletsUpdate");
        _actualBullets.text = $"{evt.currentBullets} / 1";
    }

    public void OnUpdateReloadingState(ReloadingStateChangedEvent evt) 
    {
        if (evt.reloading) 
        {
            _reloadingState.text = "Reloading";
        }
        else
        {
            _reloadingState.text = "";

        }

    }


    public void OnDashStateChanged(DashStateChangedEvent evt) 
    {
        if (evt.canDash)
        {
            _dashState.text = "Ready";
        }
        else
        {
            _dashState.text = "Cooldown";

        }
    }
    public void OnMatchStateUpdate(MatchScoreUpdateEvent evt) 
    {
        _matchState.text = $"{evt.team0Rounds} - {evt.team1Rounds}";
    }

    private void OnDisable()
    {
        _eventBus?.Unsubscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
        _eventBus?.Unsubscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
        _eventBus?.Unsubscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
        _eventBus?.Unsubscribe<DashStateChangedEvent>(OnDashStateChanged);
    }
}
