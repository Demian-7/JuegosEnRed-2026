using Game.Core;
using Game.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenManager : MonoBehaviour
{
    
    [SerializeField] private TMP_Text _winnerTeamTitle;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Image _background;

    private IEventBus _eventBus;

    public void Init()
    {
        _eventBus = GameLauncher.Instance.EventBus;
        
        _eventBus.Subscribe<MatchEndedEvent>(OnTeamWin);
        DeactivateElements();
    }

    public void OnDisable()
    {
        _eventBus.Unsubscribe<MatchEndedEvent>(OnTeamWin);
    }

    public void OnTeamWin(MatchEndedEvent evt)
    {
        ActivateElements();
        _winnerTeamTitle.text = $"TEAM {evt.teamNumber} VICTORY";
    }

    private void ActivateElements()
    {
        _background.gameObject.SetActive(true);
        _winnerTeamTitle.gameObject.SetActive(true);
        _title.gameObject.SetActive(true);
        _lobbyButton.gameObject.SetActive(true);
    }

    private void DeactivateElements()
    {
        _background.gameObject.SetActive(false);
        _winnerTeamTitle.gameObject.SetActive(false);
        _title.gameObject.SetActive(false);
        _lobbyButton.gameObject.SetActive(false);
    }
}
