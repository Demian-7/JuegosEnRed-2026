using UnityEngine;
using TMPro;
using Game.Core;
using Game.Simulation;
using Game.Meta;

namespace Game.Presentation
{
    public class TankUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TankController _tank; 

        private bool _isNameSet = false;

        private void Start()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = Camera.main;
            }

            if (_tank == null)
            {
                _tank = GetComponentInParent<TankController>(); 
            }
        }

        private void Update()
        {
            if (MatchManager.Instance == null || _tank == null || _tank.Object == null || !_tank.Object.IsValid) return;

            bool shouldShow = MatchManager.Instance.Phase == MatchPhase.PreRound;
            
            if (_nameText.enabled != shouldShow)
            {
                _nameText.enabled = shouldShow;
            }

            if (shouldShow && !_isNameSet)
            {
                TryInitializeName();
            }
        }

        private void TryInitializeName()
        {
            if (_tank.MovementSource.IsNone) return;

            if (_tank.MovementSource == _tank.Runner.LocalPlayer)
            {
                _nameText.text = "You";
                _nameText.color = Color.yellow; 
                _isNameSet = true;
            }
            else
            {
                foreach (var playerState in FindObjectsOfType<NetworkPlayerState>())
                {
                    if (playerState.PlayerRef == _tank.MovementSource)
                    {
                        string playerName = playerState.PlayerName.ToString();
                        
                        if (!string.IsNullOrEmpty(playerName))
                        {
                            _nameText.text = playerName;
                            _nameText.color = Color.white;
                            _isNameSet = true;
                        }
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            _isNameSet = false;
        }
    }
}