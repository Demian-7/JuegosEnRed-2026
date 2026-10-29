using Game.Core;
using Game.Foundation;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text _actualBullets;
        [SerializeField] private TMP_Text _reloadingState;
        [SerializeField] private TMP_Text _dashState;
        [SerializeField] private TMP_Text _matchState;

        private IEventBus _eventBus;

        public void Init()
        {
            _eventBus = GameLauncher.Instance.EventBus;
            
            _eventBus.Subscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
            _eventBus.Subscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
            _eventBus.Subscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
            _eventBus.Subscribe<DashStateChangedEvent>(OnDashStateChanged);
        }

        private void OnBulletsUpdate(CurrentBulletsUpdateEvent evt)
        {
            _actualBullets.text = $"{evt.currentBullets} / 1"; 
        }

        private void OnUpdateReloadingState(ReloadingStateChangedEvent evt) 
        {
            _reloadingState.text = evt.reloading ? "Reloading" : "";
        }

        private void OnDashStateChanged(DashStateChangedEvent evt) 
        {
            _dashState.text = evt.canDash ? "Ready" : "Cooldown";
        }

        private void OnMatchStateUpdate(MatchScoreUpdateEvent evt) 
        {
            _matchState.text = $"{evt.team0Rounds} - {evt.team1Rounds}";
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            
            _eventBus.Unsubscribe<MatchScoreUpdateEvent>(OnMatchStateUpdate);
            _eventBus.Unsubscribe<CurrentBulletsUpdateEvent>(OnBulletsUpdate);
            _eventBus.Unsubscribe<ReloadingStateChangedEvent>(OnUpdateReloadingState);
            _eventBus.Unsubscribe<DashStateChangedEvent>(OnDashStateChanged);
        }
    }
}