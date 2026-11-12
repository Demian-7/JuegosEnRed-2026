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

            Init(_tank);
        }

        public void Init(TankController tank)
        {
            if (tank == null || tank.Runner == null) return;

            if (tank.MovementSource == tank.Runner.LocalPlayer)
            {
                _nameText.text = "You";
                _nameText.color = Color.yellow; 
            }
            else
            {
                foreach (var playerState in FindObjectsOfType<NetworkPlayerState>())
                {
                    if (playerState.PlayerRef == tank.MovementSource)
                    {
                        _nameText.text = playerState.PlayerName.ToString();
                        _nameText.color = Color.white;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (MatchManager.Instance == null) return;

            bool shouldShow = MatchManager.Instance.Phase == MatchPhase.PreRound;
            
            if (_nameText.enabled != shouldShow)
            {
                _nameText.enabled = shouldShow;
            }
        }
    }
}